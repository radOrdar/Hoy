using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hoy.Cards;
using Hoy.Helpers;
using Hoy.Services;
using Hoy.StaticData;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Hoy
{
    public abstract class BaseGameManager : NetworkBehaviour
    {
        public static BaseGameManager Instance { get; private set; }

        [field: SerializeField] public Transform DealZone { get; private set; }
        /// <summary>DON'T MODIFY!</summary>
        [SerializeField] public CardStaticData[] cardStaticDatas;
        [SerializeField] private Transform cardDeckSpawnTrans;
        [SerializeField] private Card cardPf;

        protected List<HoyPlayer> _hoyPlayers;
        protected ListNode<HoyPlayer> _playerNodes;

        protected List<Card> _cardsSpawned = new();

        [field: SyncVar]
        public GameState CurrentGameState { get; set; }

        [field: SyncVar(hook = nameof(OnWhosNextMoveChanged))]
        public HoyPlayer WhosNextMove { get; protected set; }

        protected PlayedOutCardSlotPack _playedOutCardSlotPack;


        private void Awake()
        {
            Instance = this;
        }

        public override void OnStartClient()
        {
            var leaderRoomPlayer = HoyRoomNetworkManager.Singleton.LeaderPlayer;
            FindObjectOfType<UI>().SetRoundsInfo(leaderRoomPlayer.CurrentRound, leaderRoomPlayer.NumOfRounds);
        }

        [Server]
        public IEnumerator StartGame(List<HoyPlayer> players)
        {
            _hoyPlayers = players;
            foreach (var player in _hoyPlayers)
            {
                player.TargetGameStarted();
            }

            _playerNodes = new ListNode<HoyPlayer>(players[0]);
            var playerNodesTemp = _playerNodes;
            for (int i = 1; i < players.Count; i++)
            {
                playerNodesTemp.Next = new ListNode<HoyPlayer>(players[i]);
                playerNodesTemp = playerNodesTemp.Next;
            }

            playerNodesTemp.Next = _playerNodes;
            for (int i = 0; i < Random.Range(0, players.Count); i++)
            {
                _playerNodes = _playerNodes.Next;
            }

            NewPlayedCardSlotPack();
            CurrentGameState = GameState.DealingCards;
            InitCardDeck();
            AudioService.Instance.RpcPlayOneShotDelayed(AudioSfxType.ShuffleCards, 0);
            yield return new WaitForSeconds(1f);
            yield return StartCoroutine(DealCardsToPlayersFromDeck());
            OnCardsDealed();
        }

        private IEnumerator DealCardsToPlayersFromDeck()
        {
            yield return StartCoroutine(DealCardsToPlayersRoutine(_hoyPlayers, GetCardsPackToDeal()));

            IEnumerator DealCardsToPlayersRoutine(List<HoyPlayer> players, List<List<Card>> listCards)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    yield return StartCoroutine(DealCardsToOnePlayerRoutine((p, c) => p.TakeCard(c), players[i], listCards[i]));
                }

                CurrentGameState = GameState.PlayerTurn;
                WhosNextMove = _playerNodes.Value;
            }
        }

        protected abstract List<List<Card>> GetCardsPackToDeal();

        protected virtual void OnCardsDealed()
        { }

        [Server]
        protected void NewPlayedCardSlotPack()
        {
            _playedOutCardSlotPack = new PlayedOutCardSlotPack(0, 4.5f);
        }

        [Server]
        private void InitCardDeck()
        {
            SpawnCards();

            AssignDataToCard();

            void SpawnCards()
            {
                float currY = 0;

                for (int i = 0; i < cardStaticDatas.Select(c => c.numberInDeck).Sum(); i++)
                {
                    Card card = Instantiate(cardPf);
                    NetworkServer.Spawn(card.gameObject);
                    _cardsSpawned.Add(card);

                    card.transform.position = cardDeckSpawnTrans.position + new Vector3(0, currY);
                    currY -= 0.05f;
                }
            }

            void AssignDataToCard()
            {
                List<int> indices = Enumerable.Range(0, cardStaticDatas.Select(c => c.numberInDeck).Sum()).ToList();
                foreach (CardStaticData cardStaticData in cardStaticDatas)
                {
                    for (int i = 0; i < cardStaticData.numberInDeck; i++)
                    {
                        int indexToTake = indices[Random.Range(0, indices.Count)];
                        _cardsSpawned[indexToTake].Initialize(cardStaticData);
                        indices.Remove(indexToTake);
                    }
                }
            }
        }

        protected IEnumerator DealCardsToOnePlayerRoutine(Action<HoyPlayer, Card> dealAction, HoyPlayer player, IEnumerable<Card> cards)
        {
            foreach (Card card in cards)
            {
                dealAction(player, card);
                yield return new WaitForSeconds(card.cardDealMoveTime);
            }
        }

        [Server]
        public virtual void DragEnded(Card card)
        {
            HoyPlayer player = card.connectionToClient.owned.First(_ => _.GetComponent<HoyPlayer>() != null).GetComponent<HoyPlayer>();
            var dealZoneRadius = DealZone.transform.localScale.x / 2;
            var cardToDealZone = DealZone.transform.position - card.transform.position;
            if (cardToDealZone.sqrMagnitude > dealZoneRadius * dealZoneRadius)
            {
                player.TakeCard(card);
                card.netIdentity.RemoveClientAuthority();
            } else
            {
                if (_playedOutCardSlotPack.Count % 2 == 0)
                {
                    PlayCard(card, player);
                    CurrentGameState = GameState.PlayerTurn;
                    _playerNodes = _playerNodes.Next;
                    WhosNextMove = _playerNodes.Value;
                } else
                {
                    if (card.Value == _playedOutCardSlotPack.LastCard.Value)
                    {
                        PlayCard(card, player);
                        CurrentGameState = GameState.PlayerTurn;
                        _playerNodes = _playerNodes.Next;
                        WhosNextMove = _playerNodes.Value;
                        if (_hoyPlayers.All(_ => _.IsEmpty()))
                        {
                            StartCoroutine(FromTableToBankRoutine());
                        }
                    } else
                    {
                        PlayCard(card, player);
                        StartCoroutine(FromTableToBankRoutine());
                    }
                }
            }
        }

        [Server]
        private IEnumerator FromTableToBankRoutine()
        {
            WhosNextMove = null;
            CurrentGameState = GameState.DealingCards;
            yield return new WaitForSeconds(2);
            yield return StartCoroutine(DealCardsToOnePlayerRoutine((p, c) => p.AddToBank(c), _playedOutCardSlotPack.Winner, _playedOutCardSlotPack.GetCards()));
            while (_playerNodes.Value != _playedOutCardSlotPack.Winner)
                _playerNodes = _playerNodes.Next;
            NewPlayedCardSlotPack();

            if (_hoyPlayers.All(_ => _.IsEmpty()))
            {
                if (_cardsSpawned.Count == 0)
                {
                    StartCoroutine(GameOver());
                } else
                {
                    StartCoroutine(DealCardsToPlayersFromDeck());
                }
            } else
            {
                WhosNextMove = _playerNodes.Value;
                CurrentGameState = GameState.PlayerTurn;
            }
        }

        protected IEnumerator GameOver()
        {
            CurrentGameState = GameState.GameOver;
            foreach (var player in _hoyPlayers)
            {
                player.RPCSetGameOverUI();
            }

            yield return StartCoroutine(CountPointsRoutine());
            yield return new WaitForSeconds(2f);
            var winnerOfRound = _hoyPlayers.OrderByDescending(_ => _.Score).First();
            var networkManager = HoyRoomNetworkManager.Singleton;
            var hoyRoomPlayers = networkManager.roomSlots.Cast<HoyRoomPlayer>();
            var roomPlayerOfWinner = hoyRoomPlayers.First(_ => _.PlayerName == winnerOfRound.PlayerName);
            roomPlayerOfWinner.Wins++;
            foreach (var hoyPlayer in _hoyPlayers)
            {
                hoyPlayer.RpcShowWinner(winnerOfRound);
            }

            yield return new WaitForSeconds(3f);

            foreach (var hoyPlayer in _hoyPlayers)
            {
                hoyPlayer.RpcShowSeriesStat();
            }

            yield return new WaitForSeconds(3f);

            if (networkManager.LeaderPlayer.CurrentRound < networkManager.LeaderPlayer.NumOfRounds)
            {
                networkManager.NextRound();
            } else
            {
                foreach (var hoyPlayer in _hoyPlayers)
                {
                    hoyPlayer.RpcGameOver();
                }
            }

            yield return new WaitForSeconds(5f);
            //ToDO ui counter to next round
            networkManager.NextRound();

            IEnumerator CountPointsRoutine()
            {
                foreach (var player in _hoyPlayers)
                {
                    int score = 0;
                    int orderInLayer = 0;
                    var cards = player.GetBank();
                    cards.Reverse();
                    foreach (var card in cards)
                    {
                        score += card.FaceType == CardFaceType.FInfinity ? 0 : card.Value;
                        var score1 = score;
                        card.SetTargetServer(Vector3.zero, () => player.RpcSetScore(score1));
                        AudioService.Instance.RpcPlayOneShotDelayed(AudioSfxType.PlayTable, card.cardDealMoveTime - 0.1f);
                        card.RpcSetOrderInLayer(orderInLayer++);
                        yield return new WaitForSeconds(card.cardDealMoveTime);
                    }

                    foreach (var c in cards)
                    {
                        NetworkServer.Destroy(c.gameObject);
                    }
                    player.Score = score;
                }
            }
        }

        [Server]
        protected void PlayCard(Card card, HoyPlayer player)
        {
            card.netIdentity.RemoveClientAuthority();
            card.RpcShowCardToAllClients();
            _playedOutCardSlotPack.AddCard(card, player);
        }

        private void OnWhosNextMoveChanged(HoyPlayer oldValue, HoyPlayer newValue)
        {
            var ui = FindObjectOfType<UI>();
            if (ui != null) ui.SetMoveNextName(newValue != null ? newValue.PlayerName : null);
        }
    }
}
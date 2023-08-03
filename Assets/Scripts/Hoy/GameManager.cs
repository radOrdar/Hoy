using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hoy.StaticData;
using Mirror;
using UnityEngine;
using Object = System.Object;
using Random = UnityEngine.Random;

namespace Hoy
{
    public class GameManager : NetworkBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private Transform dealZone;
        [SerializeField] private Transform cardDeckSpawnTrans;
        [SerializeField] private CardStaticData[] cardStaticDatas;
        [SerializeField] private Card cardPf;

        private HoyPlayer[] _hoyPlayers;
        private ListNode<HoyPlayer> _playerNodes;

        private List<Card> _cardsSpawned = new();

        [field: SyncVar]
        public GameState CurrentGameState { get; set; }

        [field: SyncVar(hook = nameof(OnWhosNextMoveChanged))]
        public HoyPlayer WhosNextMove { get; set; }

        private Bounds _dealZoneBounds;
        private PlayedOutCardSlotPack _playedOutCardSlotPack;


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
        public void StartGame(HoyPlayer[] players)
        {
            _hoyPlayers = players;
            _playerNodes = new ListNode<HoyPlayer>(players[0]);
            var playerNodesTemp = _playerNodes;
            for (int i = 1; i < players.Length; i++)
            {
                playerNodesTemp.Next = new ListNode<HoyPlayer>(players[i]);
                playerNodesTemp = playerNodesTemp.Next;
            }

            playerNodesTemp.Next = _playerNodes;

            NewPlayedCardSlotPack();
            CurrentGameState = GameState.DealingCards;
            _dealZoneBounds = new Bounds(dealZone.position, dealZone.localScale);
            InitCardDeck();

            //Deal cards to players
            DealCardsToPlayersFromDeck();
        }

        private void DealCardsToPlayersFromDeck()
        {
            List<List<Card>> cardPacks = new List<List<Card>>();
            cardPacks.Add(_cardsSpawned.GetRange(_cardsSpawned.Count - 9, 9));
            _cardsSpawned.RemoveRange(_cardsSpawned.Count - 9, 9);
            cardPacks.Add(_cardsSpawned.GetRange(_cardsSpawned.Count - 9, 9));
            _cardsSpawned.RemoveRange(_cardsSpawned.Count - 9, 9);
            StartCoroutine(DealCardsToPlayersRoutine(_hoyPlayers, cardPacks));

            IEnumerator DealCardsToPlayersRoutine(HoyPlayer[] players, List<List<Card>> listCards)
            {
                for (int i = 0; i < players.Length; i++)
                {
                    yield return StartCoroutine(DealCardsToOnePlayerRoutine((p, c) => p.TakeCard(c), players[i], listCards[i]));
                }
                
                CurrentGameState = GameState.PlayerTurn;
                WhosNextMove = _playerNodes.Value;
            }
        }

        [Server]
        private void NewPlayedCardSlotPack()
        {
            _playedOutCardSlotPack = new PlayedOutCardSlotPack(new Vector2(-7.9f, 1f), 0, 4.5f, 3f);
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

        private IEnumerator DealCardsToOnePlayerRoutine(Action<HoyPlayer, Card> dealAction, HoyPlayer player, IEnumerable<Card> cards)
        {
            foreach (Card card in cards)
            {
                dealAction(player, card);
                yield return new WaitForSeconds(card.cardDealMoveTime);
            }
        }

        [Server]
        public void DragEnded(Card card)
        {
            HoyPlayer player = card.connectionToClient.owned.First(_ => _.GetComponent<HoyPlayer>() != null).GetComponent<HoyPlayer>();
            if (!_dealZoneBounds.Contains(card.transform.position))
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
                    DealCardsToPlayersFromDeck();
                }
            } else
            {
                WhosNextMove = _playerNodes.Value;
                CurrentGameState = GameState.PlayerTurn;
            }
        }

        private IEnumerator GameOver()
        {
            CurrentGameState = GameState.GameOver;
            foreach (var player in _hoyPlayers)
            {
                player.RPCSetGameOverUI();
            }

            yield return StartCoroutine(CountPointsRoutine());
            var winnerOfRound = _hoyPlayers.OrderByDescending(_ => _.Score).First();
            var networkManager = HoyRoomNetworkManager.Singleton;
            var hoyRoomPlayers = networkManager.roomSlots.Cast<HoyRoomPlayer>();
            var roomPlayerOfWinner = hoyRoomPlayers.First(_ => _.PlayerName == winnerOfRound.PlayerName);
            roomPlayerOfWinner.Wins++;
            foreach (var hoyPlayer in _hoyPlayers)
            {
                foreach (var card in hoyPlayer.GetBank())
                {
                    NetworkServer.Destroy(card.gameObject);
                }
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
            networkManager.NextRound();
           
            IEnumerator CountPointsRoutine()
            {
                foreach (var player in _hoyPlayers)
                {
                    var target = player.transform.TransformPoint(Vector3.up * 6.5f);
                    int score = 0;
                    int orderInLayer = 0;
                    var cards = player.GetBank();
                    cards.Reverse();
                    foreach (var card in cards)
                    {
                        score += card.FaceType == CardFaceType.FInfinity ? 0 : card.Value;
                        var score1 = score;
                        card.SetTargetServer(target, () => player.RpcSetScore(score1, player.PlayerName));
                        card.RpcSetOrderInLayer(orderInLayer++);
                        yield return new WaitForSeconds(card.cardDealMoveTime);
                    }

                    player.Score = score;
                }
            }
        }

        [Server]
        private void PlayCard(Card card, HoyPlayer player)
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
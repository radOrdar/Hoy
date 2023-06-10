using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hoy.StaticData;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Hoy
{
    public class GameManager : NetworkBehaviour
    {
        public static GameManager singleton { get; private set; }

        [SerializeField] private Transform dealZone;
        [SerializeField] private Transform cardDeckSpawnTrans;
        [SerializeField] private CardStaticData[] cardStaticDatas;
        [SerializeField] private Card cardPf;

        private List<HoyPlayer> _hoyPlayers;
        private ListNode<HoyPlayer> _playerNodes; 
        // private int _lastPlayerMovedIndex;
        
        private List<Card> _cardsSpawned = new();
        // private int _nextCardIndex;

        [field: SyncVar]
        public GameState CurrentGameState { get; set; }

        [field: SyncVar(hook = nameof(OnWhosNextMoveChanged))]
        public HoyPlayer WhosNextMove { get; set; }

        private Bounds _dealZoneBounds;
        private PlayedOutCardSlotPack _playedOutCardSlotPack;

        private void Awake()
        {
            singleton = this;
        }

        [Server]
        public void StartGame(List<HoyPlayer> players)
        {
            _hoyPlayers = players;
            _playerNodes = new ListNode<HoyPlayer>(players[0]);
            var playerNodesTemp = _playerNodes;
            for (int i = 1; i < players.Count; i++)
            {
                playerNodesTemp.Next = new ListNode<HoyPlayer>(players[i]);
                playerNodesTemp = playerNodesTemp.Next;
            }
            playerNodesTemp.Next = _playerNodes;
            
            NewPlayedCardSlotPack();
            CurrentGameState = GameState.DealingCards;
            _dealZoneBounds = new Bounds(dealZone.position, dealZone.localScale);
            InitCardDeck();
            foreach (HoyPlayer hoyPlayer in players)
            {
                hoyPlayer.RPCGameStarted();
            }

            //Deal cards to players
            List<List<Card>> cardPacks = new List<List<Card>>();
            cardPacks.Add(_cardsSpawned.GetRange(_cardsSpawned.Count - 9, 9));
            _cardsSpawned.RemoveRange(_cardsSpawned.Count - 9, 9);
            cardPacks.Add(_cardsSpawned.GetRange(_cardsSpawned.Count - 9, 9));
            _cardsSpawned.RemoveRange(_cardsSpawned.Count - 9, 9);
            StartCoroutine(DealCardsToPlayersRoutine(_hoyPlayers, cardPacks));
            
            IEnumerator DealCardsToPlayersRoutine(List<HoyPlayer> players, List<List<Card>> listCards)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    yield return StartCoroutine(DealCardsToOnePlayerRoutine((p, c) => p.TakeCard(c),  players[i], listCards[i]));
                }
                // onComplete?.Invoke();
                CurrentGameState = GameState.PlayerTurn;
                WhosNextMove = _playerNodes.Value;
            }
        }

        [Server]
        private void NewPlayedCardSlotPack()
        {
            _playedOutCardSlotPack = new PlayedOutCardSlotPack(new Vector2(-7.9f, 1f), 0, 4.3f, -3.2f);
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

        // [Server]
        // private void ChangeGameState(GameState state)
        // {
        //     WhosNextMove = null;
        //     switch (state)
        //     {
        //         case GameState.PlayerTurn:
        //             // WhosNextMove = _hoyPlayers[NextPlayerIndex()];
        //             WhosNextMove = _playerNodes.Value;
        //             _playerNodes = _playerNodes.Next;
        //             CurrentGameState = GameState.PlayerTurn;
        //             break;
        //         case GameState.DealingCards:
        //             CurrentGameState = GameState.DealingCards;
        //             break;
        //     }
        // }
        
        // [Server]
        // private int NextPlayerIndex()
        // {
        //     _lastPlayerMovedIndex++;
        //     if (_lastPlayerMovedIndex >= _hoyPlayers.Count) _lastPlayerMovedIndex = 0;
        //     return _lastPlayerMovedIndex;
        // }

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
                    PlayCard(card);
                    // ChangeGameState(GameState.PlayerTurn);
                    CurrentGameState = GameState.PlayerTurn;
                    _playerNodes = _playerNodes.Next;
                    WhosNextMove = _playerNodes.Value;
                } else if (card.Value == _playedOutCardSlotPack.LastCard.Value)
                {
                    PlayCard(card);
                    // ChangeGameState(GameState.PlayerTurn);
                    CurrentGameState = GameState.PlayerTurn;
                    _playerNodes = _playerNodes.Next;
                    WhosNextMove = _playerNodes.Value;
                } else if (card.Value > _playedOutCardSlotPack.LastCard.Value)
                {
                    StartCoroutine(FromTableToBankRoutine(card, player));
                } else
                {
                    player.TakeCard(card);
                    card.netIdentity.RemoveClientAuthority();
                }
            }
        }

        [Server]
        private IEnumerator FromTableToBankRoutine(Card card, HoyPlayer player)
        {
            WhosNextMove = null;
            CurrentGameState = GameState.DealingCards;
            PlayCard(card);
            yield return new WaitForSeconds(2);
            List<Card> cards = _playedOutCardSlotPack.GetCards();
            yield return StartCoroutine(DealCardsToOnePlayerRoutine((p,c) => p.AddToBank(c), player, cards));
            WhosNextMove = _playerNodes.Value;
            CurrentGameState = GameState.PlayerTurn;
            NewPlayedCardSlotPack();
        }

        [Server]
        private void PlayCard(Card card)
        {
            card.netIdentity.RemoveClientAuthority();
            card.RpcShowCardToAllClients();
            _playedOutCardSlotPack.AddCard(card);
        }

        private void OnWhosNextMoveChanged(HoyPlayer oldValue, HoyPlayer newValue)
        {
            FindObjectOfType<UI>().SetMoveNextName(newValue != null ? newValue.PlayerName : null);
        }
    }
}
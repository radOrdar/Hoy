using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hoy.StaticData;
using Mirror;
using UnityEngine;

namespace Hoy
{
    public class GameManager : NetworkBehaviour
    {
        public static GameManager singleton { get; private set; }
        
        [SerializeField] private Transform dealZone;
        [SerializeField] private Transform cardDeckSpawnTrans;
        [SerializeField] private CardStaticData[] cardStaticDatas;
        [SerializeField] private Card cardPf;

        public List<HoyPlayer> hoyPlayers;

        private List<Card> _cardsSpawned = new();
        private int _nextCardIndex;
        
        [field:SyncVar] 
        public GameState CurrentGameState { get; set; }
        
        [field:SyncVar(hook = nameof(OnWhosNextMoveChanged))]
        public HoyPlayer WhosNextMove { get; set; }
        private Bounds _dealZoneBounds;
        private PlayedOutCardSlotPack _playedOutCardSlotPack;

        private void Awake()
        {
            singleton = this;
        }

        [Server]
        public void Init()
        {
            _playedOutCardSlotPack = new PlayedOutCardSlotPack(new Vector2(-7.9f, 1f), 0, 4.3f, -3.2f);
            ChangeGameState(GameState.DealingCards);
            _dealZoneBounds = new Bounds(dealZone.position, dealZone.localScale);
            InitCardDeck();
        }

        [Server]
        private void InitCardDeck()
        {
            SpawnCards();

            AssignDataToCard();
        }

        [Server]
        public void DealTheCards()
        {
            StartCoroutine(DealCardsRoutine());
        }

        [Server]
        private void SpawnCards()
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

            _nextCardIndex = _cardsSpawned.Count - 1;
        }

        [Server]
        private void AssignDataToCard()
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

        [Server]
        private IEnumerator DealCardsRoutine()
        {
            foreach (HoyPlayer hoyPlayer in hoyPlayers)
            {
                yield return StartCoroutine(DealCardsToPlayer(hoyPlayer, 9));
            }

            IEnumerator DealCardsToPlayer(HoyPlayer hoyPlayer, int amount)
            {
                for (int i = 0; i < amount; i++)
                {
                    hoyPlayer.TakeCard(_cardsSpawned[_nextCardIndex--]);
                    yield return new WaitForSeconds(_cardsSpawned[i].cardDealMoveTime);
                }
            }

            ChangeGameState(GameState.PlayerTurn);
        }

        [Server]
        private void ChangeGameState(GameState state)
        {
            switch (state)
            {
                case GameState.PlayerTurn:
                    WhosNextMove = hoyPlayers.First(_ => _ != WhosNextMove);
                    CurrentGameState = GameState.PlayerTurn;
                    break;
                case GameState.DealingCards:
                    CurrentGameState = GameState.DealingCards;
                    break;
            }
        }

        [Server]
        public void DragEnded(Card card)
        {
            var connToClient = card.connectionToClient;
            if (!_dealZoneBounds.Contains(card.transform.position))
            {
                connToClient.owned.First(_ => _.GetComponent<HoyPlayer>() != null).GetComponent<HoyPlayer>().TakeCard(card);
                card.netIdentity.RemoveClientAuthority();
            } else
            {
                if (_playedOutCardSlotPack.Count % 2 == 0)
                {
                    PlayCard(card);
                    ChangeGameState(GameState.PlayerTurn);
                }else if (card.Value >= _playedOutCardSlotPack.LastCard.Value)
                {
                    PlayCard(card);
                    ChangeGameState(GameState.PlayerTurn);
                } else
                {
                    connToClient.owned.First(_ => _.GetComponent<HoyPlayer>() != null).GetComponent<HoyPlayer>().TakeCard(card);
                    card.netIdentity.RemoveClientAuthority();
                }
            }
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
            if (newValue != null)
            {
                FindObjectOfType<UI>().SetMoveNextName(newValue.PlayerName);
                Debug.Log($"{newValue.PlayerName} turn");
            } else
            {
                Debug.Log("No player turn");
            }
        }
    }
}
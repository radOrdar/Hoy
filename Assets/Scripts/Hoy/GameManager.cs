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
        
        [field:SyncVar] 
        public GameState CurrentGameState { get; set; }
        
        [field:SyncVar(hook = nameof(OnPlayerTurnChanged))]
        public HoyPlayer PlayerWhosTurn { get; set; }
        private Bounds _dealZoneBounds;

        private void Awake()
        {
            singleton = this;
        }

        [Server]
        public void Init()
        {
            
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
            float currZ = 0;

            for (int i = 0; i < cardStaticDatas.Select(c => c.numberInDeck).Sum(); i++)
            {
                Card card = Instantiate(cardPf);
                NetworkServer.Spawn(card.gameObject);
                _cardsSpawned.Add(card);

                card.transform.position = cardDeckSpawnTrans.position + new Vector3(0, currY, currZ);
                currY -= 0.05f;
                currZ -= .01f;
            }
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
            yield return StartCoroutine(DealCardsToPlayer(35, 9, 0));
            yield return StartCoroutine(DealCardsToPlayer(26, 9, 1));

            IEnumerator DealCardsToPlayer(int startIndexCard, int amount, int playerIndex)
            {
                Vector3 playerPos = hoyPlayers[playerIndex].transform.position;
                var connToClient = hoyPlayers[playerIndex].connectionToClient;
                float horizDisplacement = -5;

                for (int i = startIndexCard; i > startIndexCard - amount; i--)
                {
                    _cardsSpawned[i].netIdentity.AssignClientAuthority(connToClient);
                }

                yield return new WaitForSeconds(0.1f);

                for (int i = startIndexCard; i > startIndexCard - amount; i--)
                {
                    _cardsSpawned[i].SetTarget(connToClient, playerPos + Vector3.right * horizDisplacement);
                    horizDisplacement += 1.25f;
                    yield return new WaitForSeconds(_cardsSpawned[i].cardDealMoveTime);
                }
            }

            ChangeGameState(GameState.PlayerTurn);
        }

        private void ChangeGameState(GameState state)
        {
            switch (state)
            {
                case GameState.PlayerTurn:
                    PlayerWhosTurn = hoyPlayers.First(_ => _ != PlayerWhosTurn);
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
            if (!_dealZoneBounds.Contains(card.transform.position))
            {
                var connToClient = card.connectionToClient;
                card.SetTarget(connToClient, connToClient.owned.First(_ => _.GetComponent<HoyPlayer>() != null).transform.position);
            }
        }

        private void OnPlayerTurnChanged(HoyPlayer oldValue, HoyPlayer newValue)
        {
            if (newValue != null)
            {
                Debug.Log($"{newValue.PlayerName} turn");
            } else
            {
                Debug.Log("No player turn");
            }
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hoy.Cards;
using Mirror;
using UnityEngine;

namespace Hoy
{
    class GameManagerMorePlayers : BaseGameManager
    {
        private Chip _chip;
        private int _nextPlayerIndex;

        protected override List<List<Card>> GetCardsPackToDeal()
        {
            List<List<Card>> cardPacks = new List<List<Card>>();
            int numPlayers = HoyRoomNetworkManager.Singleton.numPlayers;
            int amountCardsToOnePlayer = 36 / numPlayers;
            for (int i = 0; i < numPlayers; i++)
            {
                cardPacks.Add(_cardsSpawned.GetRange(_cardsSpawned.Count - amountCardsToOnePlayer, amountCardsToOnePlayer));
                _cardsSpawned.RemoveRange(_cardsSpawned.Count - amountCardsToOnePlayer, amountCardsToOnePlayer);
            }

            if (numPlayers == 5)
            {
                NetworkServer.Destroy(_cardsSpawned[0].gameObject);
                _cardsSpawned.Clear();
            }

            return cardPacks;
        }

        protected override void OnCardsDealed()
        {
            _nextPlayerIndex = Random.Range(0, _hoyPlayers.Count);
            WhosNextMove = _hoyPlayers[_nextPlayerIndex];
            _chip = FindObjectOfType<Chip>();
            AssignChipToLastPlayer();
        }

        public override void DragEnded(Card card)
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
                bool isSameCard = _playedOutCardSlotPack.Count > 0 && _playedOutCardSlotPack.LastCard.Value == card.Value;
                PlayCard(card, player);

                StartCoroutine(MoveDeciderRoutine(player, isSameCard));
            }
        }

        private IEnumerator MoveDeciderRoutine(HoyPlayer player, bool isSameCard)
        {
            bool hasChip = player == _chip.Owner;

            int playersWithCards = _hoyPlayers.Count(_ => !_.IsEmpty());
            if (playersWithCards == 0)
            {
                yield return StartCoroutine(FromTableToWinnerRoutine());
                Debug.Log("players with cards 0");
                StartCoroutine(GameOver());
                yield break;
            }

            if (hasChip && !isSameCard)
            {
                yield return StartCoroutine(FromTableToWinnerRoutine());

                if (playersWithCards == 1)
                {
                    var lastPlayer = _hoyPlayers.First(_ => !_.IsEmpty());
                    var lastPlayerHandCards = lastPlayer.GiveAwayCards();
                    foreach (var card in lastPlayerHandCards)
                    {
                        card.RpcShowCardToAllClients();
                    }

                    yield return StartCoroutine(DealCardsToOnePlayerRoutine((p, c) => p.AddToBank(c), lastPlayer, lastPlayerHandCards));
                    Debug.Log("players with cards 1");
                    StartCoroutine(GameOver());
                } else
                {
                    PrepareNewPlay();
                }

                yield break;
            }

            if (isSameCard && !player.IsEmpty())
            {
                _chip.Owner = player;
                player.TakeChip(_chip);
            }

            while (_hoyPlayers[NextPlayerIndex()].IsEmpty())
            { }

            WhosNextMove = _hoyPlayers[_nextPlayerIndex];
            CurrentGameState = GameState.PlayerTurn;
        }

        private int NextPlayerIndex() =>
            _nextPlayerIndex = (_nextPlayerIndex + 1) % _hoyPlayers.Count;

        private IEnumerator FromTableToWinnerRoutine()
        {
            WhosNextMove = null;
            CurrentGameState = GameState.DealingCards;
            yield return new WaitForSeconds(2);
            yield return StartCoroutine(DealCardsToOnePlayerRoutine((p, c) => p.AddToBank(c), _playedOutCardSlotPack.Winner, _playedOutCardSlotPack.GetCards()));
        }

        private void PrepareNewPlay()
        {
            _nextPlayerIndex = _hoyPlayers.IndexOf(_playedOutCardSlotPack.Winner);
            while (_hoyPlayers[_nextPlayerIndex].IsEmpty())
            {
                NextPlayerIndex();
            }

            // DeleteEmptyPlayers();
            AssignChipToLastPlayer();
            WhosNextMove = _hoyPlayers[_nextPlayerIndex];
            CurrentGameState = GameState.PlayerTurn;
            NewPlayedCardSlotPack();
        }

        private void AssignChipToLastPlayer()
        {
            int chipPlayerIndex = _nextPlayerIndex - 1;
            if (chipPlayerIndex < 0)
                chipPlayerIndex = _hoyPlayers.Count - 1;

            while (_hoyPlayers[chipPlayerIndex].IsEmpty())
            {
                chipPlayerIndex--;
                if (chipPlayerIndex < 0)
                    chipPlayerIndex = _hoyPlayers.Count - 1;
            }

            _hoyPlayers[chipPlayerIndex].TakeChip(_chip);
            _chip.Owner = _hoyPlayers[chipPlayerIndex];
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hoy.Cards;
using UnityEngine;

namespace Hoy
{
    class GameManagerMorePlayers : BaseGameManager
    {
        private Chip _chip;

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

            return cardPacks;
        }

        protected override void OnCardsDealed()
        {
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

                StartCoroutine(MoveDeciderRoutine(card, player, isSameCard));
            }
        }

        private IEnumerator MoveDeciderRoutine(Card card, HoyPlayer player, bool isSameCard)
        {
            bool hasChip = player == _chip.Owner;

            int playersWithCards = _hoyPlayers.Count(_ => !_.IsEmpty());
            if (hasChip && !isSameCard)
            {
                yield return StartCoroutine(FromTableToWinnerRoutine());
                if (playersWithCards == 1)
                {
                    var lastPlayer = _hoyPlayers.First(_ => !_.IsEmpty());
                    yield return StartCoroutine(DealCardsToOnePlayerRoutine((p, c) => p.AddToBank(c), lastPlayer, lastPlayer.GiveAwayCards()));
                    StartCoroutine(GameOver());
                }

                yield break;
            }

            if (playersWithCards == 0)
            {
                yield return StartCoroutine(FromTableToWinnerRoutine());
                StartCoroutine(GameOver()); 
                yield break;
            }

            if (isSameCard)
            {
                _chip.Owner = player;
                player.TakeChip(_chip);
            }

            DeleteAllEmptyPlayers();
            _playerNodes = _playerNodes.Next;
            WhosNextMove = _playerNodes.Value;
            CurrentGameState = GameState.PlayerTurn;
        }

        private IEnumerator FromTableToWinnerRoutine()
        {
            WhosNextMove = null;
            CurrentGameState = GameState.DealingCards;
            yield return new WaitForSeconds(2);
            yield return StartCoroutine(DealCardsToOnePlayerRoutine((p, c) => p.AddToBank(c), _playedOutCardSlotPack.Winner, _playedOutCardSlotPack.GetCards()));
            while (_playerNodes.Value != _playedOutCardSlotPack.Winner)
                _playerNodes = _playerNodes.Next;
            while (_playerNodes.Value.IsEmpty())
                _playerNodes = _playerNodes.Next;
            DeleteAllEmptyPlayers();
            AssignChipToLastPlayer();
            WhosNextMove = _playerNodes.Value;
            CurrentGameState = GameState.PlayerTurn;
            NewPlayedCardSlotPack();
        }

        private void DeleteAllEmptyPlayers()
        {
            var nodesTemp = _playerNodes;
            while (nodesTemp.Next != _playerNodes)
            {
                if (nodesTemp.Next.Value.IsEmpty())
                {
                    nodesTemp.Next = nodesTemp.Next.Next;
                } else
                {
                    nodesTemp = nodesTemp.Next;
                }
            }
        }

        private void AssignChipToLastPlayer()
        {
            var lastPlayerToMove = _playerNodes;
            while (lastPlayerToMove.Next != _playerNodes)
            {
                lastPlayerToMove = lastPlayerToMove.Next;
            }

            lastPlayerToMove.Value.TakeChip(_chip);
            _chip.Owner = lastPlayerToMove.Value;
        }
    }
}
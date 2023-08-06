using System.Collections.Generic;
using Hoy.Cards;

namespace Hoy
{
    class GameManagerMorePlayers : BaseGameManager
    {
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
    }
}
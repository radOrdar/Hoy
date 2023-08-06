using System.Collections.Generic;
using Hoy.Cards;

namespace Hoy
{
    class GameManager2Players : BaseGameManager
    {
        protected override List<List<Card>> GetCardsPackToDeal()
        {
            List<List<Card>> cardPacks = new List<List<Card>>();
            cardPacks.Add(_cardsSpawned.GetRange(_cardsSpawned.Count - 9, 9));
            _cardsSpawned.RemoveRange(_cardsSpawned.Count - 9, 9);
            cardPacks.Add(_cardsSpawned.GetRange(_cardsSpawned.Count - 9, 9));
            _cardsSpawned.RemoveRange(_cardsSpawned.Count - 9, 9);
            return cardPacks;
        }
    }
}
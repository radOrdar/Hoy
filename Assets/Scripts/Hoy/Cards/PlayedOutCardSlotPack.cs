using System.Collections.Generic;
using System.Linq;
using Hoy.Services;
using UnityEngine;

namespace Hoy.Cards
{
    public class PlayedOutCardSlotPack
    {
        private Vector2 startPoint;
        private int _orderInLayer;
        private float _horizontalOffset;
        private float _verticalOffset;

        private List<Card> _cards = new();
        private int _nextOrderInLayer;
        private float _nextHorizontalOffset;
        
        public HoyPlayer Winner { get; private set; }

        public PlayedOutCardSlotPack(Vector2 startPoint, int orderInLayer, float horizontalOffset, float verticalOffset)
        {
            this.startPoint = startPoint;
            _orderInLayer = orderInLayer;
            _horizontalOffset = horizontalOffset;
            _verticalOffset = verticalOffset;
            _nextOrderInLayer = _orderInLayer;
        }

        public int Count => _cards.Count;
        public Card LastCard => _cards[^1];

        public void AddCard(Card card, HoyPlayer player)
        {
            if (_cards.All(c => card.Value >= c.Value))
            {
                Winner = player;
            }

            card.RpcSetOrderInLayer(_nextOrderInLayer);
            _nextOrderInLayer++;
            card.SetTargetServer(startPoint + new Vector2(_nextHorizontalOffset, Mathf.Sign(player.transform.position.y) * _verticalOffset));
            AudioService.Instance.RpcPlayOneShotDelayed(AudioSfxType.PlayTable, card.cardDealMoveTime - 0.1f);
            _cards.Add(card);

            if (_cards.Count % 2 == 0)
            {
                _nextHorizontalOffset += _horizontalOffset;
            }
        }

        public List<Card> GetCards()
        {
            return new List<Card>(_cards);
        }
    }
}
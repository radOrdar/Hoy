using System.Collections.Generic;
using UnityEngine;

namespace Hoy
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
        private float _nextVerticalOffset;

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

        public void AddCard(Card card)
        {
            card.RpcSetOrderInLayer(_nextOrderInLayer);
            _nextOrderInLayer++;
            card.SetTargetServer(startPoint + new Vector2(_nextHorizontalOffset, _nextVerticalOffset));
            _cards.Add(card);
            
            if (_cards.Count % 2 == 0)
            {
                _nextHorizontalOffset += _horizontalOffset;
                _nextVerticalOffset = 0;
            } else
            {
                _nextVerticalOffset = _verticalOffset;
            }
            
        }
    }
}

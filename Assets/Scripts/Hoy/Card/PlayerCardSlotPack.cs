using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Hoy
{
    public class PlayerCardSlotPack
    {
        private Vector2 _initialPoint;
        private Vector2 _horizontalOffset;
        private int _orderInLayer;
        private NetworkConnectionToClient _connectionToClient;

        private List<Card> _bank = new();
        private int bankOrderInLayer;

        private List<Card> _cards = new();
        private int _nextOrderInLayer;

        public PlayerCardSlotPack(Vector2 initialPoint, Vector2 horizontalOffset, int orderInLayer, NetworkConnectionToClient connectionToClient)
        {
            _initialPoint = initialPoint;
            _horizontalOffset = horizontalOffset;
            _orderInLayer = orderInLayer;
            _connectionToClient = connectionToClient;

            _nextOrderInLayer = orderInLayer;
        }

        public void AddCard(Card card)
        {
            card.RpcSetOrderInLayer(_nextOrderInLayer++);
            card.SetTargetServer(_initialPoint + _horizontalOffset * _cards.Count, () => card.netIdentity.AssignClientAuthority(_connectionToClient));
            _cards.Add(card);
        }

        public void DeleteCard(Card card)
        {
            _cards.Remove(card);
            _nextOrderInLayer = _orderInLayer;

            for (int i = 0; i < _cards.Count; i++)
            {
                _cards[i].RpcSetOrderInLayer(_nextOrderInLayer++);
                _cards[i].RpcSetTargetOnLocalPlayer(_connectionToClient, _initialPoint + _horizontalOffset * i);
            }
        }

        public void AddToBank(Card card)
        {
            card.RpcSetOrderInLayer(bankOrderInLayer++);
            card.SetTargetServer(_initialPoint - _horizontalOffset.normalized * 6);
            _bank.Add(card);
        }

        public bool IsEmpty() => _cards.Count == 0;
    }
}
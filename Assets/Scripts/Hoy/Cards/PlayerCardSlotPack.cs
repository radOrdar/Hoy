using System.Collections.Generic;
using Hoy.Services;
using Mirror;
using UnityEngine;

namespace Hoy.Cards
{
    public class PlayerCardSlotPack
    {
        private readonly Vector3 _upDirection;
        private readonly Vector2 _initialPoint;
        private readonly Vector2 _horizontalOffset;
        private readonly int _orderInLayer;
        private readonly NetworkConnectionToClient _connectionToClient;

        public readonly List<Card> Bank = new();
        private int _bankOrderInLayer;

        public readonly List<Card> Cards = new();
        private int _nextOrderInLayer;

        public PlayerCardSlotPack(Vector3 upDirection, Vector2 initialPoint, Vector2 horizontalOffset, int orderInLayer, NetworkConnectionToClient connectionToClient)
        {
            _upDirection = upDirection;
            _initialPoint = initialPoint;
            _horizontalOffset = horizontalOffset;
            _orderInLayer = orderInLayer;
            _connectionToClient = connectionToClient;

            _nextOrderInLayer = orderInLayer;
        }

        public void AddCard(Card card)
        {
            card.RpcSetOrderInLayer(_nextOrderInLayer++);
            card.transform.up = _upDirection;
            var fluctuation = card.transform.TransformDirection(new Vector2(Random.Range(-.04f, .04f),0));
            card.transform.up = _upDirection + fluctuation;
            card.SetTargetServer(_initialPoint + _horizontalOffset * Cards.Count, () => card.netIdentity.AssignClientAuthority(_connectionToClient));
            AudioService.Instance.RpcPlayOneShotDelayed(AudioSfxType.DealPlayer, card.cardDealMoveTime - 0.1f);
            Cards.Add(card);
        }

        public void DeleteCard(Card card)
        {
            Cards.Remove(card);
            _nextOrderInLayer = _orderInLayer;

            for (int i = 0; i < Cards.Count; i++)
            {
                Cards[i].RpcSetOrderInLayer(_nextOrderInLayer++);
                Cards[i].SetTargetServer(_initialPoint + _horizontalOffset * i);
            }
        }

        public void AddToBank(Card card)
        {
            card.transform.up = _upDirection;
            var fluctuation = card.transform.TransformDirection(new Vector2(Random.Range(-.04f, .04f),0));
            card.transform.up = _upDirection + fluctuation;
            card.RpcSetOrderInLayer(_bankOrderInLayer++);
            card.SetTargetServer(_initialPoint - _horizontalOffset.normalized * 5);
            AudioService.Instance.RpcPlayOneShotDelayed(AudioSfxType.TakeBank, card.cardDealMoveTime - 0.1f);
            Bank.Add(card);
        }

        public bool IsEmpty() => Cards.Count == 0;

        // public List<Card> GetBank() => 
        //     Bank;
        public void Clear()
        {
            foreach (var card in Cards)
            {
                card.netIdentity.RemoveClientAuthority();
            }
            Cards.Clear();
        }
    }
}
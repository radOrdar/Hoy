using System.Collections.Generic;
using Hoy.Services;
using Mirror;
using UnityEngine;

namespace Hoy.Cards
{
    public class PlayerCardSlotPack
    {
        private readonly Vector3 _upDirection;
        private readonly Vector2 _packCenter;
        private readonly Vector2 _horizontalOffset;
        private readonly int _orderInLayer;
        private readonly NetworkConnectionToClient _connectionToClient;
        private readonly float _bankOffsetCoeff;

        public readonly List<Card> Bank = new();
        private int _bankOrderInLayer;

        public readonly List<Card> Cards = new();
        private int _nextOrderInLayer;

        public PlayerCardSlotPack(Vector3 upDirection, Vector2 packCenter, Vector2 horizontalOffset, int orderInLayer, NetworkConnectionToClient connectionToClient, float bankOffsetCoeff)
        {
            _upDirection = upDirection;
            _packCenter = packCenter;
            _horizontalOffset = horizontalOffset;
            _orderInLayer = orderInLayer;
            _connectionToClient = connectionToClient;
            _nextOrderInLayer = orderInLayer;
            _bankOffsetCoeff = bankOffsetCoeff;
        }

        public void AddCard(Card card)
        {
            card.RpcSetOrderInLayer(_nextOrderInLayer++);
            card.transform.up = _upDirection;
            var fluctuation = card.transform.TransformDirection(new Vector2(Random.Range(-.04f, .04f),0));
            card.transform.up = _upDirection + fluctuation;
            Cards.Add(card);
            float halfCards = Cards.Count / 2f;
            var initPoint = _packCenter -(halfCards - 0.5f) * _horizontalOffset;
            for (int i = 0; i < Cards.Count; i++)
            {
                Cards[i].RpcSetOrderInLayer(_nextOrderInLayer++);
                Cards[i].SetTargetServer(initPoint + _horizontalOffset * i, i == Cards.Count - 1 ? () => card.netIdentity.AssignClientAuthority(_connectionToClient) : null);
            }
            AudioService.Instance.RpcPlayOneShotDelayed(AudioSfxType.DealPlayer, card.cardDealMoveTime - 0.1f);
        }

        public void DeleteCard(Card card)
        {
            Cards.Remove(card);
            _nextOrderInLayer = _orderInLayer;
            
            float halfCards = Cards.Count / 2f;
            var initPoint =_packCenter - (halfCards - 0.5f) * _horizontalOffset;
            for (int i = 0; i < Cards.Count; i++)
            {
                Cards[i].RpcSetOrderInLayer(_nextOrderInLayer++);
                Cards[i].SetTargetServer(initPoint + _horizontalOffset * i);
            }
        }

        public void AddToBank(Card card)
        {
            card.transform.up = _upDirection;
            var fluctuation = card.transform.TransformDirection(new Vector2(Random.Range(-.04f, .04f),0));
            card.transform.up = _upDirection + fluctuation;
            card.RpcSetOrderInLayer(_bankOrderInLayer++);
            card.SetTargetServer(_packCenter + _bankOffsetCoeff * _horizontalOffset);
            AudioService.Instance.RpcPlayOneShotDelayed(AudioSfxType.TakeBank, card.cardDealMoveTime - 0.1f);
            Bank.Add(card);
        }

        public bool IsEmpty() => Cards.Count == 0;
        
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
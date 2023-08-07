using System.Collections.Generic;
using System.Linq;
using Hoy.Services;
using UnityEngine;

namespace Hoy.Cards
{
    public class PlayedOutCardSlotPack
    {
        private readonly float _horizontalOffset;

        private readonly List<Card> _cards = new();
        private readonly Dictionary<HoyPlayer, List<Card>> _playersCardPacks = new();
        private int _nextOrderInLayer;
        private float _nextHorizontalOffset;

        public HoyPlayer Winner { get; private set; }

        public PlayedOutCardSlotPack(int orderInLayer, float horizontalOffset)
        {
            _horizontalOffset = horizontalOffset;
            _nextOrderInLayer = orderInLayer;
        }

        public int Count => _cards.Count;
        public Card LastCard => _cards[^1];

        public void AddCard(Card card, HoyPlayer player)
        {
            if (_cards.All(c => card.Value >= c.Value))
            {
                Winner = player;
            }

            _cards.Add(card);

            card.RpcSetOrderInLayer(_nextOrderInLayer);
            _nextOrderInLayer++;

            if (!_playersCardPacks.ContainsKey(player))
            {
                _playersCardPacks[player] = new List<Card>();
            }
            var playerPack = _playersCardPacks[player];
            playerPack.Add(card);
            float dealZoneRadius = BaseGameManager.Instance.DealZone.localScale.x / 2;
            Vector3 initPoint = player.transform.position.normalized * dealZoneRadius;
            Vector3 localLeftDir = player.transform.TransformDirection(Vector3.left);
            Vector3 startPoint = initPoint + localLeftDir * _horizontalOffset * (playerPack.Count / 2f - 0.5f);
            for (int i = 0; i < playerPack.Count; i++)
            {
                playerPack[i].SetTargetServer(startPoint - localLeftDir * _horizontalOffset * i);
            }

            AudioService.Instance.RpcPlayOneShotDelayed(AudioSfxType.PlayTable, card.cardDealMoveTime - 0.1f);
        }

        public List<Card> GetCards()
        {
            return new List<Card>(_cards);
        }
    }
}
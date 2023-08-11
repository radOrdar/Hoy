using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Hoy.Cards;
using Hoy.Helpers;
using Mirror;
using UnityEngine;

/*
	Documentation: https://mirror-networking.gitbook.io/docs/guides/networkbehaviour
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkBehaviour.html
*/

// NOTE: Do not put objects in DontDestroyOnLoad (DDOL) in Awake.  You can do that in Start instead.

namespace Hoy
{
    public class HoyPlayer : NetworkBehaviour
    {
        [SerializeField] private float _horizontalOffset;

        [field: SyncVar]
        public string PlayerName { get; set; }
        [field: SyncVar] public int Score { get; set; }
        private PlayerCardSlotPack _playerCardSlotPack;

        /// <summary>
        /// This is invoked for NetworkBehaviour objects when they become active on the server.
        /// <para>This could be triggered by NetworkServer.Listen() for objects in the scene, or by NetworkServer.Spawn() for objects that are dynamically created.</para>
        /// <para>This will be called for objects on a "host" as well as for object on a dedicated server.</para>
        /// </summary>
        public override void OnStartServer()
        {
            transform.rotation = Quaternion.LookRotation(Vector3.forward, -transform.position);

            int numPlayers = HoyRoomNetworkManager.Singleton.numPlayers;
            float coeff = numPlayers == 2 ? 4.5f : 18f / numPlayers;

            Vector2 initialPoint = new Vector2(-coeff * _horizontalOffset + _horizontalOffset / 2, 0);
            _playerCardSlotPack = new PlayerCardSlotPack(transform.up, transform.TransformPoint(initialPoint), transform.right * _horizontalOffset, 10, connectionToClient);
        }

        [ClientRpc]
        public void RPCSetGameOverUI()
        {
            var ui = FindObjectOfType<UI>();
            ui.DeactivateWhosMoveNameText();
            ui.ActivateScores();
        }

        [Server]
        public void TakeCard(Card card)
        {
            _playerCardSlotPack.AddCard(card);
        }

        [Server]
        public void AddToBank(Card card)
        {
            _playerCardSlotPack.AddToBank(card);
        }

        [Command]
        public void CmdOnStartDrag(Card card)
        {
            _playerCardSlotPack.DeleteCard(card);
        }

        [Server]
        public bool IsEmpty()
        {
            return _playerCardSlotPack.IsEmpty();
        }

        public List<Card> GetBank() =>
            _playerCardSlotPack.Bank;

        public List<Card> GiveAwayCards()
        {
            var cards = _playerCardSlotPack.Cards;
            _playerCardSlotPack.Clear();
            return cards;
        }

        [ClientRpc]
        public void RpcSetScore(int score, string playerName)
        {
            var ui = FindObjectOfType<UI>();
            if (NetworkClient.localPlayer.GetComponent<HoyPlayer>().PlayerName == playerName)
            {
                ui.SetPlayerScore(score);
            } else
            {
                ui.SetFoeScore(score);
            }
        }

        [ClientRpc]
        public void RpcShowWinner(HoyPlayer winnerOfRound)
        {
            var ui = FindObjectOfType<UI>();
            ui.DeactivatePlayerNames();
            ui.DeactivateScoreTexts();
            ui.ShowWinner(winnerOfRound.PlayerName, winnerOfRound.Score);
        }

        [ClientRpc]
        public void RpcShowSeriesStat()
        {
            var ui = FindObjectOfType<UI>();
            ui.ShowSeriesStat(HoyRoomNetworkManager.Singleton.roomSlots.Cast<HoyRoomPlayer>().ToArray());
        }

        [ClientRpc]
        public void RpcGameOver()
        {
            var ui = FindObjectOfType<UI>();
            ui.ShowGameOver();
        }

        [TargetRpc]
        public void TargetGameStarted()
        {
            FindObjectOfType<CameraParent>().transform.rotation =
                Quaternion.LookRotation(-transform.position, -Vector3.forward);
        }

        public void TakeChip(Chip newChip)
        {
            newChip.transform.DOMove(transform.position + transform.TransformDirection(new Vector2(5f, 5f)), .5f);
        }
    }
}
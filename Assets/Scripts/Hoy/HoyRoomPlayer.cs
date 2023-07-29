﻿using System;
using System.Linq;
using Mirror;
using UnityEngine;

/*
	Documentation: https://mirror-networking.gitbook.io/docs/components/network-room-player
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkRoomPlayer.html
*/

namespace Hoy
{
    /// <summary>
    /// This component works in conjunction with the NetworkRoomManager to make up the multiplayer room system.
    /// The RoomPrefab object of the NetworkRoomManager must have this component on it.
    /// This component holds basic room player data required for the room to function.
    /// Game specific data for room players can be put in other components on the RoomPrefab or in scripts derived from NetworkRoomPlayer.
    /// </summary>
    public class HoyRoomPlayer : NetworkRoomPlayer
    {
        [SyncVar] public bool isLeader;
        [SyncVar] public bool allPlayersReady;
        [SyncVar] public int numOfRounds = 1;
        [field: SyncVar] public string PlayerName { get; set; }

        #region Optional UI

        public override void OnGUI()
        {
            if (!Utils.IsSceneActive(HoyRoomNetworkManager.Singleton.RoomScene))
                return;
          
            
            HoyRoomPlayer leaderPlayer = (HoyRoomPlayer)HoyRoomNetworkManager.Singleton.roomSlots.FirstOrDefault(_ => ((HoyRoomPlayer)_).isLeader);

            
            GUILayout.BeginArea(new Rect(20f + (index * 150), 150f, 140f, 130f));

            GUILayout.Label(PlayerName);

            if (readyToBegin)
                GUILayout.Label("Ready");
            else
                GUILayout.Label("Not Ready");

            if (isLocalPlayer)
            {
                if (readyToBegin)
                {
                    if (GUILayout.Button("Cancel"))
                        CmdChangeReadyState(false);
                } else
                {
                    if (GUILayout.Button("Ready"))
                        CmdChangeReadyState(true);
                }
            }
            if (!isLeader && leaderPlayer != null && leaderPlayer.isOwned && GUILayout.Button("REMOVE"))
            {
                // This button only shows on the Host for all players other than the Host
                // Host and Players can't remove themselves (stop the client instead)
                // Host can kick a Player this way.
                leaderPlayer.CmdDisconnect(this);
            }

            GUILayout.EndArea();
            
            

            GUILayout.BeginArea(new Rect(20f, 250f, 140f, 330f));

            if (isLeader && leaderPlayer && leaderPlayer.isOwned)
            {
                GUILayout.Label("Enter Number of Rounds");
                string numberOfRoundsText = numOfRounds.ToString();
                numberOfRoundsText = GUILayout.TextField(numberOfRoundsText, 1);
                Int32.TryParse(numberOfRoundsText, out int numOfR);
                if (numOfR != 0)
                    numOfRounds = numOfR;

                if (allPlayersReady && GUILayout.Button("START GAME"))
                {
                    CmdStartGame();
                }
            }
            
            GUILayout.EndArea();
            
            GUILayout.BeginArea(new Rect(20f, 350f, 140f, 330f));
            GUILayout.Label("Number of rounds");
            GUILayout.Label(numOfRounds.ToString());
            GUILayout.EndArea();
        }

        [Command]
        private void CmdStartGame()
        {
            HoyRoomNetworkManager.Singleton.StartGame();
        }

        #endregion

        [Command]
        private void CmdDisconnect(HoyRoomPlayer hoyRoomPlayer)
        {
            hoyRoomPlayer.GetComponent<NetworkIdentity>().connectionToClient.Disconnect();
        }
    }
}
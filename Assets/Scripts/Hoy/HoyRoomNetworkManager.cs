using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hoy.Helpers;
using Mirror;
using Udar.SceneManager;
using UnityEditor;
using UnityEngine;

/*
	Documentation: https://mirror-networking.gitbook.io/docs/components/network-room-manager
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkRoomManager.html

	See Also: NetworkManager
	Documentation: https://mirror-networking.gitbook.io/docs/components/network-manager
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkManager.html
*/

namespace Hoy
{
    /// <summary>
    /// This is a specialized NetworkManager that includes a networked room.
    /// The room has slots that track the joined players, and a maximum player count that is enforced.
    /// It requires that the NetworkRoomPlayer component be on the room player objects.
    /// NetworkRoomManager is derived from NetworkManager, and so it implements many of the virtual functions provided by the NetworkManager class.
    /// </summary>
    public class HoyRoomNetworkManager : NetworkRoomManager
    {
        [SerializeField] private SerializableDictionary<int, SceneField> gameplayScenes;

        private List<string> namesList = new() { "Snow", "Serenity", "Nuzzle", "Climax", "Saki", "Eve", "Zlatan", "Remy" };
        // Overrides the base singleton so we don't
        // have to cast to this type everywhere.
        public static HoyRoomNetworkManager Singleton => (HoyRoomNetworkManager)singleton;
        public HoyRoomPlayer LeaderPlayer { get; set; }

        private int _numOfGameplayers;


        #region Server System Callbacks

        public override void OnRoomServerSceneChanged(string sceneName)
        {
            if (sceneName == RoomScene)
            {
                _numOfGameplayers = 0;
                if (LeaderPlayer != null && LeaderPlayer.CurrentRound > 0)
                {
                    if (LeaderPlayer.NumOfRounds > LeaderPlayer.CurrentRound)
                    {
                        StartCoroutine(StartNextRound());
                        LeaderPlayer.CurrentRound++;
                    } else
                    {
                        LeaderPlayer.CurrentRound = 0;
                        foreach (var roomPlayer in roomSlots.Cast<HoyRoomPlayer>())
                        {
                            roomPlayer.Wins = 0;
                        }
                    }
                }
            }
        }

        private IEnumerator StartNextRound()
        {
            yield return new WaitForSeconds(1f);
            StartGame();
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            if (numPlayers > 5)
            {
                conn.Disconnect();
                return;
            }

            base.OnServerAddPlayer(conn);
            Debug.Log("server add player");
            var roomPlayer = conn.identity.GetComponent<HoyRoomPlayer>();
            if (numPlayers == 1)
            {
                roomPlayer.isLeader = true;
                LeaderPlayer = roomPlayer;
            }

            string nameToChose = namesList[Random.Range(0, namesList.Count)];
            roomPlayer.PlayerName = nameToChose;
            namesList.Remove(nameToChose);
        }

        [Server]
        private IEnumerator StartGameRoutine()
        {
            yield return new WaitForSeconds(0.2f);
            var gamePlayers = FindObjectsOfType<HoyPlayer>();
            Debug.Log($"{gamePlayers.Length} gameplayers on Scene");
            StartCoroutine(BaseGameManager.Instance.StartGame(gamePlayers));
        }


        public override bool OnRoomServerSceneLoadedForPlayer(NetworkConnectionToClient conn, GameObject roomPlayer, GameObject gamePlayer)
        {
            if (gameplayScenes.Values.Any(_ => Utils.IsSceneActive(_.Name)))
            {
                StartCoroutine(SetupGamePlayerRoutine(roomPlayer, gamePlayer));
                _numOfGameplayers++;
                if (_numOfGameplayers == numPlayers)
                    StartCoroutine(StartGameRoutine());
            }

            return base.OnRoomServerSceneLoadedForPlayer(conn, roomPlayer, gamePlayer);
        }

        private IEnumerator SetupGamePlayerRoutine(GameObject roomPlayer, GameObject gamePlayer)
        {
            int prevPosIndex = startPositionIndex == 0 ? startPositions.Count - 1 : startPositionIndex - 1;
            yield return new WaitForSeconds(.2f);
            HoyPlayer hoyPlayer = gamePlayer.GetComponent<HoyPlayer>();
            hoyPlayer.PlayerName = roomPlayer.GetComponent<HoyRoomPlayer>().PlayerName;
            Debug.Log($"SETUP GAME PLAYER {hoyPlayer.PlayerName}");
            startPositions[prevPosIndex].GetComponent<StartPos>().RpcSetText(hoyPlayer.PlayerName);
        }

        #endregion

        /// <summary>
        /// This is called when a server is stopped - including when a host is stopped.
        /// </summary>
        public override void OnStopServer()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#endif
            Application.Quit();
        }

        /// <summary>
        /// This is called when a client is stopped.
        /// </summary>
        public override void OnStopClient()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#endif
            Application.Quit();
        }

        public override void OnRoomServerPlayersReady()
        {
            LeaderPlayer.allPlayersReady = true;
        }

        public override void OnRoomServerPlayersNotReady()
        {
            LeaderPlayer.allPlayersReady = false;
        }

        public override void OnGUI()
        {
            if (!showRoomGUI)
                return;

            if (NetworkServer.active && gameplayScenes.Values.Any(_ => Utils.IsSceneActive(_.Name)))
            {
                GUILayout.BeginArea(new Rect(Screen.width - 150f, 10f, 140f, 30f));
                if (GUILayout.Button("Return to Room"))
                    ServerChangeScene(RoomScene);
                GUILayout.EndArea();
            }
        }

        public void StartGame()
        {
            ServerChangeScene(gameplayScenes[numPlayers].Name);
        }

        public void NextRound()
        {
            ServerChangeScene(RoomScene);
        }
    }
}
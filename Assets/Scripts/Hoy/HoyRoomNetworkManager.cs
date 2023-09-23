using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        private readonly List<string> _namesList = new() { "Snow", "Serenity", "Nuzzle", "Climax", "Saki", "Eve", "Zlatan", "Remy" };
        private HashSet<string> _occupiedNames = new();
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
                RecalculateRoomPlayerIndices();
                _numOfGameplayers = 0;
                if (LeaderPlayer == null && roomSlots.Count > 0)
                {
                    LeaderPlayer =(HoyRoomPlayer) roomSlots[0];
                    LeaderPlayer.isLeader = true;
                }
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
            if (LeaderPlayer == null)
            {
                roomPlayer.isLeader = true;
                LeaderPlayer = roomPlayer;
            }

            string nameToChose = _namesList.Except(_occupiedNames).ToList()[Random.Range(0, _namesList.Count)];
            roomPlayer.PlayerName = nameToChose;
            _occupiedNames.Add(nameToChose);
        }

        [Server]
        private IEnumerator StartGameRoutine()
        {
            yield return new WaitForSeconds(0.2f);
            var gamePlayers = FindObjectsOfType<HoyPlayer>();
            Debug.Log($"{gamePlayers.Length} gameplayers on Scene");
            StartCoroutine(BaseGameManager.Instance.StartGame(gamePlayers.OrderBy(_ => _.transform.GetSiblingIndex()).ToList()));
        }


        public override bool OnRoomServerSceneLoadedForPlayer(NetworkConnectionToClient conn, GameObject roomPlayer, GameObject gamePlayer)
        {
            if (gameplayScenes.Values.Any(_ => Utils.IsSceneActive(_.Path)))
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

        public override async void OnRoomServerDisconnect(NetworkConnectionToClient conn)
        {
            var roomPlayer = conn.owned.First(_ => _.GetComponent<HoyRoomPlayer>()).GetComponent<HoyRoomPlayer>();
            _occupiedNames.Remove(roomPlayer.PlayerName);
            await Task.Delay(500);
            if (LeaderPlayer == null)
            {
                if (numPlayers > 0)
                {
                    LeaderPlayer = (HoyRoomPlayer)roomSlots.First(_ => _ != null);
                    LeaderPlayer.isLeader = true;
                }
            }
            if (gameplayScenes.Values.Any(_ => Utils.IsSceneActive(_.Path)))
            {
                await Task.Delay(500);
                LeaderPlayer.CurrentRound = LeaderPlayer.NumOfRounds;
                ServerChangeScene(RoomScene);
            }
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
            // if (!showRoomGUI)
            //     return;
            //
            // if (NetworkServer.active && gameplayScenes.Values.Any(_ => Utils.IsSceneActive(_.Path)))
            // {
            //     GUILayout.BeginArea(new Rect(Screen.width - 150f, 10f, 140f, 30f));
            //     if (GUILayout.Button("Return to Room"))
            //         ServerChangeScene(RoomScene);
            //     GUILayout.EndArea();
            // }
        }

        public void StartGame()
        {
            ServerChangeScene(gameplayScenes[numPlayers].Path);
        }

        public void NextRound()
        {
            ServerChangeScene(RoomScene);
        }
    }
}
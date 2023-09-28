using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace Hoy.Helpers
{
    [RequireComponent(typeof(Button))]
    public class PlayRoomButton : MonoBehaviour
    {
        [SerializeField] private string networkAddress;

        private NetworkManager _netManager;
    
        private void Start()
        {
            _netManager = NetworkManager.singleton;
            GetComponent<Button>().onClick.AddListener(EnterPlayRoom);
            _netManager.networkAddress = networkAddress;
        }

        private void EnterPlayRoom()
        {
            if (!NetworkClient.active)
            {
                _netManager.StartClient();
            }
        }

        private void OnGUI()
        {
            if (NetworkClient.active)
            {
                GUILayout.Label($"Connecting to {_netManager.networkAddress}..");
            }
        }
    }
}

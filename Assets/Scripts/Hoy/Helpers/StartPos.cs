using Mirror;
using TMPro;
using UnityEngine;

namespace Hoy.Helpers
{
    public class StartPos : NetworkBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;

        [ClientRpc]
        public void RpcSetText(string text)
        {
            _text.SetText(text);
        }
    }
}
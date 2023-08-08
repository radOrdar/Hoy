using TMPro;
using UnityEngine;

namespace Hoy
{
    public class PlayerNameUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        public int id;

        public void SetPlayerName(string playerName)
        {
            text.SetText(playerName);
        }
    }
}
using TMPro;
using UnityEngine;

namespace Hoy
{
   public class UI : MonoBehaviour
   {
      [SerializeField] private GameObject waitPlayerPanel;
      [SerializeField] private TextMeshProUGUI bottomPlayerNameText;
      [SerializeField] private TextMeshProUGUI topPlayerNameText;
      [SerializeField] private TextMeshProUGUI whosMoveNameText;

      private void Awake()
      {
         waitPlayerPanel.SetActive(true);
      }

      public void DeactivateWaitPanel()
      {
         waitPlayerPanel.SetActive(false);
      }

      public void SetLocalPlayerName(string newName)
      {
         Debug.Log("Set local player " + (newName ?? "null"));
         bottomPlayerNameText.text = newName;
      }

      public void SetFoePlayerName(string newName)
      {
         topPlayerNameText.SetText(newName);
      }

      public void SetMoveNextName(string name)
      {
         whosMoveNameText.SetText($"{name} next move");
      }
   }
}

using TMPro;
using UnityEngine;

namespace Hoy
{
   public class UI : MonoBehaviour
   {
      [SerializeField] private GameObject waitPlayerPanel;
      [SerializeField] private GameObject inGameUI;
      [SerializeField] private TextMeshProUGUI bottomPlayerNameText;
      [SerializeField] private TextMeshProUGUI topPlayerNameText;
      [SerializeField] private TextMeshProUGUI whosMoveNameText;
      [SerializeField] private TextMeshProUGUI playerScore;
      [SerializeField] private TextMeshProUGUI foeScore;

      private void Awake()
      {
         waitPlayerPanel.SetActive(true);
         inGameUI.SetActive(false);
         whosMoveNameText.SetText("");
      }

      public void DeactivateWaitPanel() => 
         waitPlayerPanel.SetActive(false);

      public void ActivateInGameUI() => 
         inGameUI.SetActive(true);

      public void SetLocalPlayerName(string newName)
      {
         Debug.Log("Set local player " + (newName ?? "null"));
         bottomPlayerNameText.text = newName;
      }

      public void SetFoePlayerName(string newName) =>
         topPlayerNameText.SetText(newName);

      public void SetMoveNextName(string name) =>
         whosMoveNameText.SetText(name != null ? $"{name} next move" : "");

      public void DeactivateWhosMoveNameText() => 
         whosMoveNameText.gameObject.SetActive(false);

      public void ActivateScores()
      {
         playerScore.gameObject.SetActive(true);
         foeScore.gameObject.SetActive(true);
      }

      public void SetPlayerScore(int score)
      {
         playerScore.SetText(score.ToString());
      }

      public void SetFoeScore(int score)
      {
         foeScore.SetText(score.ToString());
      }
   }
}

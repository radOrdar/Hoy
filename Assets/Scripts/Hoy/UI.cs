using System.Linq;
using TMPro;
using UnityEngine;

namespace Hoy
{
   public class UI : MonoBehaviour
   {
      [SerializeField] private TextMeshProUGUI bottomPlayerNameText;
      [SerializeField] private TextMeshProUGUI topPlayerNameText;
      [SerializeField] private TextMeshProUGUI whosMoveNameText;
      [SerializeField] private TextMeshProUGUI playerScore;
      [SerializeField] private TextMeshProUGUI foeScore;
      [SerializeField] private TextMeshProUGUI roundsText;
      [SerializeField] private TextMeshProUGUI roundWinnerNameText;
      [SerializeField] private TextMeshProUGUI roundWinnerScoreText;
      [SerializeField] private TextMeshProUGUI seriesStatText;
      [SerializeField] private TextMeshProUGUI gameOverText;

      private void Awake()
      {
         whosMoveNameText.SetText("");
      }

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

      public void SetRoundsInfo(int currentRound, int numberOfRounds)
      {
         roundsText.SetText($"Round {currentRound}/{numberOfRounds}");
      }

      public void ShowWinner(string playerName, int score)
      {
         roundWinnerNameText.gameObject.SetActive(true);
         roundWinnerScoreText.gameObject.SetActive(true);
         roundWinnerNameText.SetText($"Round Winner {playerName}");
         roundWinnerScoreText.SetText($"His score {score.ToString()}");
      }

      public void DeactivatePlayerNames()
      {
         topPlayerNameText.gameObject.SetActive(false);
         bottomPlayerNameText.gameObject.SetActive(false);
      }

      public void DeactivateScoreTexts()
      {
         playerScore.gameObject.SetActive(false);
         foeScore.gameObject.SetActive(false);
      }

      public void ShowSeriesStat(HoyRoomPlayer[] hoyRoomPlayers)
      {
         roundWinnerNameText.gameObject.SetActive(false);
         roundWinnerScoreText.gameObject.SetActive(false);
         string stat = "";
         foreach (var roomPlayer in hoyRoomPlayers)
         {
            stat += $"{roomPlayer.PlayerName}:{roomPlayer.Wins} wins ";
         }

         seriesStatText.gameObject.SetActive(true);
         seriesStatText.SetText(stat);
      }

      public void ShowGameOver()
      {
         gameOverText.gameObject.SetActive(true);
      }
   }
}

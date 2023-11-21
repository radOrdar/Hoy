using System.Linq;
using TMPro;
using UnityEngine;

namespace Hoy
{
   public class UI : MonoBehaviour
   {
      [SerializeField] private TextMeshProUGUI whosMoveNameText;
      [SerializeField] private TextMeshProUGUI playerScore;
      [SerializeField] private TextMeshProUGUI roundsText;
      [SerializeField] private TextMeshProUGUI roundWinnerNameText;
      [SerializeField] private TextMeshProUGUI roundWinnerScoreText;
      [SerializeField] private TextMeshProUGUI seriesStatText;
      [SerializeField] private TextMeshProUGUI seriesStatLabel;
      [SerializeField] private TextMeshProUGUI gameOverText;

      private void Awake()
      {
         whosMoveNameText.SetText("");
         whosMoveNameText.gameObject.SetActive(true);
      }

      public void SetMoveNextName(string name) =>
         whosMoveNameText.SetText(name != null ? $"Ходит {name}" : "");

      public void DeactivateWhosMoveNameText() => 
         whosMoveNameText.gameObject.SetActive(false);

      public void ActivateScores()
      {
         playerScore.gameObject.SetActive(true);
      }

      public void SetPlayerScore(string score)
      {
         playerScore.SetText(score);
      }

      public void SetRoundsInfo(int currentRound, int numberOfRounds)
      {
         roundsText.SetText($"Раунд {currentRound}/{numberOfRounds}");
      }

      public void ShowWinner(string playerName, int score)
      {
         roundWinnerNameText.gameObject.SetActive(true);
         roundWinnerScoreText.gameObject.SetActive(true);
         roundWinnerNameText.SetText($"Победа в раунде за {playerName}");
         roundWinnerScoreText.SetText($"Со счетом {score.ToString()}");
      }

      // public void DeactivatePlayerNames()
      // {
      //    topPlayerNameText.gameObject.SetActive(false);
      //    bottomPlayerNameText.gameObject.SetActive(false);
      // }

      public void DeactivateScoreTexts()
      {
         playerScore.gameObject.SetActive(false);
      }

      public void ShowSeriesStat(HoyRoomPlayer[] hoyRoomPlayers)
      {
         roundWinnerNameText.gameObject.SetActive(false);
         roundWinnerScoreText.gameObject.SetActive(false);
         string stat = "";
         foreach (var roomPlayer in hoyRoomPlayers)
         {
            stat += $"{roomPlayer.PlayerName}:{roomPlayer.Wins} ";
         }

         seriesStatLabel.gameObject.SetActive(true);
         seriesStatText.gameObject.SetActive(true);
         seriesStatText.SetText(stat);
      }

      public void ShowGameOver()
      {
         gameOverText.gameObject.SetActive(true);
      }
   }
}

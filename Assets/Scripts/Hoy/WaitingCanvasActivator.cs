using UnityEngine;

namespace Hoy
{
   public class WaitingCanvasActivator : MonoBehaviour
   {
      [SerializeField] private GameObject canvas;

      private void Awake()
      {
         canvas.SetActive(true);
      }

      public void Deactivate()
      {
         canvas.SetActive(false);
      }
   }
}

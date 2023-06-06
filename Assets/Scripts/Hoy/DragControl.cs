using Mirror;
using UnityEngine;

namespace Hoy
{
    public class DragControl : NetworkBehaviour
    {
        private Camera mainCamera;
        private Transform cachedTransform;
        private bool yourTurn;
        
        public override void OnStartClient()
        {
            mainCamera = Camera.main;
            cachedTransform = transform;
        }

        [ClientCallback]
        private void OnMouseUp()
        {
            if(!isOwned) return;
            if(!yourTurn) return;
            GetComponent<Card>().CmdOnEndDrag();
        }

        [ClientCallback]
        private void OnMouseDrag()
        {
            if(!isOwned) return;
            if(!yourTurn) return;
            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            
            cachedTransform.position = new Vector3(mousePos.x, mousePos.y, cachedTransform.position.z);
        }

        [ClientCallback]
        private void OnMouseDown()
        {
            yourTurn = GameManager.singleton.CurrentGameState == GameState.PlayerTurn && GameManager.singleton.WhosNextMove.PlayerName == NetworkClient.localPlayer.GetComponent<HoyPlayer>().PlayerName;
        }
    }
}
using System;
using Mirror;
using UnityEngine;

namespace Hoy
{
    public class DragControl : NetworkBehaviour
    {
        public Action OnStartDrag;
        public Action OnEndDrag;
        private Camera mainCamera;
        private Transform cachedTransform;
        private bool yourTurn;
        private bool isDragging;
        
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
            OnEndDrag();
            isDragging = false;
        }

        [ClientCallback]
        private void OnMouseDrag()
        {
            if(!isOwned) return;
            if(!yourTurn) return;
            if (!isDragging)
            {
                isDragging = true;
                OnStartDrag();
            }
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
using System;
using Mirror;
using UnityEngine;

namespace Hoy.Cards
{
    public class DragControl : NetworkBehaviour
    {
        public Action OnStartDrag;
        public Action OnEndDrag;
        private Camera mainCamera;
        private Transform cachedTransform;
        private bool yourTurn;
        private bool isDragging;
        private Plane _plane;
        
        public override void OnStartClient()
        {
            mainCamera = Camera.main;
            _plane = new Plane(-Vector3.forward, -1); 
        }

        public override void OnStartServer()
        {
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
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            _plane.Raycast(ray, out float enter);
            var hitPoint = ray.GetPoint(enter);
            CmdMoveCard(hitPoint.x, hitPoint.y);
        }

        [Command]
        private void CmdMoveCard(float newX, float newY)
        {
            cachedTransform.position = new Vector3(newX, newY, cachedTransform.position.z);
        }

        [ClientCallback]
        private void OnMouseDown()
        {
            yourTurn = BaseGameManager.Instance.CurrentGameState == GameState.PlayerTurn && BaseGameManager.Instance.WhosNextMove.PlayerName == NetworkClient.localPlayer.GetComponent<HoyPlayer>().PlayerName;
        }
    }
}
using System.Linq;
using DG.Tweening;
using Hoy.StaticData;
using Mirror;
using UnityEngine;

namespace Hoy
{
    public class Card : NetworkBehaviour
    {
        [SerializeField] private CardStaticData[] _staticDatas;
        [SerializeField] private SpriteRenderer faceSpriteRenderer;
        [SerializeField] public float cardDealMoveTime = 0.5f;
        [SerializeField] private DragControl _dragControl;


        private Vector3 _target;

        private CardStaticData _staticData;

        public override void OnStartAuthority()
        {
            Debug.Log("card authority");
            base.OnStartAuthority();
            faceSpriteRenderer.sprite = _staticData.faceSprite;
        }

        [Server]
        public void Initialize(CardStaticData cardStaticData)
        {
            _staticData = cardStaticData;
            RPCInitialize(cardStaticData.faceType);
        }

        [ClientRpc]
        private void RPCInitialize(CardFaceType faceType)
        {
            _staticData = _staticDatas.First(sd => sd.faceType == faceType);
        }

        [TargetRpc]
        public void SetTarget(NetworkConnectionToClient conn, Vector3 newTarget)
        {
            _target = newTarget;
            _dragControl.enabled = false;
            DOTween.Sequence().Append(transform.DOMove(newTarget, cardDealMoveTime))
                .AppendCallback(() => _dragControl.enabled = true);
        }

        [Command]
        public void CmdOnEndDrag()
        {
            GameManager.singleton.DragEnded(this);
        }
    }
}
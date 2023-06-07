using System.Linq;
using DG.Tweening;
using Hoy.StaticData;
using Mirror;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hoy
{
    public class Card : NetworkBehaviour
    {
        [SerializeField] private CardStaticData[] _staticDatas;
        [SerializeField] private SpriteRenderer faceSpriteRenderer;
        [SerializeField] private SortingGroup _sortingGroup;
        [SerializeField] public float cardDealMoveTime = 0.5f;
        [SerializeField] private DragControl _dragControl;


        private Vector3 _target;

        private CardStaticData _staticData;
        public int Value => _staticData.value;

        public override void OnStartAuthority()
        {
            base.OnStartAuthority();
            if (isClient)
            {
                faceSpriteRenderer.sprite = _staticData.faceSprite;
            }
        }

        [Server]
        public void Initialize(CardStaticData cardStaticData)
        {
            _staticData = cardStaticData;
            RPCInitialize(cardStaticData.faceType);
        }

        [Command]
        public void CmdOnEndDrag()
        {
            GameManager.singleton.DragEnded(this);
        }

        [Server]
        public void SetTargetServer(Vector3 newTarget)
        {
            _target = newTarget;
            DOTween.Sequence().Append(transform.DOMove(newTarget, cardDealMoveTime));
        }

        [TargetRpc]
        public void RpcSetTargetOnLocalPlayer(NetworkConnectionToClient conn, Vector3 newTarget)
        {
            _target = newTarget;
            _dragControl.enabled = false;
            DOTween.Sequence().Append(transform.DOMove(newTarget, cardDealMoveTime))
                .AppendCallback(() => _dragControl.enabled = true);
        }

        [ClientRpc]
        private void RPCInitialize(CardFaceType faceType)
        {
            _staticData = _staticDatas.First(sd => sd.faceType == faceType);
        }


        [ClientRpc]
        public void RpcSetOrderInLayer(int nextOrderInLayer)
        {
            _sortingGroup.sortingOrder = nextOrderInLayer;
        }

        [ClientRpc]
        public void RpcShowCardToAllClients()
        {
            faceSpriteRenderer.sprite = _staticData.faceSprite;
        } 
    }
}
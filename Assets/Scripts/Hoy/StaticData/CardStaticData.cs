using UnityEngine;

namespace Hoy.StaticData
{
    [CreateAssetMenu(fileName = "CardStaticData", menuName = "StaticData/Card")]
    public class CardStaticData : ScriptableObject
    {
        public CardFaceType faceType;
        public Sprite faceSprite;
        public Sprite shirtSprite;
        public int value;
        public int numberInDeck;
    }
}

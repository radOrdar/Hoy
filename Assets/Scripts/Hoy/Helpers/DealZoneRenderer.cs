using System;
using UnityEngine;

namespace Hoy.Helpers
{
    [RequireComponent(typeof(Renderer))]
    public class DealZoneRenderer : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Renderer>().enabled = false;
        }
    }
}
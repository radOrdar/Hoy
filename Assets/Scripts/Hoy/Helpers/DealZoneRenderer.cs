using System;
using UnityEngine;

namespace Hoy.Helpers
{
    public class DealZoneRenderer : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Renderer>().enabled = false;
        }
    }
}
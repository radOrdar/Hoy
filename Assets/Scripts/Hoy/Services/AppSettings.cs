﻿using UnityEngine;

namespace Hoy.Services
{
    public class AppSettings : MonoBehaviour
    {
        public int targetFPS = 30;

        private void Start()
        {
            Application.targetFrameRate = targetFPS;
        }
    }
}
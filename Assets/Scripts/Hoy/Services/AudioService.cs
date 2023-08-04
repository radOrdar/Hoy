using System;
using System.Collections;
using Hoy.Cards;
using Hoy.Helpers;
using Mirror;
using UnityEngine;

namespace Hoy.Services
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioService : NetworkBehaviour
    {
        [SerializeField] private SerializableDictionary<AudioSfxType, AudioClip> audioDictionary;
        private AudioSource _audioSource;
        public static AudioService Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            _audioSource = GetComponent<AudioSource>();
        }

        [ClientRpc]
        public void RpcPlayOneShotDelayed(AudioSfxType sfxType, float delay)
        {
            StartCoroutine(PlayShotDelayedRoutine(sfxType, delay));
        }

        private IEnumerator PlayShotDelayedRoutine(AudioSfxType sfxType, float delay)
        {
            yield return new WaitForSeconds(delay);
            _audioSource.PlayOneShot(audioDictionary[sfxType]);
        }
    }
}
using System;
using System.Collections;
using MyAssets.MiniGames.Scripts;
using UnityEngine;
using UnityEngine.Events;

namespace Scenes.ElectricityShocl_Scene.Audio
{
    public class AudioController : MiniGame
    {
        private AudioSource _audioSource;
        private bool _letAudioPlay;
        
        [HideInInspector] public UnityEvent onAudioStart;
        [HideInInspector] public UnityEvent onAudioEnd;

        [SerializeField] private float delay;

        public override void StartGame()
        {
            base.StartGame();

            _audioSource = GetComponent<AudioSource>();

            StartCoroutine(WaitToPlaySound());

            _letAudioPlay = true;
            onAudioStart?.Invoke();
        }


        private IEnumerator WaitToPlaySound()
        { 
            yield return new WaitForSeconds(delay);
            
            if (_letAudioPlay)
            {
                _audioSource.Play();
            }
        }
        private void Update()
        {
            if (!_letAudioPlay || !(Math.Abs(_audioSource.time - _audioSource.clip.length) <= 0.05f)) return;
            _letAudioPlay = false;
            onAudioEnd?.Invoke();
            EndGame();
        }

        public override void EndGame()
        {
            _audioSource.Stop();
            onAudioEnd?.Invoke();
            base.EndGame();
        }
    }
}

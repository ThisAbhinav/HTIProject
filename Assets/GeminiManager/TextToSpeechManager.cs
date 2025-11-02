using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleTextToSpeech.Scripts.Data;
using TMPro;
using System;
using ReadyPlayerAvatar = ReadyPlayerMe.Core;

namespace GoogleTextToSpeech.Scripts
{
    public class TextToSpeechManager : MonoBehaviour
    {
        [SerializeField] private VoiceScriptableObject voice;
        [SerializeField] private TextToSpeech text_to_speech;
        // [SerializeField] private AudioSource audioSource;
        [SerializeField] private List<AudioClip> fillerClips;
        [SerializeField] private bool enableFillerSpeech = true;
        [SerializeField] private float fillerDelay = 0.1f; // small pause before filler plays
        private Action<AudioClip> _audioClipReceived;
        private Action<BadRequestData> _errorReceived;
        public ReadyPlayerAvatar.VoiceHandler voiceHandler;
        private bool fillerPlayed = false;

        public void PlayFiller()
        {
            if (!enableFillerSpeech || fillerClips == null || fillerClips.Count == 0 || voiceHandler == null)
                return;

            int randomIndex = UnityEngine.Random.Range(0, fillerClips.Count);
            var clip = fillerClips[randomIndex];

            StartCoroutine(PlayFillerCoroutine(clip));
        }

        private IEnumerator PlayFillerCoroutine(AudioClip clip)
        {
            yield return new WaitForSeconds(fillerDelay);

            voiceHandler.AudioSource.Stop();
            voiceHandler.AudioSource.clip = clip;
            voiceHandler.AudioSource.Play();

            fillerPlayed = true;
        }
        public void SendTextToGoogle(string _text)
        {
            _errorReceived += ErrorReceived;
            _audioClipReceived += AudioClipReceived;
            text_to_speech.GetSpeechAudioFromGoogle(_text, voice, _audioClipReceived, _errorReceived);
            
        }

        private void ErrorReceived(BadRequestData badRequestData)
        {
            Debug.Log($"Error {badRequestData.error.code} : {badRequestData.error.message}");
        }

        private void AudioClipReceived(AudioClip clip)
        {
            StartCoroutine(PlayAfterFiller(clip));
        }

        private IEnumerator PlayAfterFiller(AudioClip clip)
        {
            // Wait for filler to finish (if it's still playing)
            while (voiceHandler.AudioSource.isPlaying && fillerPlayed)
                yield return null;

            fillerPlayed = false;

            voiceHandler.AudioSource.Stop();
            voiceHandler.AudioSource.clip = clip;
            voiceHandler.AudioSource.Play();
        }
    }
}


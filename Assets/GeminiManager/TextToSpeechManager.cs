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
        
        [Header("Verbal Filler Settings (HTI Experiment)")]
        [SerializeField] private List<AudioClip> fillerClips;
        [SerializeField] private float fillerDelay = 0.1f;
        
        private Action<AudioClip> _audioClipReceived;
        private Action<BadRequestData> _errorReceived;
        public ReadyPlayerAvatar.VoiceHandler voiceHandler;
        private bool fillerPlayed = false;

        private void Start()
        {
            // Preload fillers from Resources
            LoadFillersFromResources();
        }

        /// <summary>
        /// Preload all filler audio clips from Resources/Audio/Fillers
        /// </summary>
        private void LoadFillersFromResources()
        {
            AudioClip[] loadedClips = Resources.LoadAll<AudioClip>("Audio/Fillers");
            
            if (loadedClips != null && loadedClips.Length > 0)
            {
                if (fillerClips == null)
                    fillerClips = new List<AudioClip>();
                    
                fillerClips.AddRange(loadedClips);
                Debug.Log($"[TTS Manager] Loaded {fillerClips.Count} filler audio clips from Resources/Audio/Fillers");
            }
            else
            {
                Debug.LogWarning($"[TTS Manager] No filler clips found in Resources/Audio/Fillers. Use HTI Tools > Filler Recorder to create them.");
            }
        }

        /// <summary>
        /// Play a random verbal filler sound
        /// Called by ConversationManager when feedback is triggered
        /// </summary>
        public void PlayFiller()
        {
            if (voiceHandler == null || fillerClips == null || fillerClips.Count == 0)
            {
                Debug.LogWarning("[TTS Manager] Cannot play filler - no clips available or VoiceHandler missing");
                return;
            }

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

        /// <summary>
        /// Send text to Google TTS for main speech
        /// </summary>
        public void SendTextToGoogle(string _text)
        {
            _errorReceived += ErrorReceived;
            _audioClipReceived += AudioClipReceived;
            text_to_speech.GetSpeechAudioFromGoogle(_text, voice, _audioClipReceived, _errorReceived);
        }

        private void ErrorReceived(BadRequestData badRequestData)
        {
            Debug.LogError($"[TTS Error] {badRequestData.error.code}: {badRequestData.error.message}");
        }

        private void AudioClipReceived(AudioClip clip)
        {
            StartCoroutine(PlayAfterFiller(clip));
        }

        private IEnumerator PlayAfterFiller(AudioClip clip)
        {
            // Wait for filler to finish if it's still playing
            while (voiceHandler.AudioSource.isPlaying && fillerPlayed)
                yield return null;

            fillerPlayed = false;

            voiceHandler.AudioSource.Stop();
            voiceHandler.AudioSource.clip = clip;
            voiceHandler.AudioSource.Play();
        }
    }
}


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
        
        [Header("Verbal Filler Settings (HTI Experiment)")]
        [SerializeField] private bool usePrerecordedFillers = true;
        [SerializeField] private string prerecordedFillersPath = "Assets/Audio/Fillers";
        [SerializeField] private bool generateFillersFromTTS = false; // Fallback only
        [SerializeField] private string[] verbalFillerPhrases = new string[]
        {
            "um",
            "hmm",
            "uh",
            "let me think",
            "well"
        };
        
        private List<AudioClip> preloadedFillers = new List<AudioClip>();
        private bool fillersPreloaded = false;
        
        private Action<AudioClip> _audioClipReceived;
        private Action<BadRequestData> _errorReceived;
        public ReadyPlayerAvatar.VoiceHandler voiceHandler;
        private bool fillerPlayed = false;
        private bool fillersEnabled = true;

        private void Start()
        {
            if (usePrerecordedFillers)
            {
                PreloadFillerAudio();
            }
        }

        /// <summary>
        /// Preload all filler audio clips from Resources/Audio/Fillers
        /// </summary>
        private void PreloadFillerAudio()
        {
            preloadedFillers.Clear();
            
            // Try loading from Resources folder
            AudioClip[] loadedClips = Resources.LoadAll<AudioClip>("Audio/Fillers");
            
            if (loadedClips != null && loadedClips.Length > 0)
            {
                preloadedFillers.AddRange(loadedClips);
                fillersPreloaded = true;
                Debug.Log($"[TTS Manager] Preloaded {preloadedFillers.Count} filler audio clips from Resources");
            }
            else
            {
                Debug.LogWarning($"[TTS Manager] No preloaded fillers found in Resources/Audio/Fillers. Will use fallback methods.");
                fillersPreloaded = false;
            }
        }

        public void PlayFiller()
        {
            if (!enableFillerSpeech || !fillersEnabled || voiceHandler == null)
                return;

            // Priority 1: Use preloaded filler clips (most efficient)
            if (usePrerecordedFillers && fillersPreloaded && preloadedFillers.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, preloadedFillers.Count);
                var clip = preloadedFillers[randomIndex];
                StartCoroutine(PlayFillerCoroutine(clip));
                return;
            }

            // Priority 2: Use manually assigned clips
            if (fillerClips != null && fillerClips.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, fillerClips.Count);
                var clip = fillerClips[randomIndex];
                StartCoroutine(PlayFillerCoroutine(clip));
                return;
            }

            // Priority 3: Generate filler from TTS (fallback, costs API calls)
            if (generateFillersFromTTS && verbalFillerPhrases.Length > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, verbalFillerPhrases.Length);
                string fillerPhrase = verbalFillerPhrases[randomIndex];
                GenerateFillerFromTTS(fillerPhrase);
                return;
            }

            Debug.LogWarning("[TTS Manager] No filler clips available. Please record fillers using HTI Tools > Filler Recorder");
        }

        /// <summary>
        /// Generate filler sound using Google TTS
        /// </summary>
        private void GenerateFillerFromTTS(string phrase)
        {
            Action<AudioClip> onFillerReceived = (clip) =>
            {
                StartCoroutine(PlayFillerCoroutine(clip));
            };

            Action<BadRequestData> onFillerError = (error) =>
            {
                Debug.LogWarning($"[TTS Manager] Failed to generate filler: {error.error.message}");
            };

            text_to_speech.GetSpeechAudioFromGoogle(phrase, voice, onFillerReceived, onFillerError);
        }

        /// <summary>
        /// Enable/disable filler speech (called by FeedbackModeManager)
        /// </summary>
        public void SetFillerEnabled(bool enabled)
        {
            fillersEnabled = enabled;
            Debug.Log($"[TTS Manager] Fillers Enabled: {enabled}");
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


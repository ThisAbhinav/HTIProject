using System;
using System.Collections;
using System.IO;
using GoogleTextToSpeech.Scripts.Data;
using UnityEngine;
using UnityEngine.Networking;

namespace GoogleTextToSpeech.Scripts
{
    public class AudioConverter : MonoBehaviour
    {
        private const string Mp3FileName = "audio.mp3";
        private static Coroutine currentLoadCoroutine;

        public static void SaveTextToMp3(AudioData audioData)
        {
            try
            {
                var bytes = Convert.FromBase64String(audioData.audioContent);
                var filePath = Path.Combine(Application.temporaryCachePath, Mp3FileName);

                // Delete existing file first to avoid corruption
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                File.WriteAllBytes(filePath, bytes);

                // Verify file was written correctly
                if (!File.Exists(filePath))
                {
                    Debug.LogError("File was not created successfully");
                    return;
                }

                var fileInfo = new FileInfo(filePath);
                Debug.Log($"MP3 saved successfully: {fileInfo.Length} bytes at {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save MP3 file: {e.Message}\n{e.StackTrace}");
            }
        }

        public void LoadClipFromMp3(Action<AudioClip> onClipLoaded)
        {
            // Stop any existing load operation
            if (currentLoadCoroutine != null)
            {
                StopCoroutine(currentLoadCoroutine);
            }

            currentLoadCoroutine = StartCoroutine(LoadClipFromMp3Cor(onClipLoaded));
        }

        private IEnumerator LoadClipFromMp3Cor(Action<AudioClip> onClipLoaded)
        {
            var filePath = Path.Combine(Application.temporaryCachePath, Mp3FileName);

            // Check if file exists
            if (!File.Exists(filePath))
            {
                Debug.LogError($"MP3 file not found at: {filePath}");
                onClipLoaded?.Invoke(null);
                currentLoadCoroutine = null;
                yield break;
            }

            // Wait a frame to ensure file is fully written and released
            yield return null;

            // Verify file size
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length == 0)
            {
                Debug.LogError("MP3 file is empty");
                onClipLoaded?.Invoke(null);
                currentLoadCoroutine = null;
                yield break;
            }

            Debug.Log($"Loading audio file: {fileInfo.Length} bytes");

            // Try different URL formats (platform-dependent)
            string fileUrl;

            fileUrl = "file:///" + filePath.Replace("\\", "/");

            Debug.Log($"Loading from URL: {fileUrl}");

            UnityWebRequest webRequest = null;
            UnityWebRequestAsyncOperation operation = null;
            bool hasError = false;
            AudioClip clip = null;
            string error = null;
            long responseCode = 0;

            try
            {
                webRequest = UnityWebRequestMultimedia.GetAudioClip(fileUrl, AudioType.MPEG);

                // Configure the download handler before sending
                var downloadHandler = (DownloadHandlerAudioClip)webRequest.downloadHandler;
                downloadHandler.compressed = false;
                downloadHandler.streamAudio = false; // Must be false for GetData() to work

                // Set timeout
                webRequest.timeout = 10;

                operation = webRequest.SendWebRequest();
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception while loading audio: {e.Message}\n{e.StackTrace}");
                onClipLoaded?.Invoke(null);
                webRequest?.Dispose();
                currentLoadCoroutine = null;
                yield break;
            }

            // Wait for completion (must be outside try-catch to avoid CS1626)
            yield return operation;

            if (webRequest != null)
            {
                hasError = webRequest.result != UnityWebRequest.Result.Success;
                error = webRequest.error;
                responseCode = webRequest.responseCode;
                if (!hasError)
                {
                    var downloadHandler = (DownloadHandlerAudioClip)webRequest.downloadHandler;
                    clip = downloadHandler.audioClip;
                }
            }

            if (hasError)
            {
                Debug.LogError($"Failed to load audio clip: {error} (Code: {responseCode})");
                onClipLoaded?.Invoke(null);
            }
            else
            {
                if (clip != null && clip.loadState == AudioDataLoadState.Loaded)
                {
                    Debug.Log($"Audio clip loaded successfully: {clip.length}s, {clip.channels} channels, {clip.frequency}Hz");
                    onClipLoaded?.Invoke(clip);
                }
                else
                {
                    Debug.LogError($"AudioClip is null or not loaded. Load state: {(clip != null ? clip.loadState.ToString() : "null")}");
                    onClipLoaded?.Invoke(null);
                }
            }

            webRequest?.Dispose();
            currentLoadCoroutine = null;
        }
    }
}
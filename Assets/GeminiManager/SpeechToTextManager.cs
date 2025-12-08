using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using System.IO;

namespace GoogleSpeechToText.Scripts
{
    public class SpeechToTextManager : MonoBehaviour
    {
        [Header("Google Cloud API Password")]
        [SerializeField] private string apiKey;

        [Header("Manager References")]
        [SerializeField] private UnityAndGeminiV3 geminiManager;
        [SerializeField] private ChatManager chatManager;
        [SerializeField] private ConversationManager conversationManager;

        private bool useVRControllers = true;
        private XRNode controllerHand = XRNode.RightHand;

        [Header("UI Interaction Feedback")]
        [Tooltip("UI Object that says 'Press Button to Start Conversation'")]
        [SerializeField] private GameObject startInstructionsUI;
        [Tooltip("UI Object that appears when Recording (e.g., Red Mic Icon)")]
        [SerializeField] private GameObject recordingUI;

        private AudioClip clip;
        private byte[] bytes;
        private bool recording = false;
        private bool wasButtonPressed = false;
        private bool hasConversationStarted = false;

        // VR input device
        private InputDevice targetDevice;

        void Start()
        {
            // Initial UI State
            if (startInstructionsUI) startInstructionsUI.SetActive(true);
            if (recordingUI) recordingUI.SetActive(false);

            if (useVRControllers)
            {
                StartCoroutine(InitializeVRControllerRoutine());
            }
        }

        void Update()
        {
            bool buttonPressed = false;
            bool buttonReleased = false;

            // 1. Keyboard Debugging (Space bar)
            if (Input.GetKeyDown(KeyCode.Space)) buttonPressed = true;
            if (Input.GetKeyUp(KeyCode.Space)) buttonReleased = true;

            // 2. VR Controller Input
            if (useVRControllers)
            {
                // If device disconnected, try to find it again
                if (!targetDevice.isValid)
                {
                    InitializeVRController();
                }

                if (targetDevice.isValid)
                {
                    bool isDown = CheckTriggerInput();

                    // Logic to detect "Just Pressed" vs "Just Released"
                    if (isDown && !wasButtonPressed)
                    {
                        if (!recording) buttonPressed = true;
                    }
                    else if (!isDown && wasButtonPressed)
                    {
                        if (recording) buttonReleased = true;
                    }

                    wasButtonPressed = isDown;
                }
            }

            // --- ACTION HANDLING ---

            if (buttonPressed)
            {
                if (!hasConversationStarted)
                {
                    // Case 1: First interaction - Start Conversation
                    StartConversationFlow();
                }
                else if (!recording)
                {
                    // Case 2: Conversation active - Start Recording
                    StartRecording();
                }
            }

            // Handle recording stop
            if (buttonReleased && recording)
            {
                StopRecording();
            }
        }

        /// <summary>
        /// Checks both the Binary Button and the Analog Axis for the trigger.
        /// This is the most robust way to detect input on Quest 2.
        /// </summary>
        private bool CheckTriggerInput()
        {
            bool buttonValue = false;
            float axisValue = 0.0f;

            // Method A: Check for the physical "Click" button
            if (targetDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool btn))
            {
                buttonValue = btn;
            }

            // Method B: Check for the analog squeeze (0.0 to 1.0)
            // Useful if the "Button" doesn't map correctly in OpenXR
            if (targetDevice.TryGetFeatureValue(CommonUsages.trigger, out float val))
            {
                axisValue = val;
            }

            // Return true if button is clicked OR trigger is squeezed > 50%
            return buttonValue || (axisValue > 0.5f);
        }

        // Coroutine to retry finding the controller for a few seconds at start
        private IEnumerator InitializeVRControllerRoutine()
        {
            yield return new WaitForSeconds(1.0f);
            InitializeVRController();
        }

        private void InitializeVRController()
        {
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(controllerHand, devices);

            if (devices.Count > 0)
            {
                targetDevice = devices[0];
                Debug.Log($"<color=green>Success: Found Controller {targetDevice.name}</color>");
            }
            else
            {
                List<InputDevice> allDevices = new List<InputDevice>();
                InputDevices.GetDevices(allDevices);
                //foreach (var d in allDevices)
                //{
                //    Debug.Log($"Found Device: {d.name} | Role: {d.role}");
                //}
            }
        }

        private void StartConversationFlow()
        {
            hasConversationStarted = true;
            Debug.Log("Starting Conversation...");
            if (startInstructionsUI) startInstructionsUI.SetActive(false);
            if (geminiManager) geminiManager.StartIntro();
        }

        private void StartRecording()
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.LogError("No Microphone found!");
                return;
            }

            clip = Microphone.Start(null, false, 20, 44100);
            recording = true;

            if (recordingUI != null) recordingUI.SetActive(true);

            TriggerHaptic(0.1f);
            Debug.Log("Recording Started...");
        }

        private void StopRecording()
        {
            if (!recording) return;

            Debug.Log("Recording Stopped. Processing...");
            var position = Microphone.GetPosition(null);
            Microphone.End(null);

            if (recordingUI) recordingUI.SetActive(false);

            // Safety check for very short recordings
            if (position <= 0)
            {
                Debug.LogWarning("Recording was empty or failed.");
                recording = false;
                return;
            }

            conversationManager.TriggerFeedback("");

            var samples = new float[position * clip.channels];
            clip.GetData(samples, 0);
            bytes = EncodeAsWAV(samples, clip.frequency, clip.channels);
            recording = false;

            TriggerHaptic(0.2f);

            GoogleCloudSpeechToText.SendSpeechToTextRequest(bytes, apiKey,
                (response) => {
                    var speechResponse = JsonUtility.FromJson<SpeechToTextResponse>(response);
                    if (speechResponse.results != null && speechResponse.results.Length > 0)
                    {
                        var transcript = speechResponse.results[0].alternatives[0].transcript;
                        Debug.Log($"Transcript: {transcript}");
                        if (chatManager != null) chatManager.AddUserMessage(transcript);
                        if (geminiManager != null) geminiManager.SendChat(transcript);
                    }
                },
                (error) => {
                    Debug.LogError("STT Error: " + error.error.message);
                    // Stop feedback if STT fails
                    if (conversationManager != null)
                    {
                        conversationManager.StopFeedback();
                    }
                });
        }

        private void TriggerHaptic(float duration)
        {
            if (useVRControllers && targetDevice.isValid)
            {
                HapticCapabilities capabilities;
                if (targetDevice.TryGetHapticCapabilities(out capabilities) && capabilities.supportsImpulse)
                {
                    targetDevice.SendHapticImpulse(0, 0.5f, duration);
                }
            }
        }

        private byte[] EncodeAsWAV(float[] samples, int frequency, int channels)
        {
            using (var memoryStream = new MemoryStream(44 + samples.Length * 2))
            {
                using (var writer = new BinaryWriter(memoryStream))
                {
                    writer.Write("RIFF".ToCharArray());
                    writer.Write(36 + samples.Length * 2);
                    writer.Write("WAVE".ToCharArray());
                    writer.Write("fmt ".ToCharArray());
                    writer.Write(16);
                    writer.Write((ushort)1);
                    writer.Write((ushort)channels);
                    writer.Write(frequency);
                    writer.Write(frequency * channels * 2);
                    writer.Write((ushort)(channels * 2));
                    writer.Write((ushort)16);
                    writer.Write("data".ToCharArray());
                    writer.Write(samples.Length * 2);
                    foreach (var sample in samples)
                    {
                        writer.Write((short)(sample * short.MaxValue));
                    }
                }
                return memoryStream.ToArray();
            }
        }
    }
}
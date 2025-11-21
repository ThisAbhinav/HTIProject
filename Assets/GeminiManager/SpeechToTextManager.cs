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

        [Header("Input Settings")]
        [SerializeField] private bool useVRControllers = true;
        [SerializeField] private XRNode controllerHand = XRNode.RightHand;
        [SerializeField] private string vrButtonFeature = "triggerButton";

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
        private InputFeatureUsage<bool> buttonUsage;

        void Start()
        {
            if (useVRControllers) InitializeVRController();

            // Initial UI State
            startInstructionsUI.SetActive(true);
            recordingUI.SetActive(false);
        }

        void Update()
        {
            bool buttonPressed = false;
            bool buttonReleased = false;

            // Check keyboard input (Space bar)
            if (Input.GetKeyDown(KeyCode.Space))
            {
                buttonPressed = true;
            }
            if (Input.GetKeyUp(KeyCode.Space))
            {
                buttonReleased = true;
            }

            // Check VR controller input
            if (useVRControllers)
            {
                if (!targetDevice.isValid)
                {
                    InitializeVRController();
                }

                if (targetDevice.isValid)
                {
                    bool buttonState = false;

                    // Try to get button state
                    if (targetDevice.TryGetFeatureValue(buttonUsage, out buttonState))
                    {
                        // Button just pressed
                        if (buttonState && !wasButtonPressed && !recording)
                        {
                            buttonPressed = true;
                        }

                        // Button just released
                        if (!buttonState && wasButtonPressed && recording)
                        {
                            buttonReleased = true;
                        }

                        wasButtonPressed = buttonState;
                    }
                }
            }

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
        private void StartConversationFlow()
        {
            hasConversationStarted = true;
            Debug.Log("Starting Conversation...");
            startInstructionsUI.SetActive(false);
            geminiManager.StartIntro();
        }
        private void InitializeVRController()
        {
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(controllerHand, devices);

            if (devices.Count > 0)
            {
                targetDevice = devices[0];
                buttonUsage = new InputFeatureUsage<bool>(vrButtonFeature);
            }
        }

        private void StartRecording()
        {
            clip = Microphone.Start(null, false, 10, 44100);
            recording = true;

            if (recordingUI != null) recordingUI.SetActive(true);

            TriggerHaptic(0.1f);
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

        private void StopRecording()
        {
            var position = Microphone.GetPosition(null);
            Microphone.End(null);

            recordingUI.SetActive(false);

            var samples = new float[position * clip.channels];
            clip.GetData(samples, 0);
            bytes = EncodeAsWAV(samples, clip.frequency, clip.channels);
            recording = false;

            TriggerHaptic(0.2f); 

            Debug.Log("Processing speech...");

            GoogleCloudSpeechToText.SendSpeechToTextRequest(bytes, apiKey,
                (response) => {
                    var speechResponse = JsonUtility.FromJson<SpeechToTextResponse>(response);
                    if (speechResponse.results != null && speechResponse.results.Length > 0)
                    {
                        var transcript = speechResponse.results[0].alternatives[0].transcript;
                        if (chatManager != null) chatManager.AddUserMessage(transcript);
                        if (geminiManager != null) geminiManager.SendChat(transcript);
                    }
                },
                (error) => {
                    Debug.LogError("STT Error: " + error.error.message);
                });
        }

    }
}
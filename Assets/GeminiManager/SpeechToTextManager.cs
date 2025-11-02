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

        private AudioClip clip;
        private byte[] bytes;
        private bool recording = false;
        private bool wasButtonPressed = false;

        // VR input device
        private InputDevice targetDevice;
        private InputFeatureUsage<bool> buttonUsage;

        void Start()
        {
            if (useVRControllers)
            {
                InitializeVRController();
            }
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

            // Handle recording start
            if (buttonPressed && !recording)
            {
                StartRecording();
                recording = true;
            }

            // Handle recording stop
            if (buttonReleased && recording)
            {
                StopRecording();
                recording = false;
            }
        }

        private void InitializeVRController()
        {
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(controllerHand, devices);

            if (devices.Count > 0)
            {
                targetDevice = devices[0];

                buttonUsage = new InputFeatureUsage<bool>(vrButtonFeature);

                Debug.Log($"VR Controller initialized: {targetDevice.name} on {controllerHand} with button: {vrButtonFeature}");
            }
            else
            {
                Debug.LogWarning($"No VR controller found for {controllerHand}");
            }
        }

        private void StartRecording()
        {
            clip = Microphone.Start(null, false, 10, 44100);
            recording = true;
            Debug.Log("Recording started...");

            // Optional: Haptic feedback for VR
            if (useVRControllers && targetDevice.isValid)
            {
                HapticCapabilities capabilities;
                if (targetDevice.TryGetHapticCapabilities(out capabilities))
                {
                    if (capabilities.supportsImpulse)
                    {
                        targetDevice.SendHapticImpulse(0, 0.3f, 0.1f); // Short vibration
                    }
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
            var samples = new float[position * clip.channels];
            clip.GetData(samples, 0);
            bytes = EncodeAsWAV(samples, clip.frequency, clip.channels);
            recording = false;

            Debug.Log("Recording stopped. Processing speech...");

            // Optional: Haptic feedback for VR
            if (useVRControllers && targetDevice.isValid)
            {
                HapticCapabilities capabilities;
                if (targetDevice.TryGetHapticCapabilities(out capabilities))
                {
                    if (capabilities.supportsImpulse)
                    {
                        targetDevice.SendHapticImpulse(0, 0.5f, 0.2f); // Stronger vibration
                    }
                }
            }

            GoogleCloudSpeechToText.SendSpeechToTextRequest(bytes, apiKey,
                (response) => {
                    Debug.Log("Speech-to-Text Response: " + response);
                    var speechResponse = JsonUtility.FromJson<SpeechToTextResponse>(response);

                    if (speechResponse.results != null && speechResponse.results.Length > 0)
                    {
                        var transcript = speechResponse.results[0].alternatives[0].transcript;
                        Debug.Log("Transcript: " + transcript);

                        // Add user message to chat display FIRST
                        if (chatManager != null)
                        {
                            chatManager.AddUserMessage(transcript);
                        }

                        // Then send to Gemini
                        if (geminiManager != null)
                        {
                            geminiManager.SendChat(transcript);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("No speech detected in audio");
                        if (chatManager != null)
                        {
                            chatManager.AddSystemMessage("No speech detected. Try speaking louder or closer to the microphone.");
                        }
                    }
                },
                (error) => {
                    Debug.LogError("Speech-to-Text Error: " + error.error.message);
                    if (chatManager != null)
                    {
                        chatManager.AddSystemMessage($"Speech recognition error: {error.error.message}");
                    }
                });
        }

        // Public methods to change settings at runtime
        public void SetVRButton(string buttonFeatureName)
        {
            vrButtonFeature = buttonFeatureName;
            buttonUsage = new InputFeatureUsage<bool>(buttonFeatureName);
            Debug.Log($"VR button changed to: {buttonFeatureName}");
        }

        public void SetControllerHand(XRNode hand)
        {
            controllerHand = hand;
            InitializeVRController();
        }

        public void ToggleVRInput(bool enabled)
        {
            useVRControllers = enabled;
            if (enabled)
            {
                InitializeVRController();
            }
        }
    }
}
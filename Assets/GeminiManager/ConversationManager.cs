using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using GoogleTextToSpeech.Scripts;
using System.Linq;

/// <summary>
/// Central controller for HTI experiment conversation management
/// Manages conversation flow, feedback modalities, timing, and data logging
/// </summary>
public class ConversationManager : MonoBehaviour
{
    [System.Flags]
    public enum FeedbackType
    {
        None = 0,
        VerbalFiller = 1 << 0,      // "um", "hmm" audio fillers
        VisualCueIcon = 1 << 1,     // Spinning/pulsing loading icon
        VisualCueText = 1 << 2,     // loading text
        Gesture = 1 << 3            // gesture feedback (thinking animation)
    }

    [Header("EXPERIMENT CONFIGURATION")]
    [SerializeField] private bool enableFeedback = true; // Main control/experiment toggle
    [SerializeField] private FeedbackType activeFeedbackTypes = FeedbackType.VerbalFiller | FeedbackType.VisualCueIcon | FeedbackType.VisualCueText | FeedbackType.Gesture;

    [Header("COMPONENT REFERENCES")]
    [SerializeField] private TextToSpeechManager ttsManager;
    [SerializeField] private TaskManager taskManager;
    [SerializeField] private GameObject visualCuePrefab;
    [SerializeField] private GameObject visualCueGameObject;// "Thinking..." text
    [SerializeField] private AvatarAnimationController avatarAnimationController; // Avatar animation control
    [SerializeField] private GameObject endingUIPanel; // Ending UI panel to show when conversation ends

    [Header("FEEDBACK DELAY SETTINGS")]
    [Tooltip("Delay in seconds before feedback is triggered. This doesn't delay LLM processing, only the feedback UI.")]
    [SerializeField] private float feedbackDelay = 0.5f;

    [Header("VISUAL CUE SETTINGS")]
    [SerializeField]
    private string[] thinkingMessages = new string[]
    {
        "Thinking...",
        "Processing...",
        "Hmm...",
        "Let me think..."
    };

    [Header("DATA LOGGING")]
    [SerializeField] private string logDirectory = "Assets/ExperimentLogs";
    [SerializeField] private bool autoSaveOnEnd = true;

    // State tracking
    private float conversationStartTime;
    private int exchangeCount = 0;
    private int infoDiscoveredCount = 0;
    private bool conversationActive = false;
    private bool endConversationQueued = false; // NEW: Flag to control ending timing
    private bool feedbackActive = false;
    private float lastLogTime = 0f;
    private float logInterval = 60f;
    private Coroutine feedbackCoroutine;

    // Logging data
    private List<ConversationLogEntry> conversationLog = new List<ConversationLogEntry>();
    private List<ConversationExchange> conversationHistory = new List<ConversationExchange>();
    private Dictionary<string, float> feedbackTimings = new Dictionary<string, float>();
    private ConversationExchange currentExchange = null;

    // Singleton
    public static ConversationManager Instance { get; private set; }

    // Events
    public static event Action OnConversationStart;
    public static event Action OnConversationEnd;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        ChatManager.OnMessageAdded += OnMessageReceived;

        // Subscribe to TaskManager events
        if (taskManager != null)
        {
            TaskManager.OnTaskCompleted += OnInfoDiscovered;
            TaskManager.OnTaskProgressChanged += OnInfoProgressChanged;
            // Note: We handle ending via the QueueConversationEnd logic now, but keeping this listener is fine
            TaskManager.OnAllTasksCompleted += OnAllTasksCompleted;
        }

        if (visualCuePrefab != null) visualCuePrefab.SetActive(false);
        if (avatarAnimationController != null) avatarAnimationController.SetIdle();

        if (endingUIPanel != null)
        {
            endingUIPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        ChatManager.OnMessageAdded -= OnMessageReceived;
        TaskManager.OnTaskCompleted -= OnInfoDiscovered;
        TaskManager.OnTaskProgressChanged -= OnInfoProgressChanged;
        TaskManager.OnAllTasksCompleted -= OnAllTasksCompleted;
    }

    private void Update()
    {
        if (!conversationActive) return;

        float elapsed = Time.time - conversationStartTime;
        float elapsedMinutes = elapsed / 60f;

        // Log progress every minute
        if (elapsed - lastLogTime >= logInterval)
        {
            lastLogTime = elapsed;
            int mins = Mathf.FloorToInt(elapsedMinutes);
            Debug.Log($"[Conversation] {mins}m | Exchanges: {exchangeCount} | Info: {infoDiscoveredCount}/{taskManager.TotalTasksCount}");
        }
    }

    public void StartConversation()
    {
        conversationStartTime = Time.time;
        conversationActive = true;
        endConversationQueued = false; // Reset queue flag
        exchangeCount = 0;
        infoDiscoveredCount = 0;
        lastLogTime = 0f;
        conversationLog.Clear();
        conversationHistory.Clear(); // Clear history
        feedbackTimings.Clear();

        // Reset TaskManager tasks
        if (taskManager != null)
        {
            taskManager.ResetAllTasks();
        }

        string feedbackStatus = enableFeedback ? activeFeedbackTypes.ToString() : "None (Control)";
        Debug.Log($"[Conversation] ▶ STARTED | Feedback: {feedbackStatus}");

        LogEvent("CONVERSATION_START", $"Feedback: {feedbackStatus}");
        OnConversationStart?.Invoke();
    }

    /// <summary>
    /// NEW: Called by UnityAndGeminiV3 when JSON indicates conversation should end
    /// or when tasks are complete.
    /// </summary>
    public void QueueConversationEnd()
    {
        Debug.Log("[Conversation] Ending Queued - Waiting for Avatar to finish speaking.");
        endConversationQueued = true;
    }

    public void EndConversation(string reason)
    {
        if (!conversationActive) return;

        conversationActive = false;
        float duration = (Time.time - conversationStartTime) / 60f;

        int totalInfo = taskManager != null ? taskManager.TotalTasksCount : 8;
        Debug.Log($"[Conversation] ■ ENDED - {reason}");
        Debug.Log($"[Stats] Duration: {duration:F2}m | Exchanges: {exchangeCount} | Info: {infoDiscoveredCount}/{totalInfo}");

        LogEvent("CONVERSATION_END", $"Reason: {reason} | Duration: {duration:F2}m");

        // Show ending UI panel
        if (endingUIPanel != null)
        {
            endingUIPanel.SetActive(true);
        }

        if (autoSaveOnEnd)
        {
            SaveConversationLog();
        }

        OnConversationEnd?.Invoke();
    }

    private string SelectAppropriateFillerWord(string responsePreview)
    {
        if (string.IsNullOrEmpty(responsePreview))
            return "um"; // Default

        string lower = responsePreview.ToLower();

        // Quick keyword-based selection
        if (lower.Contains("interesting") || lower.Contains("cool") || lower.Contains("wow"))
            return "hmm interesting";
        else if (lower.Contains("think") || lower.Contains("consider"))
            return "let me think";
        else if (lower.Contains("?"))
            return "hmm that's a good question";
        else if (lower.Length > 100)
            return "let me see";
        else
            return "um"; // Default short filler
    }

    private void OnMessageReceived(ChatMessage message)
    {
        if (!conversationActive) return;

        if (message.type == MessageType.User)
        {
            // Start a new exchange
            currentExchange = new ConversationExchange
            {
                exchangeNumber = exchangeCount + 1,
                timestamp = Time.time - conversationStartTime,
                userMessage = message.message,
                feedbackTypes = new List<string>(),
                feedbackDelayUsed = feedbackDelay,
                feedbackStartTime = -1f,
                feedbackDuration = -1f,
                feedbackCancelled = false
            };

            LogEvent("USER_MESSAGE", message.message);
        }
        // NOTE: We no longer handle AI messages here for the LOG. 
        // We use LogFullAIResponse below to get the complete text from JSON.
    }

    /// <summary>
    /// NEW: Called specifically to log the full, clean AI response from the JSON.
    /// This fixes the issue where logs only contained partial streamed text.
    /// </summary>
    public void LogFullAIResponse(string fullText)
    {
        exchangeCount++;
        LogEvent("AI_RESPONSE_FULL", "Received full JSON response");

        if (currentExchange != null)
        {
            currentExchange.aiResponse = fullText;
            currentExchange.responseTime = (Time.time - conversationStartTime) - currentExchange.timestamp;
            conversationHistory.Add(currentExchange);
            currentExchange = null; // Reset for next turn
        }
    }

    private IEnumerator EndAfterGoodbye()
    {
        // Wait for the goodbye message to be spoken
        yield return new WaitForSeconds(2f);
        EndConversationWithGoodbye();
    }

    /// <summary>
    /// Trigger feedback when LLM processing starts
    /// </summary>
    public void TriggerFeedback(string llmResponsePreview = "")
    {
        if (!enableFeedback)
        {
            LogEvent("FEEDBACK_SKIPPED", "Control condition - No feedback");
            return;
        }

        // Cancel any existing feedback coroutine
        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
        }

        // Start delayed feedback
        feedbackCoroutine = StartCoroutine(TriggerFeedbackDelayed(llmResponsePreview));
    }

    private IEnumerator TriggerFeedbackDelayed(string llmResponsePreview)
    {
        float delayStartTime = Time.time;

        // Wait for the specified delay
        yield return new WaitForSeconds(feedbackDelay);

        feedbackActive = true;
        float feedbackStartTime = Time.time;

        // Record feedback start time in current exchange
        if (currentExchange != null)
        {
            currentExchange.feedbackStartTime = feedbackStartTime - conversationStartTime;
        }

        // Select appropriate filler based on response preview
        string selectedFiller = SelectAppropriateFillerWord(llmResponsePreview);

        // Verbal Filler
        if (activeFeedbackTypes.HasFlag(FeedbackType.VerbalFiller) && ttsManager != null)
        {
            ttsManager.PlayFiller();
            LogEvent("FEEDBACK_VERBAL", $"Filler: {selectedFiller}");
            if (currentExchange != null) currentExchange.feedbackTypes.Add("VerbalFiller");
        }

        // Visual Cue Icon
        if (activeFeedbackTypes.HasFlag(FeedbackType.VisualCueIcon))
        {
            ShowVisualCueIcon();
            LogEvent("FEEDBACK_VISUAL_ICON", "Loading icon shown");
            if (currentExchange != null) currentExchange.feedbackTypes.Add("VisualCueIcon");
        }

        // Visual Cue with Text
        if (activeFeedbackTypes.HasFlag(FeedbackType.VisualCueText))
        {
            ShowVisualCueText();
            LogEvent("FEEDBACK_VISUAL_WITH_TEXT", "Loading icon with text shown");
            if (currentExchange != null) currentExchange.feedbackTypes.Add("VisualCueText");
        }

        // Gesture (Thinking Animation)
        if (activeFeedbackTypes.HasFlag(FeedbackType.Gesture))
        {
            ShowThinkingGesture();
            LogEvent("FEEDBACK_GESTURE", "Thinking animation triggered");
            if (currentExchange != null) currentExchange.feedbackTypes.Add("Gesture");
        }

        feedbackTimings["last_feedback_start"] = feedbackStartTime;
        feedbackCoroutine = null;
    }

    /// <summary>
    /// Stop feedback when LLM response is ready
    /// </summary>
    public void StopFeedback()
    {
        if (!enableFeedback) return;

        // Cancel delayed feedback if it hasn't started yet
        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
            feedbackCoroutine = null;
            LogEvent("FEEDBACK_CANCELLED", "Response arrived before delay completed");

            // Mark feedback as cancelled in current exchange
            if (currentExchange != null)
            {
                currentExchange.feedbackCancelled = true;
            }

            // Return to idle if gesture was going to be triggered
            if (activeFeedbackTypes.HasFlag(FeedbackType.Gesture) && avatarAnimationController != null)
            {
                avatarAnimationController.SetIdle();
            }
            return;
        }

        feedbackActive = false;

        if (feedbackTimings.ContainsKey("last_feedback_start"))
        {
            float duration = Time.time - feedbackTimings["last_feedback_start"];
            LogEvent("FEEDBACK_STOPPED", $"Duration: {duration:F2}s");

            // Record feedback duration in current exchange
            if (currentExchange != null)
            {
                currentExchange.feedbackDuration = duration;
            }
        }

        // Visual Cue Icon
        if (activeFeedbackTypes.HasFlag(FeedbackType.VisualCueIcon))
        {
            HideVisualCueIcon();
        }
        // Visual Cue Text
        if (activeFeedbackTypes.HasFlag(FeedbackType.VisualCueText))
        {
            HideVisualCueText();
        }
        // Gesture (Back to Idle, will transition to Talking when TTS starts)
        if (activeFeedbackTypes.HasFlag(FeedbackType.Gesture))
        {
            HideThinkingGesture();
        }
    }

    private void ShowVisualCueIcon()
    {
        if (visualCuePrefab) visualCuePrefab.SetActive(true);
    }

    private void ShowVisualCueText()
    {
        if (visualCueGameObject)
        {
            int randomIndex = UnityEngine.Random.Range(0, thinkingMessages.Length);
            visualCueGameObject.SetActive(true);
            visualCueGameObject.GetComponentInChildren<TextMeshProUGUI>().text = thinkingMessages[randomIndex];
        }
    }

    private void HideVisualCueIcon()
    {
        if (visualCuePrefab) visualCuePrefab.SetActive(false);
    }
    private void HideVisualCueText()
    {
        if (visualCueGameObject) visualCueGameObject.SetActive(false);
    }

    private void ShowThinkingGesture()
    {
        if (avatarAnimationController != null)
        {
            avatarAnimationController.SetThinking();
        }
    }

    private void HideThinkingGesture()
    {
        if (avatarAnimationController != null)
        {
            avatarAnimationController.SetIdle();
        }
    }

    /// <summary>
    /// Called by TaskManager when info is discovered
    /// </summary>
    private void OnInfoDiscovered(string infoTitle)
    {
        Debug.Log($"[Conversation] ✓ Info Discovered: {infoTitle}");
        LogEvent("INFO_DISCOVERED", infoTitle);
    }

    /// <summary>
    /// Called by TaskManager when progress changes
    /// </summary>
    private void OnInfoProgressChanged(int completed, int total)
    {
        infoDiscoveredCount = completed;
    }

    /// <summary>
    /// Called when all tasks are completed
    /// </summary>
    private void OnAllTasksCompleted()
    {
        Debug.Log("[Conversation] All tasks completed via TaskManager event.");
        LogEvent("ALL_TASKS_COMPLETED", $"All {taskManager.TotalTasksCount} tasks completed");
    }

    public void EndConversationWithGoodbye()
    {
        EndConversation("Ended via Goodbye Sequence");
    }

    private void LogEvent(string eventType, string details)
    {
        float timestamp = conversationActive ? (Time.time - conversationStartTime) : 0f;

        conversationLog.Add(new ConversationLogEntry
        {
            timestamp = timestamp,
            eventType = eventType,
            details = details,
            exchangeCount = exchangeCount,
            infoDiscovered = infoDiscoveredCount
        });
    }

    public void SaveConversationLog()
    {
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string baseFilename = $"HTI_Conversation_{timestamp}";

        SaveEventLog(Path.Combine(logDirectory, baseFilename + "_Events.csv"));
        SaveConversationHistory(Path.Combine(logDirectory, baseFilename + "_Conversation.csv"));

        Debug.Log($"[Conversation] ✓ Logs saved to {logDirectory}");
    }

    private void SaveEventLog(string filepath)
    {
        try
        {
            StringBuilder csv = new StringBuilder();
            csv.AppendLine("Timestamp(s),Event Type,Details,Exchange Count,Info Discovered");
            foreach (var entry in conversationLog)
            {
                csv.AppendLine($"{entry.timestamp:F2},{entry.eventType},\"{entry.details}\",{entry.exchangeCount},{entry.infoDiscovered}");
            }

            float duration = conversationLog.Count > 0 ? conversationLog[conversationLog.Count - 1].timestamp : 0f;
            int totalInfo = taskManager != null ? taskManager.TotalTasksCount : 8;
            csv.AppendLine("");
            csv.AppendLine("=== SUMMARY ===");
            csv.AppendLine($"Duration (minutes),{duration / 60f:F2}");
            csv.AppendLine($"Total Exchanges,{exchangeCount}");
            csv.AppendLine($"Info Discovered,{infoDiscoveredCount}/{totalInfo}");
            csv.AppendLine($"Feedback Enabled,{enableFeedback}");
            csv.AppendLine($"Feedback Types,{(enableFeedback ? activeFeedbackTypes.ToString() : "None")}");
            csv.AppendLine($"Feedback Delay,{feedbackDelay}s");

            File.WriteAllText(filepath, csv.ToString());
        }
        catch (Exception e)
        {
            Debug.LogError($"[Conversation] ✗ Failed to save event log: {e.Message}");
        }
    }

    private void SaveConversationHistory(string filepath)
    {
        try
        {
            StringBuilder csv = new StringBuilder();
            csv.AppendLine("Exchange #,Timestamp(s),User Message,AI Response,Response Time(s),Feedback Types Used,Feedback Delay(s),Feedback Start(s),Feedback Duration(s),Feedback Cancelled");

            foreach (var exchange in conversationHistory)
            {
                string feedbackTypesStr = exchange.feedbackTypes.Count > 0
                    ? string.Join("; ", exchange.feedbackTypes)
                    : "None";

                string userMsg = CleanForCSV(exchange.userMessage);
                string aiMsg = CleanForCSV(exchange.aiResponse);

                csv.AppendLine($"{exchange.exchangeNumber}," +
                              $"{exchange.timestamp:F2}," +
                              $"\"{userMsg}\"," +
                              $"\"{aiMsg}\"," +
                              $"{exchange.responseTime:F2}," +
                              $"\"{feedbackTypesStr}\"," +
                              $"{exchange.feedbackDelayUsed:F2}," +
                              $"{(exchange.feedbackStartTime >= 0 ? exchange.feedbackStartTime.ToString("F2") : "N/A")}," +
                              $"{(exchange.feedbackDuration >= 0 ? exchange.feedbackDuration.ToString("F2") : "N/A")}," +
                              $"{exchange.feedbackCancelled}");
            }

            csv.AppendLine("");
            csv.AppendLine("=== CONVERSATION SUMMARY ===");
            csv.AppendLine($"Total Exchanges,{conversationHistory.Count}");
            csv.AppendLine($"Average Response Time,{(conversationHistory.Count > 0 ? conversationHistory.Average(e => e.responseTime) : 0):F2}s");

            File.WriteAllText(filepath, csv.ToString());
        }
        catch (Exception e)
        {
            Debug.LogError($"[Conversation] ✗ Failed to save conversation history: {e.Message}");
        }
    }

    private string CleanForCSV(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text.Replace("\"", "\"\"");
    }

    [System.Serializable]
    private struct ConversationLogEntry
    {
        public float timestamp;
        public string eventType;
        public string details;
        public int exchangeCount;
        public int infoDiscovered;
    }

    [System.Serializable]
    private class ConversationExchange
    {
        public int exchangeNumber;
        public float timestamp;
        public string userMessage;
        public string aiResponse;
        public float responseTime;
        public List<string> feedbackTypes;
        public float feedbackDelayUsed;
        public float feedbackStartTime;
        public float feedbackDuration;
        public bool feedbackCancelled;
    }

    // Public accessors
    public bool IsConversationActive => conversationActive;
    public int ExchangeCount => exchangeCount;
    public int InfoDiscoveredCount => infoDiscoveredCount;
    public bool EnableFeedback => enableFeedback;
    public FeedbackType ActiveFeedbackTypes => activeFeedbackTypes;

    // Context menu testing
    [ContextMenu("Start Test Conversation")]
    private void TestStart() => StartConversation();

    [ContextMenu("End Conversation")]
    private void TestEnd() => EndConversation("Manual end");

    [ContextMenu("Test Feedback")]
    private void TestFeedback()
    {
        TriggerFeedback("This is a test response");
        Invoke(nameof(StopFeedback), 2f);
    }

    [ContextMenu("Save Log Now")]
    private void TestSaveLog() => SaveConversationLog();

    /// <summary>
    /// Called when avatar starts speaking (from TextToSpeechManager or UnityAndGeminiV3)
    /// </summary>
    public void OnAvatarStartedSpeaking()
    {
        if (avatarAnimationController != null)
        {
            avatarAnimationController.SetTalking();
            LogEvent("AVATAR_TALKING", "Avatar started speaking");
        }
    }

    /// <summary>
    /// Called when avatar finished speaking (from TextToSpeechManager or UnityAndGeminiV3)
    /// </summary>
    public void OnAvatarFinishedSpeaking()
    {
        if (avatarAnimationController != null)
        {
            avatarAnimationController.SetIdle();
            LogEvent("AVATAR_IDLE", "Avatar finished speaking");

            // NEW: Check if we need to end the conversation
            // This ensures we only switch to the Ending UI AFTER the avatar is done talking.
            if (endConversationQueued && conversationActive)
            {
                StartCoroutine(FinalEndSequence());
            }
        }
    }

    private IEnumerator FinalEndSequence()
    {
        // Give a small beat after talking before cutting to black/end screen
        yield return new WaitForSeconds(1.0f);
        EndConversation("Objectives Met / AI Requested End");
    }
}
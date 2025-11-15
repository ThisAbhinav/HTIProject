using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using GoogleTextToSpeech.Scripts;

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
        VisualCueIcon = 1 << 1,         // Spinning/pulsing loading icon
        VisualCueText = 1 << 2, // loading text
        Gesture = 1 << 3          //  gesture feedback (thinking animation)
    }

    [Header("=== EXPERIMENT CONFIGURATION ===")]
    [SerializeField] private bool enableFeedback = true; // Main control/experiment toggle
    [SerializeField] private FeedbackType activeFeedbackTypes = FeedbackType.VerbalFiller | FeedbackType.VisualCueIcon | FeedbackType.VisualCueText | FeedbackType.Gesture;
    
    [Header("=== Component References ===")]
    [SerializeField] private TextToSpeechManager ttsManager;
    [SerializeField] private TaskManager taskManager;
    [SerializeField] private GameObject visualCuePrefab; 
    [SerializeField] private TextMeshProUGUI visualCueText; // "Thinking..." text
    [SerializeField] private AvatarAnimationController avatarAnimationController; // Avatar animation control
    
    [Header("=== Timing Settings ===")]
    [SerializeField] private float targetDurationMinutes = 10f;
    [SerializeField] private float minDurationMinutes = 5f;
    [SerializeField] private float maxDurationMinutes = 15f;
    [SerializeField] private bool enableTimeLimits = true;
    
    [Header("=== Feedback Delay Settings ===")]
    [Tooltip("Delay in seconds before feedback is triggered. This doesn't delay LLM processing, only the feedback UI.")]
    [SerializeField] private float feedbackDelay = 0.5f;
    
    [Header("=== Visual Cue Settings ===")]
    [SerializeField] private string[] thinkingMessages = new string[]
    {
        "Thinking...",
        "Processing...",
        "Hmm...",
        "Let me think..."
    };
    
    [Header("=== Info Discovery Settings ===")]
    [SerializeField] private int minInfoDiscovered = 3;
    [SerializeField] private int targetInfoDiscovered = 5;
    
    [Header("=== Closing Settings ===")]
    [SerializeField] private bool enableNaturalClosing = true;
    [SerializeField] private float closingWindowStartPercent = 0.7f;
    
    [Header("=== Data Logging ===")]
    [SerializeField] private string logDirectory = "Assets/ExperimentLogs";
    [SerializeField] private bool autoSaveOnEnd = true;

    // State tracking
    private float conversationStartTime;
    private int exchangeCount = 0;
    private int infoDiscoveredCount = 0;
    private bool conversationActive = false;
    private bool inClosingWindow = false;
    private bool shouldStartClosing = false;
    private bool feedbackActive = false;
    private float lastLogTime = 0f;
    private float logInterval = 60f;
    private Coroutine feedbackCoroutine;
    
    // Logging data
    private List<ConversationLogEntry> conversationLog = new List<ConversationLogEntry>();
    private Dictionary<string, float> feedbackTimings = new Dictionary<string, float>();
    
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
        TaskManager.OnTaskCompleted += OnInfoDiscovered;
        TaskManager.OnTaskProgressChanged += OnInfoProgressChanged;
        visualCuePrefab.SetActive(false);
        avatarAnimationController.SetIdle();
        
    }

    private void OnDestroy()
    {
        ChatManager.OnMessageAdded -= OnMessageReceived;
        TaskManager.OnTaskCompleted -= OnInfoDiscovered;
        TaskManager.OnTaskProgressChanged -= OnInfoProgressChanged;
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
        
        float timePercent = Mathf.Clamp01(elapsedMinutes / targetDurationMinutes);

        // Check closing window
        if (enableNaturalClosing && !inClosingWindow && timePercent >= closingWindowStartPercent)
        {
            inClosingWindow = true;
            Debug.Log($"[Conversation] Entering closing window at {elapsedMinutes:F1} minutes");
            LogEvent("CLOSING_WINDOW_ENTERED", $"Time: {elapsedMinutes:F2}m");
            CheckShouldStartClosing();
        }

        // Hard time limit
        if (enableTimeLimits && elapsedMinutes >= maxDurationMinutes)
        {
            EndConversation("Maximum time reached");
        }

    }

    public void StartConversation()
    {
        conversationStartTime = Time.time;
        conversationActive = true;
        exchangeCount = 0;
        infoDiscoveredCount = 0;
        inClosingWindow = false;
        shouldStartClosing = false;
        lastLogTime = 0f;
        conversationLog.Clear();
        feedbackTimings.Clear();

        // Reset TaskManager tasks
        if (taskManager != null)
        {
            taskManager.ResetAllTasks();
        }

        string feedbackStatus = enableFeedback ? activeFeedbackTypes.ToString() : "None (Control)";
        Debug.Log($"[Conversation] ▶ STARTED | Target: {targetDurationMinutes}m | Feedback: {feedbackStatus}");
        
        LogEvent("CONVERSATION_START", $"Feedback: {feedbackStatus}");
        OnConversationStart?.Invoke();
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
            LogEvent("USER_MESSAGE", message.message);
        }
        else if (message.type == MessageType.AI)
        {
            exchangeCount++;
            LogEvent("AI_RESPONSE", message.message);
        }

        if (inClosingWindow)
        {
            CheckShouldStartClosing();
        }
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
        // Wait for the specified delay
        yield return new WaitForSeconds(feedbackDelay);

        feedbackActive = true;
        float feedbackStartTime = Time.time;
        
        // Select appropriate filler based on response preview
        string selectedFiller = SelectAppropriateFillerWord(llmResponsePreview);
        
        // Verbal Filler
        if (activeFeedbackTypes.HasFlag(FeedbackType.VerbalFiller) && ttsManager != null)
        {
            ttsManager.PlayFiller();
            LogEvent("FEEDBACK_VERBAL", $"Filler: {selectedFiller}");
        }

        // Visual Cue Icon
        if (activeFeedbackTypes.HasFlag(FeedbackType.VisualCueIcon))
        {
            ShowVisualCueIcon();
            LogEvent("FEEDBACK_VISUAL_ICON", "Loading icon shown");
        }

        // Visual Cue with Text
        if (activeFeedbackTypes.HasFlag(FeedbackType.VisualCueText))
        {
            ShowVisualCueText();
            LogEvent("FEEDBACK_VISUAL_WITH_TEXT", "Loading icon with text shown");
        }
        
        // Gesture (Thinking Animation)
        if (activeFeedbackTypes.HasFlag(FeedbackType.Gesture))
        {
            ShowThinkingGesture();
            LogEvent("FEEDBACK_GESTURE", "Thinking animation triggered");
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
            
            // Return to idle if gesture was going to be triggered
            if (activeFeedbackTypes.HasFlag(FeedbackType.Gesture))
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
        visualCuePrefab.SetActive(true);

    }

    private void ShowVisualCueText()
    {
        int randomIndex = UnityEngine.Random.Range(0, thinkingMessages.Length);
        visualCueText.text = thinkingMessages[randomIndex];
    }

    private void HideVisualCueIcon()
    {
        visualCuePrefab.SetActive(false);
    }
    private void HideVisualCueText()
    {
        visualCueText.text ="";
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
        
        if (inClosingWindow)
        {
            CheckShouldStartClosing();
        }
    }

    /// <summary>
    /// Called by TaskManager when progress changes
    /// </summary>
    private void OnInfoProgressChanged(int completed, int total)
    {
        infoDiscoveredCount = completed;
    }

    private void CheckShouldStartClosing()
    {
        if (taskManager == null) return;

        float elapsed = (Time.time - conversationStartTime) / 60f;
        bool minTimeMet = elapsed >= minDurationMinutes;
        bool minInfoMet = taskManager.HasMetMinimumInfo(minInfoDiscovered);

        shouldStartClosing = minTimeMet && minInfoMet;

        if (shouldStartClosing)
        {
            Debug.Log($"[Conversation] ✓ Ready to close - Time: {elapsed:F1}m, Info: {infoDiscoveredCount}/{taskManager.TotalTasksCount}");
        }
    }

    /// <summary>
    /// Get conversation state prompt for LLM context injection
    /// </summary>
    public string GetConversationStatePrompt()
    {
        if (!conversationActive) return "";

        float elapsed = (Time.time - conversationStartTime) / 60f;
        float timePercent = elapsed / targetDurationMinutes;

        if (timePercent < 0.4f)
        {
            return "\n\n[CONVERSATION PHASE: Opening - Be warm, engaging, and ask questions to learn about their background and college life. Show genuine curiosity.]";
        }
        else if (timePercent < 0.7f)
        {
            return "\n\n[CONVERSATION PHASE: Middle - Keep dialogue flowing naturally. Share your own experiences and make comparisons. Ask follow-up questions.]";
        }
        else if (shouldStartClosing)
        {
            return "\n\n[CONVERSATION PHASE: Closing - Begin wrapping up naturally. Suggest staying in touch or express that it was great talking. Be warm but prepare to conclude.]";
        }
        else
        {
            return "\n\n[CONVERSATION PHASE: Late - Continue engaging but be mindful that the conversation should naturally wind down soon.]";
        }
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

    /// <summary>
    /// Save conversation log as CSV
    /// </summary>
    public void SaveConversationLog()
    {
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        string filename = $"HTI_Conversation_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        string filepath = Path.Combine(logDirectory, filename);

        try
        {
            StringBuilder csv = new StringBuilder();
            
            // Header
            csv.AppendLine("Timestamp(s),Event Type,Details,Exchange Count,Info Discovered");
            
            // Data rows
            foreach (var entry in conversationLog)
            {
                csv.AppendLine($"{entry.timestamp:F2},{entry.eventType},\"{entry.details}\",{entry.exchangeCount},{entry.infoDiscovered}");
            }
            
            // Summary
            float duration = conversationLog.Count > 0 ? conversationLog[conversationLog.Count - 1].timestamp : 0f;
            int totalInfo = taskManager != null ? taskManager.TotalTasksCount : 8;
            csv.AppendLine("");
            csv.AppendLine("=== SUMMARY ===");
            csv.AppendLine($"Duration (minutes),{duration / 60f:F2}");
            csv.AppendLine($"Total Exchanges,{exchangeCount}");
            csv.AppendLine($"Info Discovered,{infoDiscoveredCount}/{totalInfo}");
            csv.AppendLine($"Feedback Enabled,{enableFeedback}");
            csv.AppendLine($"Feedback Types,{(enableFeedback ? activeFeedbackTypes.ToString() : "None")}");

            File.WriteAllText(filepath, csv.ToString());
            Debug.Log($"[Conversation] ✓ Log saved: {filepath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Conversation] ✗ Failed to save log: {e.Message}");
        }
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
        }
    }
}

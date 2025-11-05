using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Manages conversation timing and natural ending detection
/// Ensures conversations have appropriate length while ending naturally
/// </summary>
public class ConversationManager : MonoBehaviour
{
    [Header("Timing Settings")]
    [SerializeField] private float targetDurationMinutes = 10f;
    [SerializeField] private float minDurationMinutes = 5f;
    [SerializeField] private float maxDurationMinutes = 15f;
    [SerializeField] private bool enableTimeLimits = true;
    
    [Header("Conversation Goals (Exchange count not used - time-based only)")]
    [SerializeField][HideInInspector] private int minExchanges = 10; // Not used
    [SerializeField][HideInInspector] private int targetExchanges = 20; // Not used
    
    [Header("Background Info Discovery")]
    [SerializeField] private List<string> backgroundInfoToDiscover = new List<string>
    {
        "Lives in dorms",
        "Has roommate named Jake",
        "Works as teaching assistant",
        "Member of CS club and Robotics team",
        "Enjoys hiking",
        "Favorite campus spot",
        "Dining hall food habits",
        "Takes BART on weekends"
    };
    
    [SerializeField] private int minInfoDiscovered = 3; // Min topics covered
    [SerializeField] private int targetInfoDiscovered = 5; // Target topics
    
    [Header("Closing Signals")]
    [SerializeField] private bool enableNaturalClosing = true;
    [SerializeField] private float closingWindowStartPercent = 0.7f; // Start looking for closing after 70% time
    
    private float conversationStartTime;
    private int exchangeCount = 0;
    private HashSet<string> discoveredInfo = new HashSet<string>();
    private bool conversationActive = false;
    private bool inClosingWindow = false;
    private bool shouldStartClosing = false;

    public static ConversationManager Instance { get; private set; }
    public static event Action OnConversationStart;
    public static event Action OnConversationEnd;
    public static event Action<float> OnTimeUpdate; // Sends remaining time percentage
    public static event Action<string> OnInfoDiscovered;

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
    }

    private void OnDestroy()
    {
        ChatManager.OnMessageAdded -= OnMessageReceived;
    }

    private float lastLogTime = 0f;
    private float logInterval = 60f; // Log every minute

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
            Debug.Log($"[Conversation Progress] {mins} min | Exchanges: {exchangeCount} | Info Discovered: {discoveredInfo.Count}/{backgroundInfoToDiscover.Count}");
        }
        
        // Update time percentage
        float timePercent = Mathf.Clamp01(elapsedMinutes / targetDurationMinutes);
        OnTimeUpdate?.Invoke(timePercent);

        // Check if we've entered the closing window
        if (enableNaturalClosing && !inClosingWindow && timePercent >= closingWindowStartPercent)
        {
            inClosingWindow = true;
            Debug.Log($"[Conversation Manager] Entering closing window at {elapsedMinutes:F1} minutes");
            CheckShouldStartClosing();
        }

        // Hard time limit (safety)
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
        discoveredInfo.Clear();
        inClosingWindow = false;
        shouldStartClosing = false;
        lastLogTime = 0f;

        Debug.Log($"[Conversation Manager] ▶ Started - Target: {targetDurationMinutes} min | Min: {minDurationMinutes} min | Max: {maxDurationMinutes} min");
        OnConversationStart?.Invoke();
    }

    public void EndConversation(string reason)
    {
        if (!conversationActive) return;

        conversationActive = false;
        float duration = (Time.time - conversationStartTime) / 60f;
        
        Debug.Log($"[Conversation Manager] ■ Ended - Reason: {reason}");
        Debug.Log($"[Conversation Stats] Duration: {duration:F2} min | Exchanges: {exchangeCount} | Info: {discoveredInfo.Count}/{backgroundInfoToDiscover.Count}");
        OnConversationEnd?.Invoke();
    }

    private void OnMessageReceived(ChatMessage message)
    {
        if (!conversationActive) return;

        // Count exchanges (user + AI response = 1 exchange)
        if (message.type == MessageType.User || message.type == MessageType.AI)
        {
            if (message.type == MessageType.AI)
            {
                exchangeCount++;
            }

            // Check for background info discovery in AI messages
            if (message.type == MessageType.AI)
            {
                CheckForInfoDiscovery(message.message);
            }
        }

        // Update closing status
        if (inClosingWindow)
        {
            CheckShouldStartClosing();
        }
    }

    private void CheckForInfoDiscovery(string message)
    {
        foreach (string info in backgroundInfoToDiscover)
        {
            if (!discoveredInfo.Contains(info))
            {
                // Simple keyword matching (you can make this more sophisticated)
                if (MessageContainsInfo(message, info))
                {
                    discoveredInfo.Add(info);
                    Debug.Log($"[Info Discovered] ✓ {info} ({discoveredInfo.Count}/{backgroundInfoToDiscover.Count})");
                    OnInfoDiscovered?.Invoke(info);
                    
                    // Check if we should start closing now that we have more info
                    if (inClosingWindow)
                    {
                        CheckShouldStartClosing();
                    }
                }
            }
        }
    }

    private bool MessageContainsInfo(string message, string infoTopic)
    {
        string lowerMessage = message.ToLower();
        string lowerTopic = infoTopic.ToLower();

        // Define keywords for each topic
        Dictionary<string, string[]> topicKeywords = new Dictionary<string, string[]>
        {
            ["lives in dorms"] = new[] { "dorm", "residence hall", "live on campus" },
            ["has roommate named jake"] = new[] { "roommate", "jake" },
            ["works as teaching assistant"] = new[] { "ta", "teaching assistant", "teach" },
            ["member of cs club and robotics team"] = new[] { "cs club", "computer science club", "robotics" },
            ["enjoys hiking"] = new[] { "hiking", "hike", "trail" },
            ["favorite campus spot"] = new[] { "favorite spot", "favorite place", "library overlooking" },
            ["dining hall food habits"] = new[] { "dining hall", "food truck", "telegraph" },
            ["takes bart on weekends"] = new[] { "bart", "san francisco", "explore sf" }
        };

        if (topicKeywords.TryGetValue(lowerTopic, out string[] keywords))
        {
            foreach (string keyword in keywords)
            {
                if (lowerMessage.Contains(keyword))
                    return true;
            }
        }

        return false;
    }

    private void CheckShouldStartClosing()
    {
        // Criteria for natural closing:
        // 1. In closing window (70%+ of target time)
        // 2. Minimum time met
        // 3. Minimum info discovered (optional)

        float elapsed = (Time.time - conversationStartTime) / 60f;
        bool minTimeMet = elapsed >= minDurationMinutes;
        bool minInfoMet = discoveredInfo.Count >= minInfoDiscovered;

        // Only require time + info, NOT exchanges
        shouldStartClosing = minTimeMet && minInfoMet;

        if (shouldStartClosing)
        {
            Debug.Log($"[Conversation Manager] ✓ Ready to close - Time: {elapsed:F1}m, Exchanges: {exchangeCount}, Info: {discoveredInfo.Count}/{backgroundInfoToDiscover.Count}");
        }
    }

    /// <summary>
    /// Get additional system prompt for current conversation state
    /// </summary>
    public string GetConversationStatePrompt()
    {
        if (!conversationActive) return "";

        float elapsed = (Time.time - conversationStartTime) / 60f;
        float timePercent = elapsed / targetDurationMinutes;

        // Early conversation (0-40%)
        if (timePercent < 0.4f)
        {
            return "\n[CONVERSATION START: Be warm and engaging. Ask about their background and college experience. Show genuine curiosity.]";
        }
        // Middle conversation (40-70%)
        else if (timePercent < 0.7f)
        {
            return "\n[CONVERSATION MIDDLE: Continue dialogue naturally. Share your experiences and compare with theirs. Keep the exchange flowing.]";
        }
        // Closing window (70-100%)
        else if (shouldStartClosing)
        {
            return "\n[CONVERSATION CLOSING: Start wrapping up naturally. You can suggest exchanging contact info, mention it was great talking, or express hope to chat again. Don't abruptly end - be warm and friendly.]";
        }
        else
        {
            return "\n[CONVERSATION LATE: Continue naturally but be ready to close soon once you feel the conversation has covered good ground.]";
        }
    }

    /// <summary>
    /// Check if conversation should naturally close
    /// </summary>
    public bool ShouldStartClosing()
    {
        return shouldStartClosing && inClosingWindow;
    }

    /// <summary>
    /// Get conversation stats for display/logging
    /// </summary>
    public ConversationStats GetStats()
    {
        float duration = conversationActive ? (Time.time - conversationStartTime) / 60f : 0f;
        
        return new ConversationStats
        {
            durationMinutes = duration,
            exchangeCount = exchangeCount,
            infoDiscovered = discoveredInfo.Count,
            totalInfo = backgroundInfoToDiscover.Count,
            isInClosingWindow = inClosingWindow,
            shouldClose = shouldStartClosing
        };
    }

    public bool IsConversationActive => conversationActive;
    public int ExchangeCount => exchangeCount;
    public int InfoDiscoveredCount => discoveredInfo.Count;

    [System.Serializable]
    public struct ConversationStats
    {
        public float durationMinutes;
        public int exchangeCount;
        public int infoDiscovered;
        public int totalInfo;
        public bool isInClosingWindow;
        public bool shouldClose;

        public override string ToString()
        {
            return $"Duration: {durationMinutes:F1}m | Exchanges: {exchangeCount} | Info: {infoDiscovered}/{totalInfo} | Closing: {shouldClose}";
        }
    }
}

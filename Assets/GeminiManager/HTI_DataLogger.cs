using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// HTI Experiment Data Logger
/// Logs conversation data, feedback mode, timestamps for research analysis
/// </summary>
public class HTI_DataLogger : MonoBehaviour
{
    [Header("Logging Settings")]
    [SerializeField] private bool enableLogging = true;
    [SerializeField] private string participantID = "P001";
    [SerializeField] private string sessionID = "";
    
    [Header("Output Settings")]
    [SerializeField] private string logDirectory = "HTI_Logs";
    [SerializeField] private bool logToConsole = true;
    
    private string currentLogFile;
    private List<LogEntry> sessionLogs = new List<LogEntry>();
    private DateTime sessionStartTime;
    private FeedbackModeManager.FeedbackMode currentMode;

    [System.Serializable]
    public class LogEntry
    {
        public string timestamp;
        public string participantID;
        public string sessionID;
        public string feedbackMode;
        public string eventType;
        public string speaker;
        public string message;
        public float responseTime;
        public string additionalData;

        public string ToCSV()
        {
            return $"{timestamp},{participantID},{sessionID},{feedbackMode},{eventType},{speaker},\"{message}\",{responseTime},{additionalData}";
        }
    }

    private void Start()
    {
        if (!enableLogging) return;

        // Generate session ID if not set
        if (string.IsNullOrEmpty(sessionID))
        {
            sessionID = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }

        sessionStartTime = DateTime.Now;
        
        // Subscribe to events
        ChatManager.OnMessageAdded += LogMessage;
        FeedbackModeManager.OnModeChanged += LogModeChange;

        // Get current feedback mode
        var feedbackManager = FindObjectOfType<FeedbackModeManager>();
        if (feedbackManager != null)
        {
            currentMode = feedbackManager.CurrentMode;
        }

        InitializeLogFile();
        LogEvent("SESSION_START", "System", $"Participant: {participantID}, Mode: {currentMode}");
    }

    private void OnDestroy()
    {
        if (!enableLogging) return;

        ChatManager.OnMessageAdded -= LogMessage;
        FeedbackModeManager.OnModeChanged -= LogModeChange;

        LogEvent("SESSION_END", "System", $"Duration: {(DateTime.Now - sessionStartTime).TotalMinutes:F2} minutes");
        SaveLogFile();
    }

    private void InitializeLogFile()
    {
        // Create directory if it doesn't exist
        string fullPath = Path.Combine(Application.persistentDataPath, logDirectory);
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }

        // Create log file
        string filename = $"HTI_{participantID}_{sessionID}.csv";
        currentLogFile = Path.Combine(fullPath, filename);

        // Write CSV header
        string header = "Timestamp,ParticipantID,SessionID,FeedbackMode,EventType,Speaker,Message,ResponseTime,AdditionalData";
        File.WriteAllText(currentLogFile, header + "\n");

        Debug.Log($"[HTI Logger] Log file created: {currentLogFile}");
    }

    private void LogMessage(ChatMessage chatMessage)
    {
        if (!enableLogging) return;

        string speaker = chatMessage.sender;
        string message = chatMessage.message;
        string eventType = chatMessage.type switch
        {
            MessageType.User => "USER_MESSAGE",
            MessageType.AI => "AI_RESPONSE",
            MessageType.System => "SYSTEM_MESSAGE",
            _ => "UNKNOWN"
        };

        LogEvent(eventType, speaker, message);
    }

    private void LogModeChange(FeedbackModeManager.FeedbackMode newMode)
    {
        if (!enableLogging) return;

        currentMode = newMode;
        LogEvent("MODE_CHANGE", "System", $"Changed to: {newMode}");
    }

    public void LogEvent(string eventType, string speaker, string message, float responseTime = 0f, string additionalData = "")
    {
        if (!enableLogging) return;

        LogEntry entry = new LogEntry
        {
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            participantID = participantID,
            sessionID = sessionID,
            feedbackMode = currentMode.ToString(),
            eventType = eventType,
            speaker = speaker,
            message = message.Replace("\"", "\"\""), // Escape quotes for CSV
            responseTime = responseTime,
            additionalData = additionalData
        };

        sessionLogs.Add(entry);

        // Write to file immediately (safer for crashes)
        File.AppendAllText(currentLogFile, entry.ToCSV() + "\n");

        if (logToConsole)
        {
            Debug.Log($"[HTI Log] {eventType}: {speaker} - {message}");
        }
    }

    public void LogLatency(float latencySeconds)
    {
        LogEvent("LATENCY_MEASURED", "System", $"{latencySeconds:F3}s", latencySeconds);
    }

    public void LogFeedbackTriggered()
    {
        LogEvent("FEEDBACK_TRIGGERED", "System", $"Mode: {currentMode}");
    }

    public void LogFeedbackCompleted()
    {
        LogEvent("FEEDBACK_COMPLETED", "System", $"Mode: {currentMode}");
    }

    private void SaveLogFile()
    {
        Debug.Log($"[HTI Logger] Session saved: {sessionLogs.Count} entries logged to {currentLogFile}");
    }

    // Public methods for manual logging
    public void SetParticipantID(string id)
    {
        participantID = id;
        LogEvent("PARTICIPANT_ID_SET", "System", $"New ID: {id}");
    }

    public void LogUserAction(string action)
    {
        LogEvent("USER_ACTION", "User", action);
    }

    public void LogSystemEvent(string eventDescription)
    {
        LogEvent("SYSTEM_EVENT", "System", eventDescription);
    }

    public void LogError(string errorMessage)
    {
        LogEvent("ERROR", "System", errorMessage);
    }

    // Export methods
    public string GetLogFilePath()
    {
        return currentLogFile;
    }

    public List<LogEntry> GetSessionLogs()
    {
        return new List<LogEntry>(sessionLogs);
    }

    [ContextMenu("Open Log Directory")]
    private void OpenLogDirectory()
    {
        string fullPath = Path.Combine(Application.persistentDataPath, logDirectory);
        Application.OpenURL(fullPath);
    }

    [ContextMenu("Print Log File Path")]
    private void PrintLogPath()
    {
        Debug.Log($"Current log file: {currentLogFile}");
    }
}

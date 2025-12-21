using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Manages participant session configurations including fixed question sets
/// Allows replay of sessions with the same questions
/// Tracks questions across all 4 sessions to prevent repetition
/// </summary>
[System.Serializable]
public class SessionConfig
{
    public string participantId;
    public int sessionNumber;
    public List<int> questionIndices; // Indices into TaskManager's masterTaskPool
    public string feedbackType; // Baseline, Gestures, Visual, or Verbal
    public string timestamp;
}

/// <summary>
/// Singleton that manages session configuration.
/// Auto-creates itself if not present in scene.
/// </summary>
[DefaultExecutionOrder(-100)] // Ensure this runs before other scripts
public class SessionConfiguration : MonoBehaviour
{
    private static SessionConfiguration instance;
    private static bool isInitialized = false;
    
    public static SessionConfiguration Instance
    {
        get
        {
            if (instance == null)
            {
                // Try to find existing instance in scene
                instance = FindObjectOfType<SessionConfiguration>();
                
                if (instance == null)
                {
                    // Auto-create if not found
                    GameObject go = new GameObject("SessionConfiguration");
                    instance = go.AddComponent<SessionConfiguration>();
                    Debug.Log("[SessionConfiguration] Auto-created singleton instance");
                }
            }
            
            // Ensure initialization happens even if accessed before Awake
            if (!isInitialized && instance != null)
            {
                instance.Initialize();
            }
            
            return instance;
        }
    }

    [Header("Current Session")]
    public string currentParticipantId = "";
    public int currentSessionNumber = 0;

    private string configDirectory;
    private Dictionary<string, SessionConfig> loadedConfigs = new Dictionary<string, SessionConfig>();
    
    // Counterbalanced session orders for all 20 participants
    // Based on Latin Square design: Baseline=1, Gestures=2, Visual=3, Verbal=4
    private Dictionary<string, int[]> participantSessionOrders = new Dictionary<string, int[]>
    {
        { "P01", new[] { 1, 2, 3, 4 } }, // Baseline, Gestures, Visual, Verbal
        { "P02", new[] { 2, 3, 4, 1 } }, // Gestures, Visual, Verbal, Baseline
        { "P03", new[] { 3, 4, 1, 2 } }, // Visual, Verbal, Baseline, Gestures
        { "P04", new[] { 4, 1, 2, 3 } }, // Verbal, Baseline, Gestures, Visual
        { "P05", new[] { 1, 2, 3, 4 } },
        { "P06", new[] { 2, 3, 4, 1 } },
        { "P07", new[] { 3, 4, 1, 2 } },
        { "P08", new[] { 4, 1, 2, 3 } },
        { "P09", new[] { 1, 2, 3, 4 } },
        { "P10", new[] { 2, 3, 4, 1 } },
        { "P11", new[] { 3, 4, 1, 2 } },
        { "P12", new[] { 4, 1, 2, 3 } },
        { "P13", new[] { 1, 2, 3, 4 } },
        { "P14", new[] { 2, 3, 4, 1 } },
        { "P15", new[] { 3, 4, 1, 2 } },
        { "P16", new[] { 4, 1, 2, 3 } },
        { "P17", new[] { 1, 2, 3, 4 } },
        { "P18", new[] { 2, 3, 4, 1 } },
        { "P19", new[] { 3, 4, 1, 2 } },
        { "P20", new[] { 4, 1, 2, 3 } },
        { "P21", new[] { 1, 2, 3, 4 } }, // Baseline, Gestures, Visual, Verbal
        { "P22", new[] { 2, 3, 4, 1 } }, // Gestures, Visual, Verbal, Baseline
        { "P23", new[] { 3, 4, 1, 2 } }, // Visual, Verbal, Baseline, Gestures
        { "P24", new[] { 4, 1, 2, 3 } }, // Verbal, Baseline, Gestures, Visual
        
    };

    private string[] feedbackTypeNames = new string[] { "", "Baseline", "Gestures", "Visual", "Verbal" };

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }

        Initialize();
    }

    private void Initialize()
    {
        if (isInitialized) return;

        configDirectory = Path.Combine("", "./Assets/ExperimentLogs/SessionConfigs");

        if (!Directory.Exists(configDirectory))
        {
            Directory.CreateDirectory(configDirectory);
        }

        isInitialized = true;
    }

    /// <summary>
    /// Set the current participant and session
    /// </summary>
    public void SetSession(string participantId, int sessionNumber)
    {
        currentParticipantId = participantId.ToUpper();
        currentSessionNumber = sessionNumber;
        
        string feedbackType = GetFeedbackTypeForSession(participantId, sessionNumber);
        Debug.Log($"[SessionConfig] Set to {currentParticipantId} - Session {currentSessionNumber} ({feedbackType})");
    }
    
    /// <summary>
    /// Get the feedback type for a specific participant and session
    /// </summary>
    public string GetFeedbackTypeForSession(string participantId, int sessionNumber)
    {
        string pid = participantId.ToUpper();
        
        if (!participantSessionOrders.ContainsKey(pid))
        {
            Debug.LogWarning($"[SessionConfig] Unknown participant ID: {pid}. Using Baseline as default.");
            return "Baseline";
        }
        
        if (sessionNumber < 1 || sessionNumber > 4)
        {
            Debug.LogWarning($"[SessionConfig] Invalid session number: {sessionNumber}. Must be 1-4.");
            return "Unknown";
        }
        
        int feedbackTypeId = participantSessionOrders[pid][sessionNumber - 1];
        return feedbackTypeNames[feedbackTypeId];
    }
    
    /// <summary>
    /// Get the feedback type for the current session
    /// </summary>
    public string GetCurrentFeedbackType()
    {
        return GetFeedbackTypeForSession(currentParticipantId, currentSessionNumber);
    }

    /// <summary>
    /// Save the question configuration for the current session
    /// </summary>
    public void SaveSessionConfig(List<int> questionIndices)
    {
        // Ensure initialized
        if (!isInitialized) Initialize();
        
        if (string.IsNullOrEmpty(currentParticipantId) || currentSessionNumber == 0)
        {
            Debug.LogWarning("[SessionConfig] Cannot save: Participant ID or Session Number not set");
            return;
        }

        SessionConfig config = new SessionConfig
        {
            participantId = currentParticipantId,
            sessionNumber = currentSessionNumber,
            questionIndices = new List<int>(questionIndices),
            feedbackType = GetCurrentFeedbackType(),
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        string filename = GetConfigFilename(currentParticipantId, currentSessionNumber);
        string filepath = Path.Combine(configDirectory, filename);

        try
        {
            // Ensure directory exists
            if (!Directory.Exists(configDirectory))
            {
                Directory.CreateDirectory(configDirectory);
                Debug.Log($"[SessionConfig] Created directory: {configDirectory}");
            }
            
            string json = JsonUtility.ToJson(config, true);
            File.WriteAllText(filepath, json);
            
            // Verify file was written
            if (File.Exists(filepath))
            {
                Debug.Log($"[SessionConfig] Saved to: {filepath}");
                Debug.Log($"[SessionConfig] Content: {json}");
            }
            else
            {
                Debug.LogError($"[SessionConfig] File was NOT created at: {filepath}");
            }
            
            string key = GetConfigKey(currentParticipantId, currentSessionNumber);
            loadedConfigs[key] = config;

            Debug.Log($"[SessionConfig] Saved: {currentParticipantId} S{currentSessionNumber} ({config.feedbackType}) -> Questions: [{string.Join(", ", questionIndices)}]");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SessionConfig] Failed to save to {filepath}: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// Load the question configuration for the current session
    /// Returns null if no config exists
    /// </summary>
    public List<int> LoadSessionConfig()
    {
        if (string.IsNullOrEmpty(currentParticipantId) || currentSessionNumber == 0)
        {
            Debug.LogWarning("[SessionConfig] Cannot load: Participant ID or Session Number not set");
            return null;
        }

        string key = GetConfigKey(currentParticipantId, currentSessionNumber);
        
        // Check if already loaded in memory
        if (loadedConfigs.ContainsKey(key))
        {
            Debug.Log($"[SessionConfig] Loaded from cache: {currentParticipantId} S{currentSessionNumber}");
            return loadedConfigs[key].questionIndices;
        }

        // Try to load from file
        string filename = GetConfigFilename(currentParticipantId, currentSessionNumber);
        string filepath = Path.Combine(configDirectory, filename);

        if (!File.Exists(filepath))
        {
            Debug.Log($"[SessionConfig] No saved config found for {currentParticipantId} S{currentSessionNumber}");
            return null;
        }

        try
        {
            string json = File.ReadAllText(filepath);
            SessionConfig config = JsonUtility.FromJson<SessionConfig>(json);
            loadedConfigs[key] = config;

            Debug.Log($"[SessionConfig] Loaded: {currentParticipantId} S{currentSessionNumber} -> Questions: [{string.Join(", ", config.questionIndices)}]");
            return config.questionIndices;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SessionConfig] Failed to load: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Check if a configuration exists for the current session
    /// </summary>
    public bool HasSavedConfig()
    {
        if (string.IsNullOrEmpty(currentParticipantId) || currentSessionNumber == 0)
            return false;

        string key = GetConfigKey(currentParticipantId, currentSessionNumber);
        if (loadedConfigs.ContainsKey(key))
            return true;

        string filename = GetConfigFilename(currentParticipantId, currentSessionNumber);
        string filepath = Path.Combine(configDirectory, filename);
        return File.Exists(filepath);
    }

    /// <summary>
    /// Delete the configuration for a specific session
    /// </summary>
    public void DeleteSessionConfig(string participantId, int sessionNumber)
    {
        string key = GetConfigKey(participantId, sessionNumber);
        loadedConfigs.Remove(key);

        string filename = GetConfigFilename(participantId, sessionNumber);
        string filepath = Path.Combine(configDirectory, filename);

        if (File.Exists(filepath))
        {
            File.Delete(filepath);
            Debug.Log($"[SessionConfig] Deleted: {participantId} S{sessionNumber}");
        }
    }

    private string GetConfigKey(string participantId, int sessionNumber)
    {
        return $"{participantId}_S{sessionNumber}";
    }

    private string GetConfigFilename(string participantId, int sessionNumber)
    {
        return $"{participantId}_Session{sessionNumber}_Config.json";
    }

    /// <summary>
    /// Get all question indices already used by this participant in previous sessions
    /// </summary>
    public List<int> GetUsedQuestionIndices(string participantId)
    {
        List<int> usedIndices = new List<int>();
        
        if (!Directory.Exists(configDirectory))
            return usedIndices;

        // Search for all config files for this participant
        string searchPattern = $"{participantId.ToUpper()}_Session*_Config.json";
        string[] files = Directory.GetFiles(configDirectory, searchPattern);
        
        foreach (string file in files)
        {
            try
            {
                string json = File.ReadAllText(file);
                SessionConfig config = JsonUtility.FromJson<SessionConfig>(json);
                
                if (config != null && config.questionIndices != null)
                {
                    foreach (int index in config.questionIndices)
                    {
                        if (!usedIndices.Contains(index))
                        {
                            usedIndices.Add(index);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SessionConfig] Failed to read {file}: {e.Message}");
            }
        }
        
        return usedIndices;
    }

    /// <summary>
    /// Get available (unused) question indices for a participant
    /// </summary>
    public List<int> GetAvailableQuestionIndices(string participantId, int totalQuestions)
    {
        List<int> usedIndices = GetUsedQuestionIndices(participantId);
        List<int> availableIndices = new List<int>();
        
        for (int i = 0; i < totalQuestions; i++)
        {
            if (!usedIndices.Contains(i))
            {
                availableIndices.Add(i);
            }
        }
        
        return availableIndices;
    }
    
    /// <summary>
    /// Get which sessions have been completed for a participant
    /// </summary>
    public List<int> GetCompletedSessions(string participantId)
    {
        List<int> completedSessions = new List<int>();
        
        if (!Directory.Exists(configDirectory))
            return completedSessions;

        for (int session = 1; session <= 4; session++)
        {
            string filename = GetConfigFilename(participantId.ToUpper(), session);
            string filepath = Path.Combine(configDirectory, filename);
            
            if (File.Exists(filepath))
            {
                completedSessions.Add(session);
            }
        }
        
        return completedSessions;
    }
    
    /// <summary>
    /// Get session progress summary for a participant
    /// </summary>
    public string GetSessionProgressSummary(string participantId)
    {
        List<int> completed = GetCompletedSessions(participantId);
        int questionsUsed = GetUsedQuestionIndices(participantId).Count;
        
        return $"{participantId}: {completed.Count}/4 sessions completed, {questionsUsed}/20 questions used";
    }

    /// <summary>
    /// List all saved configurations
    /// </summary>
    public List<string> GetAllSavedConfigs()
    {
        List<string> configs = new List<string>();
        
        if (!Directory.Exists(configDirectory))
            return configs;

        string[] files = Directory.GetFiles(configDirectory, "*_Config.json");
        foreach (string file in files)
        {
            string filename = Path.GetFileNameWithoutExtension(file);
            configs.Add(filename.Replace("_Config", ""));
        }

        return configs;
    }
}

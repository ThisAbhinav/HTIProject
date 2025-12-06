using UnityEngine;

/// <summary>
/// Simple session setup - set participant ID and session number directly in Inspector
/// </summary>
public class SessionSetupUI : MonoBehaviour
{
    [Header("Session Configuration")]
    [Tooltip("Enter participant ID (e.g., P01, P02, ... P20)")]
    [SerializeField] private string participantId = "P01";
    
    [Tooltip("Select session number (1-4)")]
    [SerializeField] [Range(1, 4)] private int sessionNumber = 1;
    
    [Header("Scene Management")]
    [SerializeField] private UnityAndGeminiV3 geminiManager;

    private void Start()
    {
        StartSession();
    }

    private void StartSession()
    {
        // Validate and format participant ID
        string pid = participantId.Trim().ToUpper();
        
        if (string.IsNullOrEmpty(pid))
        {
            Debug.LogError("[SessionSetup] Participant ID is empty! Please set it in Inspector.");
            return;
        }

        // Set the session configuration
        SessionConfiguration.Instance.SetSession(pid, sessionNumber);

        // Get feedback type
        string feedbackType = SessionConfiguration.Instance.GetCurrentFeedbackType();
        
        // Check if there's a saved config
        bool hasSavedConfig = SessionConfiguration.Instance.HasSavedConfig();
        string configStatus = hasSavedConfig ? "(Reusing saved questions)" : "(New questions)";

        // Get session progress
        string progress = SessionConfiguration.Instance.GetSessionProgressSummary(pid);
        
        Debug.Log($"[SessionSetup] Starting {pid} - Session {sessionNumber} - {feedbackType} {configStatus}");
        Debug.Log($"[SessionSetup] {progress}");

        // Start the conversation
        if (geminiManager != null)
        {
            geminiManager.StartIntro();
        }
        else
        {
            Debug.LogError("[SessionSetupUI] GeminiManager reference not set in Inspector!");
        }
    }

    /// <summary>
    /// Restart with current settings
    /// </summary>
    [ContextMenu("Restart Session")]
    public void RestartSession()
    {
        StartSession();
    }
}

using UnityEngine;

/// <summary>
/// Simple session setup - set participant ID and session number directly in Inspector
/// IMPORTANT: This script sets the session configuration that TaskManager depends on.
/// </summary>
[DefaultExecutionOrder(-50)] // Run after SessionConfiguration (-100) but before TaskManager (0)
public class SessionSetupUI : MonoBehaviour
{
    [Header("Session Configuration")]
    [Tooltip("Enter participant ID (e.g., P01, P02, ... P20)")]
    [SerializeField] private string participantId = "P01";
    
    [Tooltip("Select session number (1-4)")]
    [SerializeField] [Range(1, 4)] private int sessionNumber = 1;
    
    private void Awake()
    {
        // Configure session in Awake to ensure it runs before TaskManager.Start()
        ConfigureSession();
    }

    private void ConfigureSession()
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
        
        Debug.Log($"[SessionSetup] Configured {pid} - Session {sessionNumber} - {feedbackType} {configStatus}");
        Debug.Log($"[SessionSetup] {progress}");
    }

    /// <summary>
    /// Restart with current settings
    /// </summary>
    [ContextMenu("Restart Session")]
    public void RestartSession()
    {
        ConfigureSession();
    }
}

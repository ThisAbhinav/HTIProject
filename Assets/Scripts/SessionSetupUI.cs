using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// UI Manager for setting participant ID and session number before starting experiment
/// </summary>
public class SessionSetupUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField participantIdInput;
    [SerializeField] private TMP_Dropdown sessionNumberDropdown;
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject setupPanel;
    
    [Header("Scene Management")]
    [SerializeField] private UnityAndGeminiV3 geminiManager;

    private void Start()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
        }

        // Setup dropdown with session numbers 1-4
        if (sessionNumberDropdown != null)
        {
            sessionNumberDropdown.ClearOptions();
            sessionNumberDropdown.AddOptions(new System.Collections.Generic.List<string> 
            { 
                "Session 1", 
                "Session 2", 
                "Session 3", 
                "Session 4" 
            });
        }

        UpdateStatusText("");
    }

    private void OnStartButtonClicked()
    {
        string participantId = participantIdInput?.text.Trim().ToUpper();
        int sessionNumber = sessionNumberDropdown != null ? sessionNumberDropdown.value + 1 : 1;

        // Validate input
        if (string.IsNullOrEmpty(participantId))
        {
            UpdateStatusText("Please enter a Participant ID");
            return;
        }

        // Set the session configuration
        SessionConfiguration.Instance.SetSession(participantId, sessionNumber);

        // Get feedback type
        string feedbackType = SessionConfiguration.Instance.GetCurrentFeedbackType();
        
        // Check if there's a saved config
        bool hasSavedConfig = SessionConfiguration.Instance.HasSavedConfig();
        string configStatus = hasSavedConfig ? " (Reusing saved questions)" : " (New questions)";

        // Get session progress
        string progress = SessionConfiguration.Instance.GetSessionProgressSummary(participantId);
        Debug.Log($"[SessionSetup] {progress}");

        UpdateStatusText($"Starting {participantId} - S{sessionNumber} - {feedbackType}{configStatus}");

        // Hide setup panel
        if (setupPanel != null)
        {
            setupPanel.SetActive(false);
        }

        // Start the conversation
        if (geminiManager != null)
        {
            geminiManager.StartIntro();
        }
        else
        {
            Debug.LogError("[SessionSetupUI] GeminiManager reference not set!");
        }
    }

    private void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = string.IsNullOrEmpty(message) ? Color.white : Color.yellow;
        }
    }

    /// <summary>
    /// For testing in editor
    /// </summary>
    [ContextMenu("Quick Start - P01 S1")]
    public void QuickStartP01S1()
    {
        SessionConfiguration.Instance.SetSession("P01", 1);
        if (geminiManager != null) geminiManager.StartIntro();
    }

    [ContextMenu("Quick Start - P01 S3")]
    public void QuickStartP01S3()
    {
        SessionConfiguration.Instance.SetSession("P01", 3);
        if (geminiManager != null) geminiManager.StartIntro();
    }
}

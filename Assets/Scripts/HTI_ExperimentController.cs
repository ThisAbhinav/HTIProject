using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simplified UI Controller for HTI Experiment
/// Allows test runners to toggle feedback on/off (control vs experiment condition)
/// </summary>
public class HTI_ExperimentController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Toggle feedbackToggle;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private TMP_Text statsText;
    
    [Header("Settings")]
    [SerializeField] private bool showSettingsAtStart = true;
    [SerializeField] private KeyCode toggleSettingsKey = KeyCode.F1;
    [SerializeField] private KeyCode saveLogKey = KeyCode.F3;
    
    private ConversationManager conversationManager;

    private void Start()
    {
        conversationManager = ConversationManager.Instance;
        
        if (conversationManager == null)
        {
            Debug.LogError("[HTI Controller] ConversationManager not found in scene!");
            return;
        }

        SetupUI();
        
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(showSettingsAtStart);
        }
    }

    private void Update()
    {
        // Toggle settings panel with F1
        if (Input.GetKeyDown(toggleSettingsKey) && settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }
        
        // Quick save log with F3
        if (Input.GetKeyDown(saveLogKey) && conversationManager != null)
        {
            conversationManager.SaveConversationLog();
            UpdateStatusText("✓ Log saved manually");
        }
        
        // Update stats display
        UpdateStatsDisplay();
    }

    private void SetupUI()
    {
        // Setup feedback toggle
        if (feedbackToggle != null)
        {
            feedbackToggle.isOn = conversationManager.EnableFeedback;
            feedbackToggle.onValueChanged.AddListener(OnFeedbackToggleChanged);
        }

        UpdateStatusText();
    }

    private void OnFeedbackToggleChanged(bool enabled)
    {
        // This requires ConversationManager to have public setter
        string condition = enabled ? "EXPERIMENT (Feedback ON)" : "CONTROL (No Feedback)";
        UpdateStatusText($"Mode: {condition}");
        Debug.Log($"[HTI Experiment] Condition Changed: {condition}");
    }

    private void UpdateStatusText(string message = null)
    {
        if (statusText == null) return;

        if (message != null)
        {
            statusText.text = message;
        }
        else if (conversationManager != null)
        {
            string condition = conversationManager.EnableFeedback ? "Experiment" : "Control";
            string feedbackTypes = conversationManager.EnableFeedback 
                ? conversationManager.ActiveFeedbackTypes.ToString() 
                : "None";
            statusText.text = $"Condition: {condition}\nFeedback: {feedbackTypes}";
        }
    }
    
    private void UpdateStatsDisplay()
    {
        if (statsText == null || conversationManager == null) return;
        
        if (!conversationManager.IsConversationActive)
        {
            statsText.text = "Waiting to start...";
            return;
        }
        
        statsText.text = $"Exchanges: {conversationManager.ExchangeCount}\n" +
                        $"Info Discovered: {conversationManager.InfoDiscoveredCount}";
    }

    /// <summary>
    /// Manual controls for testing
    /// </summary>
    [ContextMenu("Start Conversation")]
    public void StartConversation()
    {
        if (conversationManager != null)
            conversationManager.StartConversation();
    }

    [ContextMenu("End Conversation")]
    public void EndConversation()
    {
        if (conversationManager != null)
            conversationManager.EndConversation("Manual end by controller");
    }

    [ContextMenu("Save Log")]
    public void SaveLog()
    {
        if (conversationManager != null)
            conversationManager.SaveConversationLog();
    }
}

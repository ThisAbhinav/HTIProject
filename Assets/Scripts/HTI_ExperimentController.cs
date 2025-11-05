using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI Controller for HTI Experiment
/// Allows test runners to select feedback mode before/during experiments
/// </summary>
public class HTI_ExperimentController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Dropdown feedbackModeDropdown;
    [SerializeField] private Button applyButton;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject settingsPanel;
    
    [Header("Settings")]
    [SerializeField] private bool showSettingsAtStart = true;
    [SerializeField] private KeyCode toggleSettingsKey = KeyCode.F1;
    
    private FeedbackModeManager feedbackManager;

    private void Start()
    {
        feedbackManager = FindObjectOfType<FeedbackModeManager>();
        
        if (feedbackManager == null)
        {
            Debug.LogError("[HTI Controller] FeedbackModeManager not found in scene!");
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
    }

    private void SetupUI()
    {
        // Setup dropdown
        if (feedbackModeDropdown != null)
        {
            feedbackModeDropdown.ClearOptions();
            feedbackModeDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "Verbal Filler",
                "Gesture",
                "Visual Cue",
                "None (Control)"
            });
            
            // Set to current mode
            feedbackModeDropdown.value = (int)feedbackManager.CurrentMode;
            feedbackModeDropdown.onValueChanged.AddListener(OnModeChanged);
        }

        // Setup apply button
        if (applyButton != null)
        {
            applyButton.onClick.AddListener(OnApplyButtonClicked);
        }

        UpdateStatusText();
    }

    private void OnModeChanged(int value)
    {
        // Preview mode change
        UpdateStatusText($"Mode selected: {(FeedbackModeManager.FeedbackMode)value}");
    }

    private void OnApplyButtonClicked()
    {
        if (feedbackModeDropdown != null && feedbackManager != null)
        {
            int selectedMode = feedbackModeDropdown.value;
            feedbackManager.SetFeedbackModeByIndex(selectedMode);
            
            UpdateStatusText($"✓ Applied: {(FeedbackModeManager.FeedbackMode)selectedMode}");
            
            // Log for research data
            Debug.Log($"[HTI Experiment] Mode Changed: {(FeedbackModeManager.FeedbackMode)selectedMode} at {System.DateTime.Now}");
        }
    }

    private void UpdateStatusText(string message = null)
    {
        if (statusText == null) return;

        if (message != null)
        {
            statusText.text = message;
        }
        else if (feedbackManager != null)
        {
            statusText.text = $"Current Mode: {feedbackManager.CurrentMode}";
        }
    }

    /// <summary>
    /// Quick mode switches for testing (can be bound to keyboard shortcuts)
    /// </summary>
    [ContextMenu("Quick: Verbal Filler")]
    public void SetVerbalFiller() => ApplyMode(0);

    [ContextMenu("Quick: Gesture")]
    public void SetGesture() => ApplyMode(1);

    [ContextMenu("Quick: Visual Cue")]
    public void SetVisualCue() => ApplyMode(2);

    [ContextMenu("Quick: None")]
    public void SetNone() => ApplyMode(3);

    private void ApplyMode(int mode)
    {
        if (feedbackManager != null)
        {
            feedbackManager.SetFeedbackModeByIndex(mode);
            if (feedbackModeDropdown != null)
                feedbackModeDropdown.value = mode;
            UpdateStatusText();
        }
    }

    // Keyboard shortcuts for quick switching during testing
    private void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SetVerbalFiller();
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            SetGesture();
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            SetVisualCue();
        else if (Input.GetKeyDown(KeyCode.Alpha4))
            SetNone();
    }
}

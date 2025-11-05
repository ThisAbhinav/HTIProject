using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays conversation progress for researchers/test runners
/// Shows time, exchanges, info discovered
/// </summary>
public class ConversationProgressUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject progressPanel;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text exchangesText;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Image timeProgressBar;
    [SerializeField] private Image infoProgressBar;
    
    [Header("Settings")]
    [SerializeField] private bool showDuringConversation = true;
    [SerializeField] private bool hideFromParticipant = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.F2;
    
    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.green;
    [SerializeField] private Color closingColor = Color.yellow;
    [SerializeField] private Color endingColor = Color.red;

    private ConversationManager conversationManager;
    private bool isPanelVisible = true;

    private void Start()
    {
        conversationManager = FindObjectOfType<ConversationManager>();
        
        if (conversationManager == null)
        {
            Debug.LogWarning("[Progress UI] ConversationManager not found!");
            return;
        }

        // Subscribe to events
        ConversationManager.OnConversationStart += OnConversationStart;
        ConversationManager.OnConversationEnd += OnConversationEnd;
        ConversationManager.OnTimeUpdate += OnTimeUpdate;
        ConversationManager.OnInfoDiscovered += OnInfoDiscovered;

        if (progressPanel != null)
        {
            progressPanel.SetActive(showDuringConversation && !hideFromParticipant);
        }
    }

    private void OnDestroy()
    {
        if (conversationManager != null)
        {
            ConversationManager.OnConversationStart -= OnConversationStart;
            ConversationManager.OnConversationEnd -= OnConversationEnd;
            ConversationManager.OnTimeUpdate -= OnTimeUpdate;
            ConversationManager.OnInfoDiscovered -= OnInfoDiscovered;
        }
    }

    private void Update()
    {
        // Toggle visibility with F2
        if (Input.GetKeyDown(toggleKey) && progressPanel != null)
        {
            isPanelVisible = !isPanelVisible;
            progressPanel.SetActive(isPanelVisible);
        }

        // Update display every frame
        if (conversationManager != null && conversationManager.IsConversationActive)
        {
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        var stats = conversationManager.GetStats();

        // Time display
        if (timeText != null)
        {
            int minutes = Mathf.FloorToInt(stats.durationMinutes);
            int seconds = Mathf.FloorToInt((stats.durationMinutes - minutes) * 60);
            timeText.text = $"Time: {minutes:00}:{seconds:00}";
        }

        // Exchanges display
        if (exchangesText != null)
        {
            exchangesText.text = $"Exchanges: {stats.exchangeCount}";
        }

        // Info discovered display
        if (infoText != null)
        {
            infoText.text = $"Info: {stats.infoDiscovered}/{stats.totalInfo}";
        }

        // Status display
        if (statusText != null)
        {
            if (stats.shouldClose)
            {
                statusText.text = "Status: Ready to Close";
                statusText.color = closingColor;
            }
            else if (stats.isInClosingWindow)
            {
                statusText.text = "Status: Closing Window";
                statusText.color = Color.yellow;
            }
            else
            {
                statusText.text = "Status: Active";
                statusText.color = normalColor;
            }
        }

        // Progress bars
        if (infoProgressBar != null)
        {
            float infoProgress = stats.totalInfo > 0 ? (float)stats.infoDiscovered / stats.totalInfo : 0f;
            infoProgressBar.fillAmount = infoProgress;
            infoProgressBar.color = Color.Lerp(Color.red, Color.green, infoProgress);
        }
    }

    private void OnConversationStart()
    {
        if (progressPanel != null && showDuringConversation)
        {
            progressPanel.SetActive(!hideFromParticipant);
        }
    }

    private void OnConversationEnd()
    {
        if (statusText != null)
        {
            statusText.text = "Status: Ended";
            statusText.color = endingColor;
        }
    }

    private void OnTimeUpdate(float timePercent)
    {
        if (timeProgressBar != null)
        {
            timeProgressBar.fillAmount = timePercent;
            
            // Change color based on progress
            if (timePercent < 0.7f)
                timeProgressBar.color = normalColor;
            else if (timePercent < 0.9f)
                timeProgressBar.color = closingColor;
            else
                timeProgressBar.color = endingColor;
        }
    }

    private void OnInfoDiscovered(string info)
    {
        Debug.Log($"[Progress UI] ✓ Discovered: {info}");
    }

    public void ToggleVisibility()
    {
        isPanelVisible = !isPanelVisible;
        if (progressPanel != null)
            progressPanel.SetActive(isPanelVisible);
    }

    public void ShowPanel()
    {
        isPanelVisible = true;
        if (progressPanel != null)
            progressPanel.SetActive(true);
    }

    public void HidePanel()
    {
        isPanelVisible = false;
        if (progressPanel != null)
            progressPanel.SetActive(false);
    }
}

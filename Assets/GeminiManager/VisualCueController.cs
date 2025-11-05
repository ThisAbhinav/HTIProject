using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Controls visual cues for thinking/processing feedback
/// Part of HTI research project - Visual Cue feedback modality
/// </summary>
public class VisualCueController : MonoBehaviour
{
    [Header("Visual Cue Settings")]
    [SerializeField] private bool visualCuesEnabled = false;
    
    [Header("UI References")]
    [SerializeField] private GameObject thinkingIndicatorPanel;
    [SerializeField] private TMP_Text thinkingText;
    [SerializeField] private Image thinkingIcon;
    
    [Header("Visual Cue Types")]
    [SerializeField] private VisualCueType cueType = VisualCueType.ThoughtBubble;
    
    [Header("Animation Settings")]
    [SerializeField] private float fadeInTime = 0.3f;
    [SerializeField] private float fadeOutTime = 0.3f;
    [SerializeField] private bool animateIcon = true;
    [SerializeField] private float iconRotationSpeed = 180f;
    
    [Header("Thinking Text Options")]
    [SerializeField] private string[] thinkingMessages = new string[]
    {
        "Thinking...",
        "Hmm...",
        "Let me think...",
        "Processing..."
    };

    private Coroutine animationCoroutine;
    private bool isShowing = false;

    public enum VisualCueType
    {
        ThoughtBubble,      // Thought bubble above head
        LoadingIcon,        // Spinning/pulsing icon
        TextIndicator,      // "Thinking..." text
        ProgressBar         // Progress bar (fake loading)
    }

    private void Start()
    {
        // Hide indicator at start
        if (thinkingIndicatorPanel != null)
            thinkingIndicatorPanel.SetActive(false);
    }

    public void SetVisualCuesEnabled(bool enabled)
    {
        visualCuesEnabled = enabled;
        Debug.Log($"[Visual Cue Controller] Visual Cues Enabled: {enabled}");
        
        if (!enabled && isShowing)
        {
            HideThinkingIndicator();
        }
    }

    /// <summary>
    /// Show thinking indicator
    /// </summary>
    public void ShowThinkingIndicator()
    {
        if (!visualCuesEnabled) return;

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(ShowIndicatorRoutine());
    }

    /// <summary>
    /// Hide thinking indicator
    /// </summary>
    public void HideThinkingIndicator()
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(HideIndicatorRoutine());
    }

    private IEnumerator ShowIndicatorRoutine()
    {
        if (thinkingIndicatorPanel == null) yield break;

        isShowing = true;
        thinkingIndicatorPanel.SetActive(true);

        // Set random thinking message
        if (thinkingText != null && thinkingMessages.Length > 0)
        {
            int randomIndex = Random.Range(0, thinkingMessages.Length);
            thinkingText.text = thinkingMessages[randomIndex];
        }

        // Fade in
        CanvasGroup canvasGroup = thinkingIndicatorPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = thinkingIndicatorPanel.AddComponent<CanvasGroup>();

        float elapsedTime = 0f;
        while (elapsedTime < fadeInTime)
        {
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // Animate icon if enabled
        if (animateIcon && thinkingIcon != null)
        {
            while (isShowing)
            {
                thinkingIcon.transform.Rotate(Vector3.forward, iconRotationSpeed * Time.deltaTime);
                yield return null;
            }
        }
    }

    private IEnumerator HideIndicatorRoutine()
    {
        if (thinkingIndicatorPanel == null) yield break;

        isShowing = false;

        // Fade out
        CanvasGroup canvasGroup = thinkingIndicatorPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = thinkingIndicatorPanel.AddComponent<CanvasGroup>();

        float elapsedTime = 0f;
        while (elapsedTime < fadeOutTime)
        {
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 0f;

        thinkingIndicatorPanel.SetActive(false);
    }

    public bool IsShowing => isShowing;

    // Editor testing
    [ContextMenu("Test: Show Indicator")]
    private void TestShow() => ShowThinkingIndicator();

    [ContextMenu("Test: Hide Indicator")]
    private void TestHide() => HideThinkingIndicator();
}

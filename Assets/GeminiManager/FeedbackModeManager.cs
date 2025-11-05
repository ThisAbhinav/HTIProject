using UnityEngine;
using System;
using GoogleTextToSpeech.Scripts;

/// <summary>
/// Manages feedback modality for HTI research project
/// Allows switching between Verbal Filler, Gesture, and Visual Cue modes
/// </summary>
public class FeedbackModeManager : MonoBehaviour
{
    public enum FeedbackMode
    {
        VerbalFiller,    // Uses "um", "hmm" etc. verbal sounds
        Gesture,         // Uses avatar gestures/animations (thinking pose, scratching head, etc.)
        VisualCue,       // Uses visual indicators (thought bubble, loading icon, etc.)
        None             // No feedback (control condition)
    }

    [Header("Experiment Configuration")]
    [SerializeField] private FeedbackMode currentMode = FeedbackMode.VerbalFiller;
    
    [Header("Component References")]
    [SerializeField] private TextToSpeechManager ttsManager;
    [SerializeField] private GestureController gestureController;
    [SerializeField] private VisualCueController visualCueController;

    [Header("Timing Settings")]
    [SerializeField] private float feedbackDelay = 0.1f;
    [SerializeField] private float minThinkingTime = 0.5f;
    [SerializeField] private float maxThinkingTime = 2.0f;

    public static FeedbackModeManager Instance { get; private set; }
    public static event Action<FeedbackMode> OnModeChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Debug.Log($"[HTI Experiment] Feedback Mode: {currentMode}");
        ApplyMode();
    }

    /// <summary>
    /// Change feedback mode during runtime (for testing)
    /// </summary>
    public void SetFeedbackMode(FeedbackMode mode)
    {
        currentMode = mode;
        Debug.Log($"[HTI Experiment] Feedback Mode Changed to: {currentMode}");
        ApplyMode();
        OnModeChanged?.Invoke(currentMode);
    }

    /// <summary>
    /// Set mode by index (for UI dropdown)
    /// </summary>
    public void SetFeedbackModeByIndex(int index)
    {
        if (index >= 0 && index < Enum.GetValues(typeof(FeedbackMode)).Length)
        {
            SetFeedbackMode((FeedbackMode)index);
        }
    }

    /// <summary>
    /// Apply current mode settings to all controllers
    /// </summary>
    private void ApplyMode()
    {
        // Disable all feedback types first
        if (ttsManager != null)
            ttsManager.SetFillerEnabled(false);
        
        if (gestureController != null)
            gestureController.SetGesturesEnabled(false);
        
        if (visualCueController != null)
            visualCueController.SetVisualCuesEnabled(false);

        // Enable only the selected mode
        switch (currentMode)
        {
            case FeedbackMode.VerbalFiller:
                if (ttsManager != null)
                    ttsManager.SetFillerEnabled(true);
                break;

            case FeedbackMode.Gesture:
                if (gestureController != null)
                    gestureController.SetGesturesEnabled(true);
                break;

            case FeedbackMode.VisualCue:
                if (visualCueController != null)
                    visualCueController.SetVisualCuesEnabled(true);
                break;

            case FeedbackMode.None:
                // All disabled (control condition)
                break;
        }
    }

    /// <summary>
    /// Trigger appropriate feedback based on current mode
    /// Called when LLM starts processing
    /// </summary>
    public void TriggerFeedback()
    {
        switch (currentMode)
        {
            case FeedbackMode.VerbalFiller:
                if (ttsManager != null)
                    ttsManager.PlayFiller();
                break;

            case FeedbackMode.Gesture:
                if (gestureController != null)
                    gestureController.PlayThinkingGesture();
                break;

            case FeedbackMode.VisualCue:
                if (visualCueController != null)
                    visualCueController.ShowThinkingIndicator();
                break;

            case FeedbackMode.None:
                // No feedback
                break;
        }
    }

    /// <summary>
    /// Stop feedback when LLM response is ready
    /// </summary>
    public void StopFeedback()
    {
        switch (currentMode)
        {
            case FeedbackMode.VerbalFiller:
                // Verbal filler stops automatically
                break;

            case FeedbackMode.Gesture:
                if (gestureController != null)
                    gestureController.StopThinkingGesture();
                break;

            case FeedbackMode.VisualCue:
                if (visualCueController != null)
                    visualCueController.HideThinkingIndicator();
                break;
        }
    }

    /// <summary>
    /// Get random thinking time for natural conversation
    /// </summary>
    public float GetRandomThinkingTime()
    {
        return UnityEngine.Random.Range(minThinkingTime, maxThinkingTime);
    }

    public FeedbackMode CurrentMode => currentMode;
    public float FeedbackDelay => feedbackDelay;

    // Editor testing methods
    [ContextMenu("Test: Set Verbal Filler Mode")]
    private void TestVerbalFiller() => SetFeedbackMode(FeedbackMode.VerbalFiller);

    [ContextMenu("Test: Set Gesture Mode")]
    private void TestGesture() => SetFeedbackMode(FeedbackMode.Gesture);

    [ContextMenu("Test: Set Visual Cue Mode")]
    private void TestVisualCue() => SetFeedbackMode(FeedbackMode.VisualCue);

    [ContextMenu("Test: Set None Mode")]
    private void TestNone() => SetFeedbackMode(FeedbackMode.None);

    [ContextMenu("Test: Trigger Feedback")]
    private void TestTriggerFeedback() => TriggerFeedback();
}

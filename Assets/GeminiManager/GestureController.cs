using UnityEngine;
using System.Collections;

/// <summary>
/// Controls avatar gestures for thinking/processing feedback
/// Part of HTI research project - Gesture feedback modality
/// </summary>
public class GestureController : MonoBehaviour
{
    [Header("Avatar References")]
    [SerializeField] private Animator avatarAnimator;
    
    [Header("Gesture Settings")]
    [SerializeField] private bool gesturesEnabled = false;
    [SerializeField] private float gestureDuration = 1.5f;
    
    [Header("Animation Triggers (Set these based on your avatar)")]
    [SerializeField] private string thinkingAnimationTrigger = "Thinking";
    [SerializeField] private string idleAnimationTrigger = "Idle";
    
    // Alternative: If you don't have animations, use head tilt/hand movement
    [Header("Procedural Gesture (if no animations)")]
    [SerializeField] private bool useProceduralGesture = true;
    [SerializeField] private Transform headTransform;
    [SerializeField] private Transform rightHandTransform;
    
    private bool isGesturing = false;
    private Coroutine gestureCoroutine;

    public void SetGesturesEnabled(bool enabled)
    {
        gesturesEnabled = enabled;
        Debug.Log($"[Gesture Controller] Gestures Enabled: {enabled}");
    }

    /// <summary>
    /// Play thinking gesture (scratching head, hand on chin, tilting head, etc.)
    /// </summary>
    public void PlayThinkingGesture()
    {
        if (!gesturesEnabled) return;

        if (gestureCoroutine != null)
            StopCoroutine(gestureCoroutine);

        gestureCoroutine = StartCoroutine(ThinkingGestureRoutine());
    }

    public void StopThinkingGesture()
    {
        if (gestureCoroutine != null)
        {
            StopCoroutine(gestureCoroutine);
            gestureCoroutine = null;
        }

        // Return to idle
        if (avatarAnimator != null && !string.IsNullOrEmpty(idleAnimationTrigger))
        {
            avatarAnimator.SetTrigger(idleAnimationTrigger);
        }

        isGesturing = false;
    }

    private IEnumerator ThinkingGestureRoutine()
    {
        isGesturing = true;

        // Try animation first
        if (avatarAnimator != null && !string.IsNullOrEmpty(thinkingAnimationTrigger))
        {
            Debug.Log("[Gesture Controller] Playing thinking animation");
            avatarAnimator.SetTrigger(thinkingAnimationTrigger);
        }
        // Fall back to procedural gesture
        else if (useProceduralGesture)
        {
            Debug.Log("[Gesture Controller] Playing procedural thinking gesture");
            yield return StartCoroutine(ProceduralThinkingGesture());
        }

        yield return new WaitForSeconds(gestureDuration);

        isGesturing = false;
    }

    /// <summary>
    /// Procedural gesture: Tilt head slightly and raise hand to chin
    /// </summary>
    private IEnumerator ProceduralThinkingGesture()
    {
        if (headTransform == null) yield break;

        Vector3 originalHeadRotation = headTransform.localEulerAngles;
        Vector3 tiltedRotation = originalHeadRotation + new Vector3(0, -15f, 10f); // Tilt head slightly

        float elapsedTime = 0f;
        float gestureDuration = 0.5f;

        // Tilt head
        while (elapsedTime < gestureDuration)
        {
            headTransform.localEulerAngles = Vector3.Lerp(originalHeadRotation, tiltedRotation, elapsedTime / gestureDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        headTransform.localEulerAngles = tiltedRotation;

        // Hold pose
        yield return new WaitForSeconds(0.5f);

        // Return to original
        elapsedTime = 0f;
        while (elapsedTime < gestureDuration)
        {
            headTransform.localEulerAngles = Vector3.Lerp(tiltedRotation, originalHeadRotation, elapsedTime / gestureDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        headTransform.localEulerAngles = originalHeadRotation;
    }

    public bool IsGesturing => isGesturing;
}

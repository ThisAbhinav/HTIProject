using UnityEngine;

/// <summary>
/// Controls avatar animation states during conversation
/// Manages transitions between Idle, Talking, and Thinking states
/// Transitions:
/// - Idle → Thinking (when IsThinking = true)
/// - Thinking → Talking (when IsTalking = true)
/// - Talking → Idle (when IsTalking = false)
/// </summary>
public class AvatarAnimationController : MonoBehaviour
{
    [Header("Animator Reference")]
    [SerializeField] private Animator avatarAnimator;
    
    [Header("Animation Parameters")]
    [Tooltip("Name of the bool parameter in the Animator for Talking state")]
    [SerializeField] private string talkingParameterName = "IsTalking";
    
    [Tooltip("Name of the bool parameter in the Animator for Thinking state")]
    [SerializeField] private string thinkingParameterName = "IsThinking";
    
    [Header("Multiple Thinking Animations")]
    [Tooltip("List of thinking animation clips to randomly choose from")]
    [SerializeField] private AnimationClip[] thinkingClips;
    
    // Hash IDs for performance
    private int talkingHash;
    private int thinkingHash;
    
    // Reference to the AnimatorOverrideController
    private AnimatorOverrideController overrideController;
    private AnimationClip originalThinkingClip;
    
    private void Awake()
    {
        if (avatarAnimator == null)
            avatarAnimator = GetComponent<Animator>();
            
        // Cache hash IDs for better performance
        talkingHash = Animator.StringToHash(talkingParameterName);
        thinkingHash = Animator.StringToHash(thinkingParameterName);
        
        // Create an override controller to swap clips at runtime
        if (avatarAnimator != null && avatarAnimator.runtimeAnimatorController != null)
        {
            overrideController = new AnimatorOverrideController(avatarAnimator.runtimeAnimatorController);
            avatarAnimator.runtimeAnimatorController = overrideController;
            
            // Store the original thinking clip (first one in the array or from the controller)
            if (thinkingClips != null && thinkingClips.Length > 0)
            {
                originalThinkingClip = thinkingClips[0];
            }
        }
    }
    
    /// <summary>
    /// Set the avatar to Idle state (default state)
    /// Ensures both Talking and Thinking are false
    /// </summary>
    public void SetIdle()
    {
        if (avatarAnimator == null) return;
        
        avatarAnimator.SetBool(talkingHash, false);
        avatarAnimator.SetBool(thinkingHash, false);
        Debug.Log("[Avatar Animation] State: Idle");
    }
    
    /// <summary>
    /// Set the avatar to Talking state (when speaking)
    /// Ensures Thinking is false to prevent state loops
    /// </summary>
    public void SetTalking()
    {
        if (avatarAnimator == null) return;
        
        avatarAnimator.SetBool(thinkingHash, false); // Ensure Thinking is off first
        avatarAnimator.SetBool(talkingHash, true);
        Debug.Log("[Avatar Animation] State: Talking");
    }
    
    /// <summary>
    /// Set the avatar to Thinking state (waiting for LLM response)
    /// Ensures Talking is false to prevent state loops
    /// Randomly selects from available thinking animation clips
    /// </summary>
    public void SetThinking()
    {
        if (avatarAnimator == null) return;
        
        // Randomly swap the thinking animation clip
        if (overrideController != null && thinkingClips != null && thinkingClips.Length > 0)
        {
            int randomIndex = Random.Range(0, thinkingClips.Length);
            AnimationClip selectedClip = thinkingClips[randomIndex];
            
            // Override the original thinking clip with the randomly selected one
            overrideController[originalThinkingClip] = selectedClip;
            
            Debug.Log($"[Avatar Animation] State: Thinking (Clip: {selectedClip.name})");
        }
        else
        {
            Debug.Log("[Avatar Animation] State: Thinking");
        }
        
        avatarAnimator.SetBool(talkingHash, false);
        avatarAnimator.SetBool(thinkingHash, true);
    }
}

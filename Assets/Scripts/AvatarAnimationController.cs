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
    
    // Hash IDs for performance
    private int talkingHash;
    private int thinkingHash;
    
    private void Awake()
    {
        // Cache hash IDs for better performance
        talkingHash = Animator.StringToHash(talkingParameterName);
        thinkingHash = Animator.StringToHash(thinkingParameterName);
        avatarAnimator = GetComponent<Animator>();
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
    /// </summary>
    public void SetThinking()
    {
        if (avatarAnimator == null) return;
        
        avatarAnimator.SetBool(talkingHash, false); // Ensure Talking is off first
        avatarAnimator.SetBool(thinkingHash, true);
        Debug.Log("[Avatar Animation] State: Thinking");
    }
    

    

}

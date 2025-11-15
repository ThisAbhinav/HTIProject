using UnityEngine;

/// <summary>
/// Controls avatar animation states during conversation
/// Manages transitions between Idle, Talking, and Thinking states
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
        
        // Auto-find animator if not assigned
        if (avatarAnimator == null)
        {
            avatarAnimator = GetComponent<Animator>();
            if (avatarAnimator == null)
            {
                Debug.LogError("[AvatarAnimationController] No Animator found! Please assign an Animator component.");
            }
        }
    }
    
    /// <summary>
    /// Set the avatar to Idle state (default state)
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
    /// </summary>
    public void SetTalking()
    {
        if (avatarAnimator == null) return;
        
        avatarAnimator.SetBool(talkingHash, true);
        avatarAnimator.SetBool(thinkingHash, false);
        Debug.Log("[Avatar Animation] State: Talking");
    }
    
    /// <summary>
    /// Set the avatar to Thinking state (waiting for LLM response)
    /// </summary>
    public void SetThinking()
    {
        if (avatarAnimator == null) return;
        
        avatarAnimator.SetBool(talkingHash, false);
        avatarAnimator.SetBool(thinkingHash, true);
        Debug.Log("[Avatar Animation] State: Thinking");
    }
    
    /// <summary>
    /// Check if the animator has the required parameters
    /// </summary>
    private void OnValidate()
    {
        if (avatarAnimator != null)
        {
            bool hasTalking = HasParameter(avatarAnimator, talkingParameterName, AnimatorControllerParameterType.Bool);
            bool hasThinking = HasParameter(avatarAnimator, thinkingParameterName, AnimatorControllerParameterType.Bool);
            
            if (!hasTalking)
            {
                Debug.LogWarning($"[AvatarAnimationController] Animator is missing bool parameter: {talkingParameterName}");
            }
            
            if (!hasThinking)
            {
                Debug.LogWarning($"[AvatarAnimationController] Animator is missing bool parameter: {thinkingParameterName}");
            }
        }
    }
    
    /// <summary>
    /// Helper method to check if animator has a specific parameter
    /// </summary>
    private bool HasParameter(Animator animator, string paramName, AnimatorControllerParameterType type)
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName && param.type == type)
                return true;
        }
        return false;
    }
}

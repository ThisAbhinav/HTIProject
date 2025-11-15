using GoogleTextToSpeech.Scripts;
using GoogleTextToSpeech.Scripts.Data;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
[System.Serializable]
public class GeminiResponse
{
    public List<Candidate> candidates;
}

[System.Serializable]
public class Response
{
    public Candidate[] candidates;
}

[System.Serializable]
public class Candidate
{
    public Content content;
}

[System.Serializable]
public class Content
{
    public string role;
    public List<Part> parts;
}

[System.Serializable]
public class Part
{
    public string text;
}

public class ChatRequest
{
    public List<Content> contents;
}


public class UnityAndGeminiV3 : MonoBehaviour
{
    [Header("Gemini API Password")]
    public string apiKey;
    private string apiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent";

    [Header("NPC Function")]
    [SerializeField] private TextToSpeechManager googleServices;
    [SerializeField] private ChatManager chatManager;
    [SerializeField] private TaskManager TaskManager;
    [SerializeField] private ConversationManager conversationManager;

    private List<Content> chatHistory = new List<Content>();
    private bool isProcessing = false;
    private bool systemPromptInitialized = false;

    [Header("Character Background (HTI Experiment)")]
    [SerializeField]
    private string systemPrompt = @"You are Alex, a 20-year-old college junior studying Computer Science at University of California, Berkeley.

CORE IDENTITY:
- Full Name: Alex Thompson
- Age: 20 years old
- Year: Junior (3rd year)
- Major: Computer Science with a focus on AI/ML
- University: UC Berkeley, California
- Hometown: San Francisco, California

PERSONALITY:
- Friendly, curious, and genuinely interested in cultural exchange
- Naturally conversational - keeps dialogue flowing
- Uses casual college student language but remains articulate
- Shares experiences without being asked (like real conversations)

BACKGROUND INFO (Share naturally through conversation):
- Lives in on-campus housing (dorm) with roommate Jake (mechanical engineering major)
- Works part-time as a teaching assistant for intro CS courses
- Member of Computer Science club and Robotics team
- Enjoys hiking on weekends in nearby trails
- Favorite spot: campus library overlooking the bay
- Usually eats at dining hall, sometimes food trucks on Telegraph Avenue
- Takes BART to explore San Francisco on weekends

CRITICAL CONVERSATION RULES:
1. ALWAYS ASK A QUESTION: Every response MUST end with a question or prompt to keep conversation flowing
2. BE SUBSTANTIVE: Give 2-3 sentences minimum. Share your thoughts, experiences, or reactions before asking
3. SHOW CURIOSITY: Express genuine interest in their Indian college experience and culture
4. MAKE COMPARISONS: Relate their experiences to your Berkeley life naturally
5. SHARE PROACTIVELY: Don't wait to be asked - volunteer interesting details about your life

RESPONSE STRUCTURE (Follow this):
- Acknowledge/respond to their message (1-2 sentences)
- Add relevant detail from your life or ask for elaboration (1-2 sentences)
- End with an engaging question or prompt (1 sentence)

FORBIDDEN:
- DO NOT give one-word or single-sentence responses
- DO NOT just answer and stop
- DO NOT be passive - actively drive the conversation
- DO NOT repeat the same questions

EXAMPLES OF GOOD RESPONSES:
USER: 'I study at Punjab University.'
BAD: 'Cool! What do you study?'
GOOD: 'Oh Punjab University! I've heard it's a great school. Berkeley has a huge campus, like 178 acres - we even have our own hiking trails nearby. Is your campus more compact or spread out? What's your favorite spot there?'

USER: 'I'm studying engineering.'
BAD: 'Nice, me too.'
GOOD: 'No way, I'm in CS which is basically engineering too! I'm working as a TA for intro programming right now - it's interesting seeing how students struggle with the same concepts I did. Do you have any part-time work or internships, or is that not as common in India?'

Remember: Real conversations flow naturally. You're not an interview bot - you're a curious college student excited to learn about another culture while sharing your own experiences.";

    [Header("Conversation Settings")]
    [SerializeField] private bool addNaturalThinkingDelay = true;
    [SerializeField] private float minThinkingDelay = 0.5f;
    [SerializeField] private float maxThinkingDelay = 1.5f;

    void Start()
    {
        if (chatManager != null)
        {
            ChatManager.OnUserMessage += HandleUserMessage;
        }
        InitializeSystemPrompt();
        
        // Start conversation timing if manager exists
        if (conversationManager != null)
        {
            conversationManager.StartConversation();
        }
    }

    private void OnDestroy()
    {
        ChatManager.OnUserMessage -= HandleUserMessage;
    }

    private void InitializeSystemPrompt()
    {
        if (systemPromptInitialized) return;

        Content systemContent = new Content
        {
            role = "user",
            parts = new List<Part> { new Part { text = systemPrompt } }
        };
        chatHistory.Add(systemContent);

        systemPromptInitialized = true;
    }


    private void HandleUserMessage(string message)
    {
        SendChat(message);
    }

    public void SendChat(string userMessage)
    {
        if (isProcessing)
        {
            Debug.Log("Please wait for the current response to complete.");
            return;
        }

        if (string.IsNullOrWhiteSpace(userMessage))
        {
            chatManager?.AddSystemMessage("Message cannot be empty.");
            return;
        }

        StartCoroutine(SendChatRequestToGemini(userMessage));
    }

    private IEnumerator SendChatRequestToGemini(string newMessage)
    {
        isProcessing = true;

        // Trigger feedback via ConversationManager (HTI Experiment)
        if (conversationManager != null)
        {
            conversationManager.TriggerFeedback(newMessage);
        }

        // Add natural thinking delay (simulates human processing time)
        if (addNaturalThinkingDelay)
        {
            float thinkingDelay = UnityEngine.Random.Range(minThinkingDelay, maxThinkingDelay);
            yield return new WaitForSeconds(thinkingDelay);
        }

        string url = $"{apiEndpoint}?key={apiKey}";
        
        // Build user message with conversation state context
        string contextualMessage = newMessage;
        if (conversationManager != null)
        {
            string statePrompt = conversationManager.GetConversationStatePrompt();
            if (!string.IsNullOrEmpty(statePrompt))
            {
                contextualMessage = newMessage + statePrompt;
            }
        }
        
        // Build user message
        Content userContent = new Content
        {
            role = "user",
            parts = new List<Part> { new Part { text = contextualMessage } }
        };
        chatHistory.Add(userContent);

        ChatRequest chatRequest = new ChatRequest { contents = chatHistory };
        string jsonData = JsonConvert.SerializeObject(chatRequest);
        byte[] jsonToSend = System.Text.Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Gemini API Error: " + www.error);
                chatManager?.AddSystemMessage($"Error: {www.error}. Please try again.");
            }
            else
            {
                try
                {
                    var response = JsonConvert.DeserializeObject<GeminiResponse>(www.downloadHandler.text);

                    string rawReply = null;
                    if (response?.candidates != null &&
                        response.candidates.Count > 0 &&
                        response.candidates[0]?.content?.parts != null &&
                        response.candidates[0].content.parts.Count > 0)
                    {
                        rawReply = response.candidates[0].content.parts[0]?.text;
                    }

                    if (!string.IsNullOrWhiteSpace(rawReply))
                    {
                        // Enforce conversational length: 3-4 sentences OR ~60-80 words
                        string conciseReply = EnforceBrevity(rawReply);

                        // Clean for TTS after brevity trimming
                        string speechText = CleanTextForSpeech(conciseReply);

                        Content botContent = new Content
                        {
                            role = "model",
                            parts = new List<Part> { new Part { text = conciseReply } }
                        };
                        chatHistory.Add(botContent);

                        Debug.Log("AI Response (Display): " + conciseReply);
                        Debug.Log("AI Response (Speech): " + speechText);
                        
                        // TaskManager checks for info discovery
                        TaskManager?.CutTasks(speechText);

                        // Stop feedback via ConversationManager
                        if (conversationManager != null)
                        {
                            conversationManager.StopFeedback();
                        }
                        
                        // Stream text while speaking
                        StartCoroutine(StreamTextWhileSpeaking(conciseReply, speechText));
                    }
                    else
                    {
                        Debug.Log("No text found.");
                        chatManager?.AddSystemMessage("AI response was empty. Please try again.");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("JSON Parse Error: " + ex.Message);
                    chatManager?.AddSystemMessage("Error parsing AI response.");
                }
            }
        }

        isProcessing = false;
    }

    /// <summary>
    /// Enforce conversational length: 3-4 sentences OR ~60-80 words
    /// </summary>
    private string EnforceBrevity(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        // Light cleanup
        string t = text;
        t = Regex.Replace(t, @"\*\*(.+?)\*\*|\*(.+?)\*|__(.+?)__|_(.+?)_", "$1$2$3$4");
        t = Regex.Replace(t, @"`{3}[\s\S]*?`{3}", "");
        t = Regex.Replace(t, @"`(.+?)`", "$1");
        t = Regex.Replace(t, @"\[([^\]]+)\]\([^\)]+\)", "$1");
        t = Regex.Replace(t, @"^\s*[-*•]\s*", "", RegexOptions.Multiline);
        t = Regex.Replace(t, @"\s+", " ").Trim();

        // Split into sentences
        var sentences = Regex.Split(t, @"(?<=[\.!\?])\s+").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

        // Keep 3-4 sentences
        if (sentences.Count > 4)
            sentences = sentences.Take(4).ToList();

        string joined = string.Join(" ", sentences);

        // Word cap at ~80 words
        var words = joined.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 80)
            joined = string.Join(" ", words.Take(80)) + "...";

        joined = Regex.Replace(joined, @"\s+", " ").Trim();

        return string.IsNullOrEmpty(joined) ? t : joined;
    }

    /// <summary>
    /// Cleans text for Text-to-Speech by removing markdown and handling punctuation
    /// </summary>
    private string CleanTextForSpeech(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Remove markdown bold/italic
        text = Regex.Replace(text, @"\*\*(.+?)\*\*", "$1"); // **bold**
        text = Regex.Replace(text, @"\*(.+?)\*", "$1"); // *italic*
        text = Regex.Replace(text, @"__(.+?)__", "$1"); // __bold__
        text = Regex.Replace(text, @"_(.+?)_", "$1"); // _italic_

        // Remove markdown headers
        text = Regex.Replace(text, @"^#+\s*", "", RegexOptions.Multiline);

        // Remove markdown links but keep text [text](url) -> text
        text = Regex.Replace(text, @"\[([^\]]+)\]\([^\)]+\)", "$1");

        // Remove markdown code blocks
        text = Regex.Replace(text, @"`{3}[\s\S]*?`{3}", ""); // ```code```
        text = Regex.Replace(text, @"`(.+?)`", "$1"); // `code`

        // Replace punctuation patterns
        text = text.Replace("...", " pause ");
        text = text.Replace("–", " ");
        text = text.Replace("—", " ");

        // Normalize ! and ?
        text = Regex.Replace(text, @"!+", "!");
        text = Regex.Replace(text, @"\?+", "?");

        // Remove bullet points
        text = Regex.Replace(text, @"^\s*[-*•]\s*", "", RegexOptions.Multiline);

        // Clean up excessive whitespace
        text = Regex.Replace(text, @"\s+", " ");
        text = text.Trim();

        return text;
    }

    private IEnumerator StreamTextWhileSpeaking(string fullText, string speechText)
    {
        if (string.IsNullOrWhiteSpace(fullText)) yield break;

        // Break the full text into words for streaming
        string[] words = fullText.Split(' ');
        string streamedText = "";

        // Start TTS playback slightly earlier
        googleServices?.SendTextToGoogle(speechText);
        yield return new WaitForSeconds(0.1f); // Small delay before text streaming

        foreach (string word in words)
        {
            streamedText += word + " ";

            // Update the chat display progressively
            chatManager?.UpdateAIMessage(streamedText.TrimEnd());

            // Reduce the delay between words for faster streaming
            yield return new WaitForSeconds(0.15f);
        }

        // Ensure the final text is displayed completely
        chatManager?.UpdateAIMessage(fullText);
    }
}

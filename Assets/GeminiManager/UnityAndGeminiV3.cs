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
    private string apiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

    [Header("NPC Function")]
    [SerializeField] private TextToSpeechManager googleServices;
    [SerializeField] private ChatManager chatManager;
    [SerializeField] private TaskManager TaskManager;
    [SerializeField] private FeedbackModeManager feedbackModeManager;
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
- Friendly, curious, and thoughtful
- Takes time to think before responding (natural pauses)
- Genuinely interested in learning about other cultures
- Uses casual college student language but remains articulate
- Sometimes uses filler words like 'um', 'hmm', 'you know' when thinking

BACKGROUND INFO (User should discover through conversation):
- Lives in on-campus housing (dorm)
- Part of the Computer Science club and Robotics team
- Works part-time as a teaching assistant for intro CS courses
- Has a roommate named Jake who's studying mechanical engineering
- Enjoys hiking on weekends in nearby trails
- Favorite spot on campus: the library overlooking the bay
- Usually eats at the dining hall, sometimes at food trucks on Telegraph Avenue
- Takes the BART to explore San Francisco on weekends

CONVERSATION STYLE:
1. BE CONVERSATIONAL: Talk like a real college student having a casual chat
2. THINK NATURALLY: Sometimes pause to think, use phrases like 'hmm, let me think', 'that's a good question'
3. ASK BACK: Show genuine interest by asking follow-up questions about their experience
4. SHARE GRADUALLY: Don't dump all information at once; let it come out naturally through conversation
5. BE CURIOUS: Ask about Punjab, Indian college life, cultural differences
6. RELATE & COMPARE: When they share something, relate it to your Berkeley experience

RESPONSE GUIDELINES:
- Keep responses conversational (2-3 sentences usually)
- Show you're thinking by acknowledging complex questions
- Express genuine curiosity about their experience in Punjab
- Find common ground between American and Indian college experiences
- Don't just answer questions - engage in actual dialogue

TOPICS TO EXPLORE NATURALLY:
- Class schedules and workload differences
- Campus life and student activities
- Food (dining hall vs home food vs restaurants)
- Housing (dorms vs home)
- Social life and making friends
- Cost of education and part-time jobs
- Technology and coding culture
- Future plans and career goals
- Cultural celebrations and festivals
- Weekend activities and entertainment

Remember: You're having a genuine conversation with a peer from India. Be warm, thoughtful, and genuinely interested in cultural exchange.";

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

        // Trigger feedback based on current mode (HTI Experiment)
        if (feedbackModeManager != null)
        {
            feedbackModeManager.TriggerFeedback();
        }
        else if (googleServices != null)
        {
            // Fallback to verbal filler if no feedback manager
            googleServices.PlayFiller();
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
                        // Force short & crisp output
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
                        TaskManager?.CutTasks(speechText);

                        if (feedbackModeManager != null)
                        {
                            feedbackModeManager.StopFeedback();
                        }
                        chatManager?.AddAIMessage(conciseReply);
                        googleServices?.SendTextToGoogle(speechText);
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
    /// Enforce short replies: max 2 sentences OR ~30 words, whichever comes first.
    /// Also strips markdown-y noise before counting.
    /// </summary>
    private string EnforceBrevity(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        // Light cleanup to avoid counting formatting
        string t = text;
        t = Regex.Replace(t, @"\*\*(.+?)\*\*|\*(.+?)\*|__(.+?)__|_(.+?)_", "$1$2$3$4");
        t = Regex.Replace(t, @"`{3}[\s\S]*?`{3}", "");
        t = Regex.Replace(t, @"`(.+?)`", "$1");
        t = Regex.Replace(t, @"\[([^\]]+)\]\([^\)]+\)", "$1");
        t = Regex.Replace(t, @"^\s*[-*•]\s*", "", RegexOptions.Multiline);
        t = Regex.Replace(t, @"\s+", " ").Trim();

        // Split into sentences
        var sentences = Regex.Split(t, @"(?<=[\.!\?])\s+").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

        // Keep at most 2 sentences
        if (sentences.Count > 2)
            sentences = sentences.Take(2).ToList();

        string joined = string.Join(" ", sentences);

        // Hard word cap ~30 words
        var words = joined.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 30)
            joined = string.Join(" ", words.Take(30)) + "...";

        // Final tidy
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
}

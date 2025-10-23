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

public enum LearningTopic
{
    CoffeeShop,
    Cooking
}

public class UnityAndGeminiV3 : MonoBehaviour
{
    [Header("Gemini API Password")]
    public string apiKey;
    private string apiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

    [Header("NPC Function")]
    [SerializeField] private TextToSpeechManager googleServices;
    [SerializeField] private ChatManager chatManager;

    [Header("Learning Settings")]
    [SerializeField] private LearningTopic currentTopic = LearningTopic.CoffeeShop;

    private List<Content> chatHistory = new List<Content>();
    private bool isProcessing = false;
    private bool systemPromptInitialized = false;

    // System prompts for different topics (explicitly enforce short replies)
    private Dictionary<LearningTopic, string> systemPrompts = new Dictionary<LearningTopic, string>()
    {
        {
            LearningTopic.CoffeeShop,
            @"You are Jean, a friendly French tutor helping students order in a coffee shop.

Your teaching approach:
- Introduce coffee-ordering terms naturally: 'le café', 'le thé', 'le menu', 'commander', 'à emporter', 'sur place', 'décaféiné', 'avec du lait', 'sans sucre', 'l’addition'.
- Provide French + English on first mention of a term.
- Use simple beginner sentences and encourage quick practice.

RESPONSE STYLE (IMPORTANT):
- Always reply in 1–2 SHORT sentences, max ~30 words total.
- Keep it conversational, no markdown, no lists, no asterisks.
- Prefer giving 1 example phrase the learner can repeat.

Key phrases:
- 'Je voudrais un café, s’il vous plaît.' (I’d like a coffee, please.)
- 'À emporter ou sur place ?' (To go or for here?)
- 'L’addition, s’il vous plaît.' (The bill, please.)"
        },
        {
            LearningTopic.Cooking,
            @"You are Jean, a friendly French tutor helping students learn cooking vocabulary.

Your teaching approach:
- Introduce cooking terms naturally: 'la cuisine', 'cuisiner', 'la recette', 'les ingrédients', 'le four', 'la casserole', 'couper', 'mélanger', 'cuire'.
- Provide French + English on first mention of a term.

RESPONSE STYLE (IMPORTANT):
- Always reply in 1–2 SHORT sentences, max ~30 words total.
- Conversational, no markdown, no lists, no asterisks.
- Prefer giving 1 example phrase to repeat.

Key phrases:
- 'Je fais la cuisine.' (I’m cooking.)
- 'Ajoute du sel.' (Add salt.)"
        }
    };

    void Start()
    {
        if (chatManager != null)
        {
            ChatManager.OnUserMessage += HandleUserMessage;
        }
        InitializeSystemPrompt();
    }

    private void OnDestroy()
    {
        ChatManager.OnUserMessage -= HandleUserMessage;
    }

    private void InitializeSystemPrompt()
    {
        if (systemPromptInitialized) return;

        string systemPrompt = systemPrompts[currentTopic];

        // Send the system prompt as the first "user" message to prime Gemini
        Content systemContent = new Content
        {
            role = "user",
            parts = new List<Part> { new Part { text = systemPrompt } }
        };
        chatHistory.Add(systemContent);

        // Optional ack from model to stabilize context
        Content modelAck = new Content
        {
            role = "model",
            parts = new List<Part> { new Part { text = "Understood. I will teach with short, simple replies for quick practice." } }
        };
        chatHistory.Add(modelAck);

        systemPromptInitialized = true;
        Debug.Log($"System prompt initialized for topic: {currentTopic}");
    }

    // Public method to change learning topic
    public void ChangeTopic(LearningTopic newTopic)
    {
        currentTopic = newTopic;
        chatHistory.Clear();
        systemPromptInitialized = false;
        InitializeSystemPrompt();

        string topicName = newTopic == LearningTopic.CoffeeShop ? "coffee shop ordering" : "cooking";
        chatManager?.AddSystemMessage($"Topic changed to {topicName}. On y va !");
    }

    private void HandleUserMessage(string message)
    {
        SendChat(message);
    }

    public void SendChat(string userMessage)
    {
        if (isProcessing)
        {
            chatManager?.AddSystemMessage("Please wait for the current response to complete.");
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
        chatManager?.AddSystemMessage("AI is thinking...");

        string url = $"{apiEndpoint}?key={apiKey}";

        // Build user message
        Content userContent = new Content
        {
            role = "user",
            parts = new List<Part> { new Part { text = newMessage } }
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

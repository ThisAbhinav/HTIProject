using GoogleTextToSpeech.Scripts;
using GoogleTextToSpeech.Scripts.Data;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class AIStructuredResponse
{
    public string message; // The text to speak
    public bool end_conversation; // Whether to trigger the ending
}

[System.Serializable]
public class GeminiResponse
{
    public List<Candidate> candidates;
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
    [Header("Gemini API")]
    public string apiKey;
    private string apiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

    [Header("References")]
    [SerializeField] private TextToSpeechManager googleServices;
    [SerializeField] private ChatManager chatManager;
    [SerializeField] private TaskManager taskManager;
    [SerializeField] private ConversationManager conversationManager;

    private List<Content> chatHistory = new List<Content>();
    private bool isProcessing = false;
    private bool systemPromptInitialized = false;

    [Header("Timing")]
    [SerializeField] private bool addNaturalThinkingDelay = true;
    [SerializeField] private float minThinkingDelay = 0.5f;
    [SerializeField] private float maxThinkingDelay = 1.5f;

    private string baseSystemPrompt = @"You are Alex, a senior student mentor at UC Berkeley. The user is a childhood friend who is currently studying in Plaksha University in Mohali, India and you have visited him/her for a summer break and have come to India.

YOUR BACKSTORY (FACTS YOU MUST STICK TO):
1. Major: Computer Science (CS).
2. Dorm: You live in Blackwell Hall (the newest one).
3. Hobby: Photography (you love taking photos around campus).
4. Best Coffee: Caffe Strada (it's a classic).

ROLE & GOAL:
- Your goal is to make the user feel welcome while naturally revealing these facts about yourself when asked.
- You are friendly, casual, but concise.
- You MUST output your response in JSON format ONLY.

CONVERSATION RULES (STRICT):
1. SHORT RESPONSES: Maximum 2-4 sentences. Never monologue.
2. ONE QUESTION ONLY: Ask exactly one follow-up question per turn. Never ask two.
3. STAY ON TRACK: If the user ignores your question, Always acknowledge their statement first, then gently circle back.
4. DO NOT ANSWER EVERYTHING: If the user asks 3 questions, pick the most interesting one to answer.

JSON OUTPUT FORMAT:
You must strictly output a JSON object with no markdown formatting:
{
  ""message"": ""Your spoken response to the user here."",
  ""end_conversation"": false
}

Set ""end_conversation"" to true ONLY if the user says ""Goodbye"" or ""I have to go"".
";

    void Start()
    {
        ChatManager.OnUserMessage += HandleUserMessage;
        InitializeSystemPrompt();
    }

    private void OnDestroy()
    {
        ChatManager.OnUserMessage -= HandleUserMessage;
    }

    private void InitializeSystemPrompt()
    {
        Content systemContent = new Content
        {
            role = "user",
            parts = new List<Part> { new Part { text = baseSystemPrompt } }
        };
        chatHistory.Add(systemContent);
        systemPromptInitialized = true;
    }

    public void StartIntro()
    {
        conversationManager.StartConversation();
        string triggerMsg = "The user has just approached you. Say 'Hi bro, long time no see', and introduce yourself briefly.";
        StartCoroutine(SendChatRequestToGemini(triggerMsg, true));
    }
    private void HandleUserMessage(string message)
    {
        SendChat(message);
    }

    public void SendChat(string userMessage)
    {
        if (isProcessing) return;
        if (string.IsNullOrWhiteSpace(userMessage)) return;

        StartCoroutine(SendChatRequestToGemini(userMessage,false));
    }

    private IEnumerator SendChatRequestToGemini(string newMessage, bool isHiddenSystemTrigger)
    {
        isProcessing = true;

        if (!isHiddenSystemTrigger && conversationManager != null)
            conversationManager.TriggerFeedback(newMessage);

        if (addNaturalThinkingDelay)
            yield return new WaitForSeconds(UnityEngine.Random.Range(minThinkingDelay, maxThinkingDelay));

        string url = $"{apiEndpoint}?key={apiKey}";

        // Inject Task Context
        string currentTasks = taskManager != null ? taskManager.GetTaskStatusForPrompt() : "";
        string fullMessage = newMessage + "\n\n[SYSTEM NOTE: Output JSON only. " + currentTasks + "]";

        Content userContent = new Content
        {
            role = "user",
            parts = new List<Part> { new Part { text = fullMessage } }
        };
        chatHistory.Add(userContent);

        ChatRequest chatRequest = new ChatRequest { contents = chatHistory };
        string jsonData = JsonConvert.SerializeObject(chatRequest);

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Gemini Error: " + www.error);
                chatManager?.AddSystemMessage("Connection error...");
            }
            else
            {
                HandleGeminiResponse(www.downloadHandler.text);
            }
        }

        isProcessing = false;
    }

    private void HandleGeminiResponse(string jsonResponse)
    {
        try
        {
            var response = JsonConvert.DeserializeObject<GeminiResponse>(jsonResponse);
            string rawContent = response?.candidates?[0]?.content?.parts?[0]?.text;

            if (string.IsNullOrEmpty(rawContent)) return;

            rawContent = rawContent.Replace("```json", "").Replace("```", "").Trim();

            AIStructuredResponse aiData = JsonConvert.DeserializeObject<AIStructuredResponse>(rawContent);

            if (aiData != null)
            {
                conversationManager.LogFullAIResponse(aiData.message);
                conversationManager.StopFeedback();

                chatHistory.Add(new Content { role = "model", parts = new List<Part> { new Part { text = rawContent } } });

                string speechText = CleanTextForSpeech(aiData.message);
                taskManager.CutTasks(speechText);

                bool allTasksDone = taskManager.AllTasksCompleted();

                if (aiData.end_conversation || allTasksDone)
                {
                    conversationManager.QueueConversationEnd();
                }

                StartCoroutine(StreamTextWhileSpeaking(aiData.message, speechText));
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Parsing Error: " + ex.Message);
            chatManager?.AddSystemMessage("I didn't quite catch that.");
        }
    }

    private string CleanTextForSpeech(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return Regex.Replace(text, @"[*_`]", "").Trim();
    }

    private IEnumerator StreamTextWhileSpeaking(string fullText, string speechText)
    {
        googleServices?.SendTextToGoogle(speechText);
        yield return new WaitForSeconds(0.2f);

        string[] words = fullText.Split(' ');
        string currentDisplay = "";
        chatManager?.AddAIMessage("");

        foreach (string word in words)
        {
            currentDisplay += word + " ";
            chatManager?.UpdateAIMessage(currentDisplay.TrimEnd());
            yield return new WaitForSeconds(0.1f);
        }
        chatManager?.UpdateAIMessage(fullText);
    }
}
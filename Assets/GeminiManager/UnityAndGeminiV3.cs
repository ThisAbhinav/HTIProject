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

    [Header("Timing")]
    [SerializeField] private bool addNaturalThinkingDelay = true;
    [SerializeField] private float minThinkingDelay = 0.5f;
    [SerializeField] private float maxThinkingDelay = 1.5f;

    private string baseSystemPrompt = @"You are Alex, a senior student at UC Berkeley. You are visiting the user in Mohali, who is your childhood friend currently studying at Plaksha University, Mohali, India.

YOUR KNOWLEDGE BASE (FACTS YOU MUST STICK TO):
1.  Major: Computer Science (CS).
2.  Dorm: Blackwell Hall (newest/best dorm).
3.  Hometown: San Francisco (SF).
4.  Year: Senior (4th Year).
5.  Hobby: Photography (taking photos of architecture).
6.  Best Coffee: Caffe Strada (classic).
7.  Fav Food: Pizza (specifically from Sliver or Cheeseboard).
8.  Club: Robotics Club (IEEE).
9.  Pet: A Golden Retriever named Cooper back home.
10. Fav Library: Moffitt (open 24/7).
11. Transport: Electric Skateboard (Boosted board).
12. Fav Movie: Interstellar.
13. Music Taste: Lo-fi and Jazz for studying.
14. Siblings: One younger sister named Maya.
15. Sport: Basketball (Die-hard Warriors fan).
16. Summer Plans: Software Engineering Internship at Google.
17. Fav Non-Coffee Drink: Boba (specifically from Plentea).
18. Fav Professor: John DeNero (CS 61A legend).
19. Current Game: Elden Ring.
20. Campus Landmark: The Campanile (Clock Tower).

ROLE & GOAL:
- The user has a list of specific facts they are trying to learn about you (a game).
- Answer their questions naturally. Do not dump all info at once.
- Be friendly, casual, and concise.

CONVERSATION RULES (STRICT):
1. SHORT RESPONSES: Maximum 2-4 sentences.
2. ONE QUESTION ONLY: Ask exactly one follow-up question per turn. 90% OF TIMES ASK A FOLLOW UP QUESTION PLEASE. 
3. JSON ONLY: Output valid JSON.

JSON OUTPUT FORMAT:
{
  ""message"": ""Your spoken response here."",
  ""end_conversation"": false
}
Set ""end_conversation"" to true ONLY if the user says ""Goodbye"".
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
    }

    public void StartIntro()
    {
        conversationManager.StartConversation();
        string triggerMsg = "The user has just approached you. Say 'Hey there, long time no see', and introduce yourself briefly.";
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
using GoogleTextToSpeech.Scripts;
using GoogleTextToSpeech.Scripts.Data;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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


    private List<Content> chatHistory = new List<Content>();
    private bool isProcessing = false;


    private IEnumerator SendPromptRequestToGemini(string promptText)
    {
        string url = $"{apiEndpoint}?key={apiKey}";

        string jsonData = "{\"contents\": [{\"parts\": [{\"text\": \"{" + promptText + "}\"}]}]}";

        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);

        // Create a UnityWebRequest with the JSON data
        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Gemini API Error: " + www.error);
                if (chatManager != null)
                {
                    chatManager.AddSystemMessage("Error: Failed to get response from AI. Please try again.");
                }
            }
            else
            {
                Debug.Log("Request complete!");
                Response response = JsonUtility.FromJson<Response>(www.downloadHandler.text);

                // ✅ Corrected line below
                if (response.candidates != null && response.candidates.Length > 0 &&
                    response.candidates[0].content.parts != null && response.candidates[0].content.parts.Count > 0)
                {
                    string text = response.candidates[0].content.parts[0].text;
                    Debug.Log(text);

                    chatManager?.AddAIMessage(text);
                }
                else
                {
                    Debug.Log("No text found.");
                    if (chatManager != null)
                    {
                        chatManager.AddSystemMessage("AI response was empty. Please try again.");
                    }
                }
            }
        }
    }
    void Start()
    {
        if (chatManager != null)
        {
            ChatManager.OnUserMessage += HandleUserMessage;
        }
    }

    private void OnDestroy()
    {
        ChatManager.OnUserMessage -= HandleUserMessage;
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

                    if (response?.candidates != null &&
                        response.candidates.Count > 0 &&
                        response.candidates[0].content.parts.Count > 0)
                    {
                        string reply = response.candidates[0].content.parts[0].text;

                        Content botContent = new Content
                        {
                            role = "model",
                            parts = new List<Part> { new Part { text = reply } }
                        };
                        chatHistory.Add(botContent);

                        Debug.Log("AI Response: " + reply);
                        chatManager?.AddAIMessage(reply);
                        googleServices?.SendTextToGoogle(reply);
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
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

[System.Serializable]
public class ChatMessage
{
    public string sender;
    public string message;
    public DateTime timestamp;
    public MessageType type;

    public ChatMessage(string sender, string message, MessageType type)
    {
        this.sender = sender;
        this.message = message;
        this.type = type;
        this.timestamp = DateTime.Now;
    }
}

public enum MessageType
{
    User,
    AI,
    System
}

public class ChatManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text chatDisplay;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Chat Settings")]
    [SerializeField] private int maxMessages = 100;
    [SerializeField] private bool showTimestamps = false;
    [SerializeField] private bool autoScroll = true;

    [Header("Message Colors")]
    [SerializeField] private Color userColor = Color.blue;
    [SerializeField] private Color aiColor = Color.green;
    [SerializeField] private Color systemColor = Color.gray;

    private List<ChatMessage> chatHistory = new List<ChatMessage>();

    // Events for other scripts to subscribe to
    public static event Action<string> OnUserMessage;
    public static event Action<ChatMessage> OnMessageAdded;

    private void Start()
    {
        // Add a welcome message
        AddSystemMessage("Chat system initialized. Hold SPACE to speak!");
    }

    private void OnDestroy()
    {
        // No UI cleanup needed for speech-only system
    }

    // Method to add user message
    public void AddUserMessage(string message)
    {
        if (string.IsNullOrEmpty(message.Trim())) return;

        ChatMessage chatMessage = new ChatMessage("You", message, MessageType.User);
        AddMessageToHistory(chatMessage);
        UpdateChatDisplay();

        // Notify other scripts that user sent a message
        OnUserMessage?.Invoke(message);
    }

    // Method to add AI response
    public void AddAIMessage(string message)
    {
        if (string.IsNullOrEmpty(message.Trim())) return;

        ChatMessage chatMessage = new ChatMessage("AI", message, MessageType.AI);
        AddMessageToHistory(chatMessage);
        UpdateChatDisplay();
    }

    // Method to add system messages
    public void AddSystemMessage(string message)
    {
        ChatMessage chatMessage = new ChatMessage("System", message, MessageType.System);
        AddMessageToHistory(chatMessage);
        UpdateChatDisplay();
    }

    // Generic method to add any message
    public void AddMessage(string sender, string message, MessageType type)
    {
        ChatMessage chatMessage = new ChatMessage(sender, message, type);
        AddMessageToHistory(chatMessage);
        UpdateChatDisplay();
    }

    private void AddMessageToHistory(ChatMessage message)
    {
        chatHistory.Add(message);

        // Remove old messages if we exceed the limit
        if (chatHistory.Count > maxMessages)
        {
            chatHistory.RemoveAt(0);
        }

        // Notify subscribers
        OnMessageAdded?.Invoke(message);
    }

    private void UpdateChatDisplay()
    {
        if (chatDisplay == null) return;

        string displayText = "";

        foreach (ChatMessage message in chatHistory)
        {
            string colorHex = GetColorForMessageType(message.type);
            string timestamp = showTimestamps ? $"[{message.timestamp:HH:mm:ss}] " : "";

            displayText += $"{timestamp}<color={colorHex}><b>{message.sender}:</b></color> {message.message}\n\n";
        }

        chatDisplay.text = displayText;

        // Auto scroll to bottom
        if (autoScroll && scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private string GetColorForMessageType(MessageType type)
    {
        Color color = type switch
        {
            MessageType.User => userColor,
            MessageType.AI => aiColor,
            MessageType.System => systemColor,
            _ => Color.white
        };

        return "#" + ColorUtility.ToHtmlStringRGB(color);
    }

    // Method to clear chat history
    public void ClearChat()
    {
        chatHistory.Clear();
        UpdateChatDisplay();
        AddSystemMessage("Chat cleared. Hold SPACE to speak!");
    }

    // Method to get chat history as string (useful for saving)
    public string GetChatHistoryAsString()
    {
        string history = "";
        foreach (ChatMessage message in chatHistory)
        {
            history += $"[{message.timestamp}] {message.sender}: {message.message}\n";
        }
        return history;
    }

    // Method to get recent messages for context
    public List<ChatMessage> GetRecentMessages(int count = 10)
    {
        int startIndex = Mathf.Max(0, chatHistory.Count - count);
        int actualCount = chatHistory.Count - startIndex;

        List<ChatMessage> recent = new List<ChatMessage>();
        for (int i = startIndex; i < chatHistory.Count; i++)
        {
            recent.Add(chatHistory[i]);
        }

        return recent;
    }

    // Public property to access chat history
    public List<ChatMessage> ChatHistory => new List<ChatMessage>(chatHistory);
}
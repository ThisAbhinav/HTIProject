using TMPro;
using UnityEngine;
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

    [Header("Chat Settings")]
    [SerializeField] private int maxVisibleMessages = 15;
    [SerializeField] private bool showTimestamps = false;
    [SerializeField] private bool cleanDisplayText = true;

    [Header("Message Colors")]
    [SerializeField] private Color userColor = Color.blue;
    [SerializeField] private Color aiColor = Color.green;
    [SerializeField] private Color systemColor = Color.gray;

    private List<ChatMessage> chatHistory = new List<ChatMessage>();

    public static event Action<string> OnUserMessage;
    public static event Action<ChatMessage> OnMessageAdded;

    private void Start()
    {
        SetupChatDisplay();
        AddSystemMessage("Alex - Your friend From abroad | HTI Experiment");
    }

    private void SetupChatDisplay()
    {
        if (chatDisplay == null) return;

        chatDisplay.textWrappingMode = TextWrappingModes.Normal;
    }

    public void AddUserMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        ChatMessage chatMessage = new ChatMessage("You", message, MessageType.User);
        AddMessageToHistory(chatMessage);
        UpdateChatDisplay();

        OnUserMessage?.Invoke(message);
    }

    public void AddAIMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        ChatMessage chatMessage = new ChatMessage("Alex", message, MessageType.AI);
        AddMessageToHistory(chatMessage);
        UpdateChatDisplay();
    }

    public void UpdateAIMessage(string message)
    {
        if (chatHistory.Count == 0 || chatHistory[chatHistory.Count - 1].type != MessageType.AI)
        {
            AddAIMessage(message);
            return;
        }

        // Update the last AI message in the history
        chatHistory[chatHistory.Count - 1].message = message;
        UpdateChatDisplay();
    }

    public void AddSystemMessage(string message)
    {
        ChatMessage chatMessage = new ChatMessage("System", message, MessageType.System);
        AddMessageToHistory(chatMessage);
        UpdateChatDisplay();
    }

    public void AddMessage(string sender, string message, MessageType type)
    {
        ChatMessage chatMessage = new ChatMessage(sender, message, type);
        AddMessageToHistory(chatMessage);
        UpdateChatDisplay();
    }

    private void AddMessageToHistory(ChatMessage message)
    {
        chatHistory.Add(message);
        OnMessageAdded?.Invoke(message);
    }

    private void UpdateChatDisplay()
    {
        if (chatDisplay == null) return;

        // Get the range of messages to display (most recent N messages)
        int startIndex = Mathf.Max(0, chatHistory.Count - maxVisibleMessages);
        List<ChatMessage> visibleMessages = chatHistory.GetRange(startIndex, chatHistory.Count - startIndex);

        string displayText = "";

        foreach (ChatMessage message in visibleMessages)
        {
            string colorHex = GetColorForMessageType(message.type);
            string timestamp = showTimestamps ? $"[{message.timestamp:HH:mm:ss}] " : "";

            string messageContent = cleanDisplayText ?
                TextCleanerUtility.CleanForDisplay(message.message) :
                message.message;

            displayText += $"{timestamp}<color={colorHex}><b>{message.sender}:</b></color> {messageContent}\n\n";
        }

        chatDisplay.text = displayText.TrimEnd();
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

    public void ClearChat()
    {
        chatHistory.Clear();
        UpdateChatDisplay();
        AddSystemMessage("Chat cleared. Hold SPACE to speak!");
    }

    public string GetChatHistoryAsString()
    {
        string history = "";
        foreach (ChatMessage message in chatHistory)
        {
            history += $"[{message.timestamp}] {message.sender}: {message.message}\n";
        }
        return history;
    }

    public List<ChatMessage> GetRecentMessages(int count = 10)
    {
        int startIndex = Mathf.Max(0, chatHistory.Count - count);
        return chatHistory.GetRange(startIndex, chatHistory.Count - startIndex);
    }

    public List<ChatMessage> ChatHistory => new List<ChatMessage>(chatHistory);

    public void ToggleTextCleaning(bool enabled)
    {
        cleanDisplayText = enabled;
        UpdateChatDisplay();
    }

    public void SetMaxVisibleMessages(int count)
    {
        maxVisibleMessages = Mathf.Max(1, count);
        UpdateChatDisplay();
    }
}
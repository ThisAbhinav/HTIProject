using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System;

/// <summary>
/// Manages background information discovery tasks for HTI experiment
/// Tracks what info has been naturally revealed during conversation
/// Emits events that ConversationManager subscribes to
/// </summary>
public class TaskManager : MonoBehaviour
{
    [SerializeField] private GameObject TasksBox;
    [SerializeField] private List<UserTask> tasks = new()
    {
            new UserTask("Find out which college Alex studies at", new List<string> { "berkeley", "uc berkeley", "university of california"}),
            new UserTask("Learn who Alex’s roommate is", new List<string> { "jake" }),
            new UserTask("Discover what Alex does part-time", new List<string> { "teaching assistant", "ta", "intro cs", "cs ta" }),
            new UserTask("Figure out Alex’s favorite weekend activity", new List<string> { "hiking", "hike", "trails" }),
            new UserTask("Find out Alex’s favorite food spot", new List<string> { "telegraph avenue", "food trucks", "dining hall" }),
            new UserTask("Learn where Alex is from", new List<string> { "san francisco", "sf" })
        };

private Dictionary<UserTask, TextMeshProUGUI> taskTextMap = new Dictionary<UserTask, TextMeshProUGUI>();

    public static event Action<string> OnTaskCompleted;
    public static event Action<int, int> OnTaskProgressChanged; 
    public int CompletedTasksCount { get; private set; }
    public int TotalTasksCount => tasks.Count;

    void Start()
    {
        InitializeTaskUI();
        CompletedTasksCount = 0;
    }

    private void InitializeTaskUI()
    {
        foreach (UserTask task in tasks)
        {
            GameObject taskTextObj = new GameObject(task.title);
            taskTextObj.transform.SetParent(TasksBox.transform, false);

            TextMeshProUGUI textComponent = taskTextObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = "☐ " + task.title;
            textComponent.fontSize = 12;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Left;

            RectTransform rect = taskTextObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(220, 20);

            taskTextMap.Add(task, textComponent);
        }
    }

    /// <summary>
    /// Check avatar response for task completion keywords
    /// Called by UnityAndGeminiV3 after each AI response
    /// </summary>
    public void CutTasks(string avatarResponse)
    {
        if (string.IsNullOrEmpty(avatarResponse)) return;

        avatarResponse = avatarResponse.ToLower();

        foreach (UserTask task in tasks)
        {
            if (!task.isCompleted)
            {
                foreach (string keyword in task.keywords)
                {
                    if (avatarResponse.Contains(keyword.ToLower()))
                    {
                        task.isCompleted = true;
                        CompletedTasksCount++;
                        UpdateTaskVisual(task);
                        
                        Debug.Log($"[TaskManager] ✓ Discovered: {task.title} ({CompletedTasksCount}/{TotalTasksCount})");
                        
                        // Notify ConversationManager
                        OnTaskCompleted?.Invoke(task.title);
                        OnTaskProgressChanged?.Invoke(CompletedTasksCount, TotalTasksCount);
                        break;
                    }
                }
            }
        }
    }

    private void UpdateTaskVisual(UserTask task)
    {
        if (taskTextMap.TryGetValue(task, out TextMeshProUGUI text))
        {
            text.text = "☑ " + task.title;
            text.fontStyle = FontStyles.Strikethrough;
            text.color = new Color(0.5f, 0.5f, 0.5f, 0.7f); // Gray with slight transparency
        }
    }

    /// <summary>
    /// Get completion status for ConversationManager
    /// </summary>
    public bool HasMetMinimumInfo(int minRequired)
    {
        return CompletedTasksCount >= minRequired;
    }

    /// <summary>
    /// Reset all tasks (for testing)
    /// </summary>
    [ContextMenu("Reset All Tasks")]
    public void ResetAllTasks()
    {
        CompletedTasksCount = 0;
        foreach (UserTask task in tasks)
        {
            task.isCompleted = false;
            if (taskTextMap.TryGetValue(task, out TextMeshProUGUI text))
            {
                text.text = "☐ " + task.title;
                text.fontStyle = FontStyles.Normal;
            }
        }
        OnTaskProgressChanged?.Invoke(0, TotalTasksCount);
    }
}

[System.Serializable]
public class UserTask
{
    public string title;
    public List<string> keywords;
    public bool isCompleted;

    public UserTask(string title, List<string> keywords)
    {
        this.title = title;
        this.keywords = keywords;
        this.isCompleted = false;
    }
}

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
        new UserTask("Find out Alex's major", new List<string> { "computer science", "cs", "comp sci" }),
        new UserTask("Learn which dorm Alex lives in", new List<string> { "unit 1", "blackwell", "foothill", "unit 2", "unit 3" }),
        new UserTask("Discover Alex's favorite hobby", new List<string> { "photography", "photos", "camera" }),
        new UserTask("Ask where the best coffee in Berkerley ", new List<string> { "caffe strada", "strada", "blue bottle" })
    };

private Dictionary<UserTask, TextMeshProUGUI> taskTextMap = new Dictionary<UserTask, TextMeshProUGUI>();

    public static event Action<string> OnTaskCompleted;
    public static event Action<int, int> OnTaskProgressChanged;
    public static event Action OnAllTasksCompleted; 
    
    public int CompletedTasksCount { get; private set; }
    public int TotalTasksCount => tasks.Count;

    void Start()
    {
        InitializeTaskUI();
        CompletedTasksCount = 0;
    }
    
    /// <summary>
    /// Check if all tasks are completed
    /// </summary>
    public bool AllTasksCompleted()
    {
        return CompletedTasksCount >= TotalTasksCount;
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
                        CompleteTask(task, keyword); 
                        break;
                    }
                }
            }
        }
    }

    private void CompleteTask(UserTask task, string foundAnswer)
    {
        task.isCompleted = true;
        CompletedTasksCount++;
        
        if (taskTextMap.TryGetValue(task, out TextMeshProUGUI text))
        {
            text.text = $"<s>{task.title}</s> <color=#00FF00>({foundAnswer})</color>";
            text.color = new Color(0.8f, 0.8f, 0.8f, 1f); 
        }

        Debug.Log($"Task Completed: {task.title}");
    }


    public string GetTaskStatusForPrompt()
    {
        // Helper to tell LLM what is left
        string status = "Tasks User needs to complete: ";
        foreach(var t in tasks)
        {
            if(!t.isCompleted) status += t.title + ", ";
        }
        if (AllTasksCompleted()) status = "ALL TASKS COMPLETED. You can say goodbye now.";
        return status;
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
                text.color = Color.white;
            }
        }
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

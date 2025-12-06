using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System;
using System.Linq;
/// <summary>
/// Manages background information discovery tasks for HTI experiment
/// Tracks what info has been naturally revealed during conversation
/// Emits events that ConversationManager subscribes to
/// </summary>
public class TaskManager : MonoBehaviour
{
    [SerializeField] private GameObject TasksBox;
    [SerializeField] private TMP_FontAsset interFont;
    private List<UserTask> activeTasks = new List<UserTask>();
    private Dictionary<UserTask, TextMeshProUGUI> taskTextMap = new Dictionary<UserTask, TextMeshProUGUI>();
    private List<UserTask> masterTaskPool = new()
    {
        new UserTask("Find out Alex's major", new List<string> { "computer science", "cs", "comp sci" }),
        new UserTask("Learn which dorm Alex lives in", new List<string> { "blackwell" }),
        new UserTask("Discover Alex's favorite hobby", new List<string> { "photography", "photos", "camera" }),
        new UserTask("Ask where the best coffee is", new List<string> { "caffe strada", "strada" }),
        new UserTask("Find out Alex's hometown", new List<string> { "san francisco", "sf", "bay area" }),
        new UserTask("Ask what year Alex is in", new List<string> { "senior", "4th year", "fourth year" }),
        new UserTask("Ask about Alex's favorite food", new List<string> { "pizza", "sliver", "cheeseboard" }),
        new UserTask("Find out which club Alex joined", new List<string> { "robotics", "ieee" }),
        new UserTask("Ask about Alex's pet", new List<string> { "cooper", "golden retriever", "dog" }),
        new UserTask("Learn Alex's favorite library", new List<string> { "moffitt" }),
        new UserTask("Find out how Alex gets to class", new List<string> { "skateboard", "electric board" }),
        new UserTask("Ask for Alex's favorite movie", new List<string> { "interstellar", "nolan" }),
        new UserTask("Discover Alex's music taste", new List<string> { "lo-fi", "jazz", "hip hop" }),
        new UserTask("Ask if Alex has siblings", new List<string> { "sister", "maya" }),
        new UserTask("Find out Alex's favorite sport", new List<string> { "basketball", "warriors" }),
        new UserTask("Ask about Alex's summer plans", new List<string> { "google", "internship" }),
        new UserTask("Find out Alex's favorite drink (non-coffee)", new List<string> { "boba", "bubble tea", "plentea" }),
        new UserTask("Ask who Alex's favorite professor is", new List<string> { "denero", "john denero" }),
        new UserTask("Find out what game Alex is playing", new List<string> { "elden ring", "souls" }),
        new UserTask("Ask for a campus landmark", new List<string> { "campanile", "clock tower" })
    };

    public static event Action<string> OnTaskCompleted;
    public static event Action<int, int> OnTaskProgressChanged;
    public static event Action OnAllTasksCompleted; 
    
    public int CompletedTasksCount { get; private set; }
    public int TotalTasksCount => activeTasks.Count;

    void Start()
    {
        SelectRandomTasks();
        InitializeTaskUI();
        CompletedTasksCount = 0;
    }

    private void SelectRandomTasks()
    {
        // Shuffle the master pool
        System.Random updateRandom = new System.Random();
        List<UserTask> shuffledTasks = masterTaskPool.OrderBy(x => updateRandom.Next()).ToList();

        // Take the first 4
        activeTasks = shuffledTasks.Take(4).ToList();

        Debug.Log($"Tasks selected for this session: {string.Join(", ", activeTasks.Select(t => t.title))}");
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
        foreach (Transform child in TasksBox.transform) Destroy(child.gameObject);
        taskTextMap.Clear();

        foreach (UserTask task in activeTasks)
        {
            GameObject taskTextObj = new GameObject(task.title);
            taskTextObj.transform.SetParent(TasksBox.transform, false);

            TextMeshProUGUI textComponent = taskTextObj.AddComponent<TextMeshProUGUI>();
            textComponent.font = interFont;
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

        foreach (UserTask task in activeTasks)
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

        if (AllTasksCompleted())
        {
            OnAllTasksCompleted?.Invoke();
        }
    }


    public string GetTaskStatusForPrompt()
    {
        string status = "Tasks User is trying to solve (Help them naturally): ";
        foreach (var t in activeTasks)
        {
            if (!t.isCompleted) status += t.title + ", ";
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
        // For debugging, we just reset the current active ones
        CompletedTasksCount = 0;
        foreach (UserTask task in activeTasks)
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

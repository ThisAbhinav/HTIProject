using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
public class TaskManager : MonoBehaviour
{
    [SerializeField] private GameObject TasksBox;

    // Use Task objects instead of strings for better control
    [SerializeField] private List<UserTask> tasks = new List<UserTask>();

    // Keep track of text objects for UI updates
    private Dictionary<UserTask, TextMeshProUGUI> taskTextMap = new ();

    void Start()
    {
        RectTransform boxRect = TasksBox.GetComponent<RectTransform>();
        boxRect.sizeDelta = new Vector2(250, 300);
        boxRect.transform.localPosition = boxRect.transform.localPosition + new Vector3(0, 0, 0);


        ContentSizeFitter fitter = TasksBox.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Create text for each task
        foreach (UserTask task in tasks)
        {
            GameObject taskTextObj = new GameObject(task.title);
            taskTextObj.transform.SetParent(TasksBox.transform, false);

            // Add TextMeshProUGUI
            TextMeshProUGUI textComponent = taskTextObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = "• " + task.title;
            textComponent.fontSize = 15;
            textComponent.color = Color.black;
            textComponent.alignment = TextAlignmentOptions.Left;

            // Adjust layout width
            RectTransform rect = taskTextObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(230, 20);

            taskTextMap.Add(task, textComponent);
        }
    }

    public void CutTasks(string avatarResponse)
    {
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
                        UpdateTaskVisual(task);
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
            // Apply strikethrough for completed tasks
            text.fontStyle = FontStyles.Strikethrough;
            text.color = Color.gray;
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

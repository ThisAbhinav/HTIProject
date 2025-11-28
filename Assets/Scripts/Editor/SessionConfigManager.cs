using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

#if UNITY_EDITOR
/// <summary>
/// Editor window for managing participant session configurations
/// </summary>
public class SessionConfigManager : EditorWindow
{
    private string participantId = "P01";
    private int sessionNumber = 1;
    private Vector2 scrollPosition;
    private Vector2 usedQuestionsScrollPosition;

    [MenuItem("HTI Experiment/Session Config Manager")]
    public static void ShowWindow()
    {
        GetWindow<SessionConfigManager>("Session Configs");
    }

    private void OnGUI()
    {
        GUILayout.Label("Session Configuration Manager", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Current Session Setup
        GUILayout.Label("Set Current Session", EditorStyles.boldLabel);
        participantId = EditorGUILayout.TextField("Participant ID:", participantId.ToUpper());
        sessionNumber = EditorGUILayout.IntSlider("Session Number:", sessionNumber, 1, 4);
        
        // Show feedback type for this combination
        string feedbackType = SessionConfiguration.Instance.GetFeedbackTypeForSession(participantId, sessionNumber);
        EditorGUILayout.HelpBox($"Feedback Type: {feedbackType}", MessageType.None);

        if (GUILayout.Button("Set Session", GUILayout.Height(30)))
        {
            SessionConfiguration.Instance.SetSession(participantId, sessionNumber);
            Debug.Log($"Session set to {participantId} - Session {sessionNumber} ({feedbackType})");
        }

        GUILayout.Space(10);

        // Check if config exists
        bool hasConfig = SessionConfiguration.Instance.HasSavedConfig();
        if (hasConfig)
        {
            EditorGUILayout.HelpBox($"Saved configuration exists for {participantId} S{sessionNumber}", MessageType.Info);
            
            if (GUILayout.Button("Delete This Config", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Delete Configuration", 
                    $"Delete saved questions for {participantId} Session {sessionNumber}?", 
                    "Delete", "Cancel"))
                {
                    SessionConfiguration.Instance.DeleteSessionConfig(participantId, sessionNumber);
                    Debug.Log($"Deleted config for {participantId} S{sessionNumber}");
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox($"No saved configuration for {participantId} S{sessionNumber}", MessageType.Warning);
        }

        GUILayout.Space(10);

        // Show used questions for this participant
        GUILayout.Label($"Questions Used by {participantId}", EditorStyles.boldLabel);
        
        List<int> usedQuestions = SessionConfiguration.Instance.GetUsedQuestionIndices(participantId);
        List<int> availableQuestions = SessionConfiguration.Instance.GetAvailableQuestionIndices(participantId, 20);
        List<int> completedSessions = SessionConfiguration.Instance.GetCompletedSessions(participantId);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.HelpBox($"Sessions: {completedSessions.Count}/4", MessageType.Info, true);
        EditorGUILayout.HelpBox($"Questions Used: {usedQuestions.Count}/20", MessageType.Info, true);
        EditorGUILayout.HelpBox($"Available: {availableQuestions.Count}/20", MessageType.Info, true);
        EditorGUILayout.EndHorizontal();
        
        // Show which sessions are completed
        if (completedSessions.Count > 0)
        {
            string sessionsStr = string.Join(", ", completedSessions);
            EditorGUILayout.LabelField($"Completed Sessions: {sessionsStr}", EditorStyles.miniLabel);
        }
        
        if (usedQuestions.Count > 0)
        {
            usedQuestionsScrollPosition = EditorGUILayout.BeginScrollView(usedQuestionsScrollPosition, GUILayout.Height(80));
            string usedStr = string.Join(", ", usedQuestions);
            EditorGUILayout.SelectableLabel($"Used indices: {usedStr}", GUILayout.Height(60));
            EditorGUILayout.EndScrollView();
        }

        GUILayout.Space(20);

        // List all saved configurations
        GUILayout.Label("All Saved Configurations", EditorStyles.boldLabel);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
        
        List<string> savedConfigs = SessionConfiguration.Instance.GetAllSavedConfigs();
        
        if (savedConfigs.Count == 0)
        {
            EditorGUILayout.HelpBox("No saved configurations found", MessageType.Info);
        }
        else
        {
            foreach (string config in savedConfigs)
            {
                EditorGUILayout.BeginHorizontal("box");
                GUILayout.Label(config, GUILayout.Width(150));
                
                if (GUILayout.Button("Load", GUILayout.Width(60)))
                {
                    string[] parts = config.Split('_');
                    if (parts.Length == 2)
                    {
                        participantId = parts[0];
                        sessionNumber = int.Parse(parts[1].Replace("S", ""));
                        SessionConfiguration.Instance.SetSession(participantId, sessionNumber);
                        Debug.Log($"Loaded session: {participantId} S{sessionNumber}");
                    }
                }
                
                if (GUILayout.Button("Delete", GUILayout.Width(60)))
                {
                    string[] parts = config.Split('_');
                    if (parts.Length == 2)
                    {
                        string pid = parts[0];
                        int snum = int.Parse(parts[1].Replace("S", ""));
                        
                        if (EditorUtility.DisplayDialog("Delete Configuration", 
                            $"Delete saved questions for {config}?", 
                            "Delete", "Cancel"))
                        {
                            SessionConfiguration.Instance.DeleteSessionConfig(pid, snum);
                            Debug.Log($"Deleted config for {config}");
                        }
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        EditorGUILayout.EndScrollView();

        GUILayout.Space(10);

        // Instructions
        EditorGUILayout.HelpBox(
            "How to use:\n" +
            "1. Set Participant ID (P01-P20) and Session Number (1-4)\n" +
            "2. Feedback type is automatically determined from counterbalanced design\n" +
            "3. Click 'Set Session'\n" +
            "4. Start the game - questions will be saved automatically\n" +
            "5. Questions used in previous sessions are automatically excluded\n" +
            "6. Each participant completes exactly 4 sessions with 4 unique questions each\n" +
            "7. If you replay the same participant/session, saved questions will be used\n" +
            "8. Delete configs to generate new random questions\n\n" +
            "Feedback Types: Baseline (1), Gestures (2), Visual (3), Verbal (4)\n" +
            "Each participant has a unique counterbalanced order",
            MessageType.Info);
    }
}
#endif

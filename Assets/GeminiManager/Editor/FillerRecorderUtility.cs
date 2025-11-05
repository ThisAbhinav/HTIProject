using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using GoogleTextToSpeech.Scripts;
using GoogleTextToSpeech.Scripts.Data;

/// <summary>
/// Editor utility to batch-record filler phrases using Google TTS
/// Records all fillers once to avoid repeated API calls during experiments
/// </summary>
public class FillerRecorderUtility : EditorWindow
{
    [SerializeField] private VoiceScriptableObject voice;
    [SerializeField] private TextToSpeech textToSpeech;
    
    private string outputDirectory = "Assets/Audio/Fillers";
    private int recordedCount = 0;
    private int totalCount = 0;
    private bool isRecording = false;
    private string statusMessage = "";

    // Comprehensive list of filler phrases
    // Categorized by context and emotion
    private static readonly Dictionary<string, List<string>> FillerCategories = new Dictionary<string, List<string>>
    {
        // Short thinking sounds
        ["Short Thinking"] = new List<string>
        {
            "um",
            "uh",
            "hmm",
            "ah",
            "er",
            "oh"
        },

        // Longer thinking phrases
        ["Thinking Phrases"] = new List<string>
        {
            "let me think",
            "hmm let me see",
            "uh let me think about that",
            "that's a good question",
            "interesting question",
            "hmm interesting"
        },

        // Positive acknowledgment with thinking
        ["Positive Thinking"] = new List<string>
        {
            "oh that's interesting",
            "hmm that's cool",
            "oh wow",
            "interesting",
            "that's a good point"
        },

        // Conversational fillers
        ["Conversational"] = new List<string>
        {
            "you know",
            "I mean",
            "well",
            "so",
            "like",
            "actually"
        },

        // Pauses and hesitations
        ["Hesitation"] = new List<string>
        {
            "hmm how do I put it",
            "uh how should I say",
            "let me put it this way",
            "hmm where do I start"
        },

        // Processing longer thoughts
        ["Processing"] = new List<string>
        {
            "hmm give me a second",
            "let me think for a moment",
            "uh I need to think about that",
            "that's a great question let me see"
        }
    };

    [MenuItem("HTI Tools/Filler Recorder")]
    public static void ShowWindow()
    {
        GetWindow<FillerRecorderUtility>("Filler Recorder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Filler Audio Recorder", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Voice configuration
        voice = (VoiceScriptableObject)EditorGUILayout.ObjectField(
            "Voice Profile", voice, typeof(VoiceScriptableObject), false);
        
        textToSpeech = (TextToSpeech)EditorGUILayout.ObjectField(
            "TTS Manager", textToSpeech, typeof(TextToSpeech), true);

        GUILayout.Space(10);

        // Output directory
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Output Directory:", GUILayout.Width(120));
        outputDirectory = EditorGUILayout.TextField(outputDirectory);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFolderPanel("Select Output Directory", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                outputDirectory = "Assets" + path.Substring(Application.dataPath.Length);
            }
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Display filler categories
        GUILayout.Label("Filler Phrases to Record:", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        totalCount = 0;
        foreach (var category in FillerCategories)
        {
            EditorGUILayout.LabelField($"{category.Key}: {category.Value.Count} phrases", EditorStyles.miniLabel);
            totalCount += category.Value.Count;
        }
        EditorGUILayout.LabelField($"Total: {totalCount} phrases", EditorStyles.boldLabel);
        
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // Record button
        EditorGUI.BeginDisabledGroup(isRecording || voice == null || textToSpeech == null);
        if (GUILayout.Button(isRecording ? "Recording..." : "Record All Fillers", GUILayout.Height(30)))
        {
            RecordAllFillers();
        }
        EditorGUI.EndDisabledGroup();

        // Status
        if (!string.IsNullOrEmpty(statusMessage))
        {
            GUILayout.Space(10);
            EditorGUILayout.HelpBox(statusMessage, UnityEditor.MessageType.Info);
        }

        // Progress
        if (isRecording)
        {
            float progress = totalCount > 0 ? (float)recordedCount / totalCount : 0f;
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progress, 
                $"Recording: {recordedCount}/{totalCount}");
        }

        GUILayout.Space(10);

        // Info box
        EditorGUILayout.HelpBox(
            "This tool records all filler phrases using Google TTS once and saves them as .wav files.\n\n" +
            "Benefits:\n" +
            "• Saves API costs (no repeated TTS calls)\n" +
            "• Faster playback during experiments\n" +
            "• Consistent audio quality\n\n" +
            "Make sure your Google TTS API key is configured before recording.",
            UnityEditor.MessageType.Info);
    }

    private void RecordAllFillers()
    {
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        isRecording = true;
        recordedCount = 0;
        statusMessage = "Starting recording...";
        
        RecordNextFiller();
    }

    private void RecordNextFiller()
    {
        // Get all fillers in a flat list
        List<(string category, string phrase)> allFillers = new List<(string, string)>();
        foreach (var kvp in FillerCategories)
        {
            foreach (var fillerPhrase in kvp.Value)
            {
                allFillers.Add((kvp.Key, fillerPhrase));
            }
        }

        if (recordedCount >= allFillers.Count)
        {
            // Done!
            isRecording = false;
            statusMessage = $"✓ Successfully recorded {recordedCount} filler audio files!";
            AssetDatabase.Refresh();
            Debug.Log($"[Filler Recorder] Completed! {recordedCount} files saved to {outputDirectory}");
            return;
        }

        var (category, phrase) = allFillers[recordedCount];
        string filename = SanitizeFilename(phrase);
        string outputPath = Path.Combine(outputDirectory, $"{filename}.wav");

        statusMessage = $"Recording: '{phrase}' ({recordedCount + 1}/{allFillers.Count})";
        
        // Record using TTS
        textToSpeech.GetSpeechAudioFromGoogle(
            phrase,
            voice,
            (audioClip) => OnAudioReceived(audioClip, outputPath, phrase),
            (error) => OnRecordError(error, phrase)
        );
    }

    private void OnAudioReceived(AudioClip clip, string outputPath, string phrase)
    {
        // Save audio clip as WAV
        if (SaveAudioClipAsWav(clip, outputPath))
        {
            recordedCount++;
            Debug.Log($"[Filler Recorder] Saved: '{phrase}' -> {outputPath}");
            
            // Record next
            EditorApplication.delayCall += RecordNextFiller;
        }
        else
        {
            Debug.LogError($"[Filler Recorder] Failed to save: {phrase}");
            isRecording = false;
            statusMessage = $"✗ Error saving: {phrase}";
        }
    }

    private void OnRecordError(BadRequestData error, string phrase)
    {
        Debug.LogError($"[Filler Recorder] TTS Error for '{phrase}': {error.error.message}");
        statusMessage = $"✗ TTS Error: {error.error.message}";
        isRecording = false;
    }

    private bool SaveAudioClipAsWav(AudioClip clip, string filepath)
    {
        if (clip == null) return false;

        try
        {
            // Get audio data
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            // Convert to 16-bit PCM
            short[] intData = new short[samples.Length];
            byte[] bytesData = new byte[samples.Length * 2];

            int rescaleFactor = 32767; // Max value for 16-bit signed int

            for (int i = 0; i < samples.Length; i++)
            {
                intData[i] = (short)(samples[i] * rescaleFactor);
                byte[] byteArr = System.BitConverter.GetBytes(intData[i]);
                byteArr.CopyTo(bytesData, i * 2);
            }

            // Create WAV file
            using (FileStream fileStream = new FileStream(filepath, FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(fileStream))
            {
                // WAV file header
                writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
                writer.Write(36 + bytesData.Length);
                writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));
                
                // Format chunk
                writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
                writer.Write(16); // Subchunk size
                writer.Write((ushort)1); // Audio format (PCM)
                writer.Write((ushort)clip.channels);
                writer.Write(clip.frequency);
                writer.Write(clip.frequency * clip.channels * 2); // Byte rate
                writer.Write((ushort)(clip.channels * 2)); // Block align
                writer.Write((ushort)16); // Bits per sample
                
                // Data chunk
                writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
                writer.Write(bytesData.Length);
                writer.Write(bytesData);
            }

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Filler Recorder] Error saving WAV: {e.Message}");
            return false;
        }
    }

    private string SanitizeFilename(string phrase)
    {
        // Replace spaces and special characters
        string filename = phrase.ToLower()
            .Replace(" ", "_")
            .Replace("'", "")
            .Replace("?", "")
            .Replace("!", "")
            .Replace(",", "");
        
        // Limit length
        if (filename.Length > 50)
            filename = filename.Substring(0, 50);
        
        return filename;
    }

    /// <summary>
    /// Get all filler phrases as a flat list for export
    /// </summary>
    public static List<string> GetAllFillerPhrases()
    {
        List<string> allFillers = new List<string>();
        foreach (var category in FillerCategories)
        {
            allFillers.AddRange(category.Value);
        }
        return allFillers;
    }

    /// <summary>
    /// Export filler list to text file
    /// </summary>
    [MenuItem("HTI Tools/Export Filler List")]
    public static void ExportFillerList()
    {
        string path = EditorUtility.SaveFilePanel("Export Filler List", "Assets", "filler_list.txt", "txt");
        if (string.IsNullOrEmpty(path)) return;

        using (StreamWriter writer = new StreamWriter(path))
        {
            writer.WriteLine("FILLER PHRASES FOR HTI EXPERIMENT");
            writer.WriteLine("Generated: " + System.DateTime.Now);
            writer.WriteLine("Total phrases: " + GetAllFillerPhrases().Count);
            writer.WriteLine();

            foreach (var category in FillerCategories)
            {
                writer.WriteLine($"=== {category.Key.ToUpper()} ({category.Value.Count}) ===");
                foreach (var phrase in category.Value)
                {
                    writer.WriteLine($"  - {phrase}");
                }
                writer.WriteLine();
            }
        }

        Debug.Log($"[Filler Recorder] Exported filler list to: {path}");
        AssetDatabase.Refresh();
    }
}

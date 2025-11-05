# HTI PROJECT - SYSTEM ARCHITECTURE DIAGRAM

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        HTI EXPERIMENT SYSTEM FLOW                            │
└─────────────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────────────┐
│  USER INPUT (VR or Keyboard)                                                 │
│  • Hold Space Bar OR VR Trigger                                              │
│  • Record voice input                                                        │
└────────────────────────┬─────────────────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│  SPEECH-TO-TEXT (Google Cloud STT)                                           │
│  • Capture audio @ 44.1kHz                                                   │
│  • Encode to WAV → Base64                                                    │
│  • Send to Google Speech API                                                 │
│  • Receive transcript                                                        │
└────────────────────────┬─────────────────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│  CHAT MANAGER                                                                │
│  • Display user message                                                      │
│  • Trigger OnUserMessage event                                               │
└────────────────────────┬─────────────────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│  GEMINI LLM (UnityAndGeminiV3.cs)                                            │
│  ┌────────────────────────────────────────────────────────────────┐         │
│  │  1. Receive user message                                        │         │
│  │  2. CHECK: Is processing? → Wait if busy                        │         │
│  │  3. TRIGGER FEEDBACK (via FeedbackModeManager)                  │         │
│  │  4. Add natural thinking delay (0.5-1.5s random)                │         │
│  │  5. Build chat request with full history                        │         │
│  │  6. Send to Gemini API (gemini-2.5-flash)                       │         │
│  │  7. Receive response                                            │         │
│  │  8. Process: EnforceBrevity (2 sentences, 30 words max)         │         │
│  │  9. Process: CleanTextForSpeech (remove markdown, etc.)         │         │
│  │  10. STOP FEEDBACK                                              │         │
│  │  11. Display AI message                                         │         │
│  │  12. Send to TTS                                                │         │
│  └────────────────────────────────────────────────────────────────┘         │
└────────────────┬───────────────────────────────────────┬─────────────────────┘
                 │                                       │
                 │ (Step 3)                              │ (Step 12)
                 ▼                                       ▼
┌─────────────────────────────────────┐   ┌──────────────────────────────────┐
│  FEEDBACK MODE MANAGER               │   │  TEXT-TO-SPEECH MANAGER          │
│  ┌───────────────────────────────┐  │   │  • Clean text for speech         │
│  │ Current Mode: [Select One]    │  │   │  • Send to Google TTS API        │
│  │  • Verbal Filler              │  │   │  • Receive audio clip            │
│  │  • Gesture                    │  │   │  • Play through avatar voice     │
│  │  • Visual Cue                 │  │   │  • Ready Player Me lip sync      │
│  │  • None (Control)             │  │   └──────────────────────────────────┘
│  └───────────────────────────────┘  │
│                                      │
│  TriggerFeedback():                  │
│   ├─ IF Verbal → PlayFiller()        │
│   ├─ IF Gesture → PlayThinkingGesture()
│   ├─ IF Visual → ShowThinkingIndicator()
│   └─ IF None → (do nothing)          │
└─────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────────────────┐
│                      FEEDBACK MODALITY DETAILS                               │
└─────────────────────────────────────────────────────────────────────────────┘

MODE 1: VERBAL FILLER
┌────────────────────────────────────────────┐
│  TextToSpeechManager                       │
│  • Generate filler via Google TTS          │
│  • Random phrases: "um", "hmm", "uh", etc. │
│  • Play through avatar AudioSource         │
│  • Auto-stops when response ready          │
└────────────────────────────────────────────┘

MODE 2: GESTURE
┌────────────────────────────────────────────┐
│  GestureController                         │
│  • Trigger avatar animation (if available) │
│  • OR procedural gesture (head tilt)       │
│  • Visual thinking behavior                │
│  • Silent feedback                         │
└────────────────────────────────────────────┘

MODE 3: VISUAL CUE
┌────────────────────────────────────────────┐
│  VisualCueController                       │
│  • Show UI panel with indicator            │
│  • Animated icon (spinning/pulsing)        │
│  • Random text: "Thinking...", "Hmm..."    │
│  • Fade in/out smoothly                    │
└────────────────────────────────────────────┘

MODE 4: NONE (Control)
┌────────────────────────────────────────────┐
│  No feedback during processing             │
│  • Pure latency experience                 │
│  • Baseline for comparison                 │
└────────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────────────────┐
│                      LLM CHARACTER SYSTEM                                    │
└─────────────────────────────────────────────────────────────────────────────┘

CHARACTER: Alex Thompson
┌──────────────────────────────────────────────────────────────────────┐
│  CORE IDENTITY                                                        │
│  • 20 years old, UC Berkeley junior                                  │
│  • Computer Science major (AI/ML focus)                              │
│  • From San Francisco, California                                    │
│                                                                       │
│  DISCOVERABLE BACKGROUND                                             │
│  • Lives in dorms (roommate: Jake)                                   │
│  • Works as CS teaching assistant                                    │
│  • Computer Science club + Robotics team                             │
│  • Enjoys weekend hiking                                             │
│  • Favorite spot: Library overlooking bay                            │
│  • Eats at dining hall + Telegraph Ave food trucks                   │
│  • Takes BART to SF on weekends                                      │
│                                                                       │
│  PERSONALITY                                                          │
│  • Friendly, curious, thoughtful                                     │
│  • Natural thinking pauses                                           │
│  • Genuinely interested in other cultures                            │
│  • Casual college language                                           │
│  • Uses filler words: "um", "hmm", "you know"                        │
│                                                                       │
│  CONVERSATION STYLE                                                   │
│  ✓ Be conversational (casual chat)                                   │
│  ✓ Think naturally (pause, acknowledge complexity)                   │
│  ✓ Ask back (follow-up questions)                                    │
│  ✓ Share gradually (not info dump)                                   │
│  ✓ Be curious (ask about Punjab/India)                               │
│  ✓ Relate & compare (Berkeley ↔ Punjab)                              │
└──────────────────────────────────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────────────────┐
│                      DATA LOGGING SYSTEM                                     │
└─────────────────────────────────────────────────────────────────────────────┘

HTI_DataLogger
┌────────────────────────────────────────────────────────────────────┐
│  Logs to CSV:                                                       │
│  • All messages (user, AI, system) with timestamps                 │
│  • Feedback mode changes                                           │
│  • Session start/end                                               │
│  • Response times/latencies                                        │
│  • User actions & system events                                    │
│                                                                     │
│  Output: HTI_Logs/HTI_P001_20251105_143022.csv                     │
│                                                                     │
│  Format:                                                            │
│  Timestamp,ParticipantID,SessionID,FeedbackMode,EventType,...      │
└────────────────────────────────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────────────────┐
│                      EXPERIMENT CONTROL UI                                   │
└─────────────────────────────────────────────────────────────────────────────┘

HTI_ExperimentController
┌────────────────────────────────────────────────────────────────────┐
│  ┌──────────────────────────────────────────────────────┐          │
│  │  Feedback Mode: [Dropdown]       [Apply Button]      │          │
│  │   • Verbal Filler                                     │          │
│  │   • Gesture                                           │          │
│  │   • Visual Cue                                        │          │
│  │   • None (Control)                                    │          │
│  │                                                       │          │
│  │  Status: ✓ Current Mode: Verbal Filler               │          │
│  └──────────────────────────────────────────────────────┘          │
│                                                                     │
│  Keyboard Shortcuts:                                                │
│  • 1: Verbal Filler    • F1: Toggle Panel                          │
│  • 2: Gesture                                                       │
│  • 3: Visual Cue                                                    │
│  • 4: None                                                          │
└────────────────────────────────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────────────────┐
│                      TIMING CONFIGURATION                                    │
└─────────────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│  Natural Thinking Delay                                      │
│  • Min: 0.5 seconds                                          │
│  • Max: 1.5 seconds                                          │
│  • Randomized per response                                   │
│  • Simulates human processing time                           │
└──────────────────────────────────────────────────────────────┘
┌──────────────────────────────────────────────────────────────┐
│  Filler Delay                                                │
│  • 0.1 seconds before filler plays                           │
│  • Creates natural gap                                       │
└──────────────────────────────────────────────────────────────┘
┌──────────────────────────────────────────────────────────────┐
│  Response Constraints                                        │
│  • Target: 2-3 sentences                                     │
│  • Max: 30 words (enforced)                                  │
│  • Natural conversational chunks                             │
└──────────────────────────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────────────────┐
│                      CONVERSATION FLOW EXAMPLE                               │
└─────────────────────────────────────────────────────────────────────────────┘

USER (Punjab): "Hey! What's your typical day like at Berkeley?"
         │
         ▼
    [STT Transcription]
         │
         ▼
    [Display in Chat]
         │
         ▼
    [Send to Gemini LLM]
         │
         ├─► [TRIGGER FEEDBACK] ─────┬─► Verbal: "Hmm..."
         │                            ├─► Gesture: *head tilt*
         │   [Natural Delay 0.5-1.5s] └─► Visual: "Thinking..."
         │
         ▼
    [Gemini Processes with Alex's Context]
         │
         ▼
    [Response: "Oh interesting question! My days are pretty packed 
               actually. I usually have classes in the morning, 
               then I TA for an intro CS course in the afternoon. 
               How about you - what's your schedule like in Punjab?"]
         │
         ├─► [EnforceBrevity: Max 2 sentences, 30 words]
         │
         ├─► [CleanForSpeech: Remove markdown, normalize punctuation]
         │
         ├─► [STOP FEEDBACK]
         │
         ├─► [Display in Chat]
         │
         └─► [Send to Google TTS → Avatar Speaks]


┌─────────────────────────────────────────────────────────────────────────────┐
│                      RESEARCH METRICS                                        │
└─────────────────────────────────────────────────────────────────────────────┘

QUANTITATIVE (Auto-logged)           QUALITATIVE (Manual collection)
┌─────────────────────────────┐     ┌─────────────────────────────┐
│ • Response times (actual)    │     │ • User preference ranking    │
│ • Number of conversational   │     │ • Comments on naturalness    │
│   turns                      │     │ • Cultural exchange quality  │
│ • Session duration           │     │ • Comfort level feedback     │
│ • Mode switches              │     │ • Immersion ratings (1-5)    │
│ • Error events               │     │ • Perceived latency (1-5)    │
└─────────────────────────────┘     └─────────────────────────────┘


┌─────────────────────────────────────────────────────────────────────────────┐
│                      COMPONENT DEPENDENCY GRAPH                              │
└─────────────────────────────────────────────────────────────────────────────┘

                    FeedbackModeManager (Singleton)
                            │
        ┌───────────────────┼───────────────────┐
        │                   │                   │
        ▼                   ▼                   ▼
TextToSpeechManager   GestureController   VisualCueController
        │
        ├─► Google TTS API
        └─► Ready Player Me VoiceHandler

                    UnityAndGeminiV3
                            │
        ┌───────────────────┼───────────────────┐
        │                   │                   │
        ▼                   ▼                   ▼
  Gemini API        FeedbackModeManager    ChatManager
                            │
                            └─► TextToSpeechManager

                    SpeechToTextManager
                            │
                    ┌───────┴───────┐
                    │               │
                    ▼               ▼
          Google STT API      ChatManager
                                    │
                                    └─► UnityAndGeminiV3

                    HTI_DataLogger
                            │
        ┌───────────────────┼───────────────────┐
        │                   │                   │
        ▼                   ▼                   ▼
   ChatManager      FeedbackModeManager    CSV File Output


┌─────────────────────────────────────────────────────────────────────────────┐
│                      FILE ORGANIZATION                                       │
└─────────────────────────────────────────────────────────────────────────────┘

Assets/GeminiManager/
├── UnityAndGeminiV3.cs              [Modified - Enhanced LLM]
├── TextToSpeechManager.cs           [Modified - Filler generation]
├── SpeechToTextManager.cs           [Existing]
├── GoogleCloudSpeechToText.cs       [Existing]
├── FeedbackModeManager.cs           [NEW - Mode controller]
├── GestureController.cs             [NEW - Avatar gestures]
├── VisualCueController.cs           [NEW - UI indicators]
├── HTI_DataLogger.cs                [NEW - Research logging]
├── HTI_PROJECT_README.md            [NEW - Documentation]
├── HTI_QUICK_REFERENCE.cs           [NEW - Quick guide]
├── IMPLEMENTATION_SUMMARY.md        [NEW - Summary]
└── SYSTEM_ARCHITECTURE.md           [NEW - This file]

Assets/Scripts/
├── ChatManager.cs                   [Modified - Character name]
├── HTI_ExperimentController.cs      [NEW - Test runner UI]
└── TextCleanerUtility.cs            [Existing]


END OF SYSTEM ARCHITECTURE DIAGRAM

# HTI Project: Feedback Modality Experiment

## Research Question
**"When measured independently, which feedback modality (verbal filler, gesture, or visual cue) most effectively reduces perceived latency and enhances user immersion in conversations with an LLM-based 3D avatar in VR environment?"**

---

## Experiment Setup

### Character Configuration

**LLM Character: Alex Thompson**
- 20-year-old Computer Science junior at UC Berkeley
- From San Francisco, California
- Lives on-campus, works as teaching assistant
- Interested in AI/ML, robotics, hiking
- Natural, conversational personality with thinking pauses

**User Character (Participant)**
- College student from NI Punjab, India
- Engaging in cultural exchange conversation
- Learning about American college life through dialogue

### Conversation Topics
The LLM is designed to naturally discuss:
- Class schedules and academic workload
- Campus life and student activities
- Food culture (dining halls, food trucks, vs home cooking)
- Housing differences (dorms vs living at home)
- Social life and making friends
- Cost of education and part-time work
- Technology and coding culture
- Future career plans
- Cultural celebrations and festivals
- Weekend activities

---

## Three Feedback Modalities

### 1. **Verbal Filler Mode**
- Avatar uses verbal filler sounds: "um", "hmm", "uh", "let me think", "well"
- Generated using Google Text-to-Speech API in real-time
- Plays while LLM processes response
- Mimics human thinking patterns

**Implementation:** `TextToSpeechManager.cs`
- Automatically generates fillers from TTS if no pre-recorded clips available
- Random selection from filler phrase array
- Seamless transition to actual response

### 2. **Gesture Mode**
- Avatar performs thinking gestures/animations
- Examples: Head tilt, hand on chin, scratching head
- Both animation-based and procedural options supported
- Visual indication of processing without sound

**Implementation:** `GestureController.cs`
- Can trigger avatar animations if available
- Falls back to procedural gestures (head tilt)
- Configurable gesture duration

### 3. **Visual Cue Mode**
- On-screen visual indicator of thinking
- Options: Thought bubble, loading icon, "Thinking..." text
- Non-intrusive UI element
- Fades in/out smoothly

**Implementation:** `VisualCueController.cs`
- Animated thinking indicator (rotation, pulse)
- Random thinking messages
- Customizable fade animations

### 4. **Control Condition (None)**
- No feedback during processing
- Pure latency experience
- Baseline for comparison

---

## System Architecture

### Core Components

1. **FeedbackModeManager.cs**
   - Central controller for switching between modes
   - Ensures only one feedback type active at a time
   - Provides timing configuration
   - Singleton pattern for global access

2. **UnityAndGeminiV3.cs** (Enhanced)
   - Gemini LLM integration
   - Natural thinking delay simulation (0.5-1.5s)
   - Rich character background and personality
   - Conversational response style (2-3 sentences)
   - Integrates with feedback system

3. **TextToSpeechManager.cs** (Updated)
   - Google TTS integration
   - Dynamic filler generation
   - Mode-aware enabling/disabling
   - Avatar lip-sync support (Ready Player Me)

4. **ChatManager.cs** (Updated)
   - Updated character name to "Alex"
   - HTI experiment status display
   - Message history and UI management

5. **HTI_ExperimentController.cs** (New)
   - Test runner UI for mode selection
   - Dropdown menu for feedback modes
   - Keyboard shortcuts for quick switching
   - Experiment logging

### Supporting Components

- **GestureController.cs**: Avatar gesture management
- **VisualCueController.cs**: UI thinking indicators
- **TextCleanerUtility.cs**: Text processing for TTS
- **SpeechToTextManager.cs**: Voice input from user

---

## Setup Instructions

### 1. Scene Setup

1. Add **FeedbackModeManager** to scene:
   ```
   Create Empty GameObject → "FeedbackModeManager"
   Add Component: FeedbackModeManager.cs
   ```

2. Configure components in FeedbackModeManager:
   - Drag `TextToSpeechManager` reference
   - Drag `GestureController` reference (if using gestures)
   - Drag `VisualCueController` reference (if using visual cues)
   - Set feedback mode (Verbal Filler, Gesture, Visual Cue, None)

3. Link to **UnityAndGeminiV3**:
   - Select your Gemini Manager GameObject
   - Drag FeedbackModeManager to "Feedback Mode Manager" field

4. Add **HTI_ExperimentController** for UI:
   ```
   Create UI → Canvas → "HTI_ControlPanel"
   Add Component: HTI_ExperimentController.cs
   ```

5. Create UI Elements:
   - Dropdown for mode selection
   - Apply button
   - Status text display

### 2. Avatar Setup

For **Ready Player Me Avatar**:
1. Ensure avatar has `VoiceHandler` component
2. Link VoiceHandler to TextToSpeechManager
3. (Optional) Add Animator for gesture animations
4. Link head/hand transforms for procedural gestures

### 3. API Configuration

Update API keys in:
- **UnityAndGeminiV3.cs**: Gemini API key
- **TextToSpeech**: Google TTS API key
- **SpeechToTextManager**: Google STT API key

### 4. Testing Modes

**Keyboard Shortcuts (during runtime):**
- `1`: Verbal Filler Mode
- `2`: Gesture Mode
- `3`: Visual Cue Mode
- `4`: None (Control)
- `F1`: Toggle settings panel

**Inspector Testing:**
- Right-click FeedbackModeManager → Context Menu → Test modes

---

## Running the Experiment

### Pre-Experiment
1. Open HTI_ControlPanel (F1 if hidden)
2. Select desired feedback mode from dropdown
3. Click "Apply"
4. Brief participant on the conversation scenario
5. Hide control panel (F1) for immersion

### During Experiment
1. Participant uses VR controller trigger or Space bar to speak
2. Speech-to-Text captures user input
3. LLM processes with natural thinking delay
4. Appropriate feedback plays based on mode
5. LLM responds through avatar TTS
6. Conversation continues naturally

### Post-Experiment Data
- Check Unity console for mode switch timestamps
- Review conversation logs in ChatManager
- Collect user feedback on perceived latency
- Measure immersion ratings

---

## Conversation Flow Example

**User (Punjab, India):** "Hey! What's your typical day like at Berkeley?"

**[Processing - Feedback Plays Based on Mode]**
- Verbal: "Hmm..." (audio filler)
- Gesture: Head tilt + thinking pose
- Visual: "Thinking..." indicator appears
- None: Silent wait

**Alex (LLM):** "Oh interesting question! My days are pretty packed actually. I usually have classes in the morning, then I TA for an intro CS course in the afternoon. How about you - what's your schedule like in Punjab?"

---

## Natural Conversation Features

### LLM Behaviors
- **Conversational**: Casual college student language
- **Thoughtful**: Acknowledges complex questions
- **Curious**: Asks follow-up questions
- **Cultural**: Compares Berkeley vs Punjab experiences
- **Gradual**: Reveals background info naturally, not all at once

### Example Interactions
✅ "That's a good question, let me think... Um, I'd say the workload here is pretty intense"
✅ "Hmm, interesting! In India, do you live on campus or at home?"
✅ "Wait, so you don't have dining halls? How does food work there?"

❌ "I am Alex. I am 20 years old. I study Computer Science." (Too robotic)
❌ "The answer is..." (Too formal)

---

## Files Modified/Created

### New Files
- `Assets/GeminiManager/FeedbackModeManager.cs`
- `Assets/GeminiManager/GestureController.cs`
- `Assets/GeminiManager/VisualCueController.cs`
- `Assets/Scripts/HTI_ExperimentController.cs`
- `Assets/GeminiManager/HTI_PROJECT_README.md`

### Modified Files
- `Assets/GeminiManager/UnityAndGeminiV3.cs`
  - Enhanced system prompt (Alex character)
  - Natural thinking delays
  - Feedback system integration
  
- `Assets/GeminiManager/TextToSpeechManager.cs`
  - Dynamic filler generation from TTS
  - Mode-aware enable/disable
  - Filler phrase array

- `Assets/Scripts/ChatManager.cs`
  - Character name updated to "Alex"
  - HTI experiment status message

---

## Troubleshooting

### Fillers Not Playing
- Check `enableFillerSpeech` is true in TextToSpeechManager
- Ensure FeedbackModeManager is set to VerbalFiller mode
- Verify voice ScriptableObject is assigned
- Check Google TTS API key is valid

### Gestures Not Working
- Verify avatar has Animator component
- Check animation trigger names match
- Ensure head/hand transforms are assigned for procedural gestures
- Enable `useProceduralGesture` as fallback

### Visual Cues Not Showing
- Check `thinkingIndicatorPanel` GameObject is assigned
- Verify Canvas is in scene
- Ensure VisualCueController is enabled in FeedbackModeManager

### LLM Not Responding Naturally
- Review system prompt in UnityAndGeminiV3
- Check `addNaturalThinkingDelay` is enabled
- Verify Gemini API key is valid
- Review chat history in ChatManager

---

## Research Metrics to Track

### Quantitative
- Response time (with/without feedback)
- Conversation length (messages exchanged)
- User satisfaction ratings (post-experiment survey)
- Perceived latency ratings (1-5 scale)
- Immersion ratings (presence questionnaire)

### Qualitative
- User comments on naturalness
- Preference ranking of modalities
- Comfort level with avatar interaction
- Cultural exchange quality feedback

---

## Future Enhancements

- [ ] Add metrics logging system
- [ ] Implement automated experiment runner
- [ ] Add more gesture variations
- [ ] Support custom filler voice styles
- [ ] Multi-language support (Hindi/Punjabi)
- [ ] Eye-tracking integration for attention measurement
- [ ] Heart rate monitoring for immersion

---

## Contact & Credits

**Project:** HTI Research - Feedback Modality Study
**Platform:** Unity + VR (Meta Quest / HTC Vive)
**LLM:** Google Gemini 2.5 Flash
**TTS/STT:** Google Cloud APIs
**Avatar:** Ready Player Me

---

## Quick Start Checklist

- [ ] FeedbackModeManager added to scene
- [ ] All three controllers (TTS, Gesture, Visual) configured
- [ ] UnityAndGeminiV3 linked to FeedbackModeManager
- [ ] HTI_ExperimentController UI created
- [ ] API keys configured
- [ ] Avatar with VoiceHandler assigned
- [ ] Test all three modes in Editor
- [ ] Run pilot conversation test
- [ ] Document baseline metrics
- [ ] Ready for experiments!

---

**Good luck with your research! 🎓🔬**

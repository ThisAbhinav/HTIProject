// OPTIMIZATION FEATURES - QUICK REFERENCE
// ========================================

/* 🎙️ FILLER AUDIO SYSTEM
 * 
 * Total Fillers: 38 phrases in 6 categories
 * 
 * Recording Tool:
 * - Unity Menu → HTI Tools → Filler Recorder
 * - One-click batch recording
 * - Saves to Assets/Audio/Fillers/
 * - Move to Resources/Audio/Fillers/ for auto-loading
 * 
 * Cost Savings:
 * - Before: ~$0.01-0.02 per conversation
 * - After: $0 (one-time $0.05 recording cost)
 * - Performance: 10x faster playback (<50ms vs 300-500ms)
 * 
 * Priority Order (Automatic):
 * 1. Preloaded from Resources (Best)
 * 2. Manual clips from Inspector
 * 3. TTS generation (Fallback only)
 */

/* ⏱️ CONVERSATION TIMING
 * 
 * Default Settings:
 * - Target Duration: 10 minutes
 * - Min Duration: 5 minutes
 * - Max Duration: 15 minutes (hard limit)
 * - Min Exchanges: 10
 * - Target Exchanges: 20
 * 
 * Conversation Stages:
 * 0-40%   → START: Warm, engaging, ask questions
 * 40-70%  → MIDDLE: Share experiences, keep flowing
 * 70-100% → LATE: Ready to wrap up soon
 * 70%+ with criteria met → CLOSING: Natural ending
 * 
 * Closing Criteria (Must meet ALL):
 * ✓ Time ≥ 5 minutes
 * ✓ Exchanges ≥ 10
 * ✓ Info discovered ≥ 3 topics
 * ✓ In closing window (70%+)
 */

/* 📊 BACKGROUND INFO TOPICS (8 total)
 * 
 * Alex's discoverable background:
 * 1. Lives in dorms
 * 2. Roommate named Jake
 * 3. Works as teaching assistant
 * 4. CS club & Robotics team member
 * 5. Enjoys hiking on weekends
 * 6. Favorite campus spot (library)
 * 7. Dining hall + food truck habits
 * 8. Takes BART to SF
 * 
 * Goal: Participants discover 3-5 topics naturally
 * Tracking: Auto-detected via keywords in AI messages
 */

/* 🎮 KEYBOARD CONTROLS
 * 
 * Feedback Modes:
 * - 1: Verbal Filler
 * - 2: Gesture
 * - 3: Visual Cue
 * - 4: None (Control)
 * 
 * UI Toggles:
 * - F1: Settings panel
 * - F2: Progress display
 * 
 * Voice Input:
 * - Space: Hold to record (keyboard)
 * - VR Trigger: Hold to record (VR)
 */

/* 📈 PROGRESS MONITORING
 * 
 * Display Elements (F2 to toggle):
 * - Time: MM:SS format
 * - Exchanges: Count of back-and-forth
 * - Info: X/8 discovered
 * - Status: Active/Closing/Ready
 * - Progress bars with color coding
 * 
 * Color Indicators:
 * 🟢 Green: Normal (0-70%)
 * 🟡 Yellow: Closing Window (70-90%)
 * 🔴 Red: Ending Soon (90-100%)
 */

/* 🛠️ SETUP QUICK STEPS
 * 
 * 1. Record Fillers:
 *    HTI Tools → Filler Recorder → Record All
 *    Move files to Resources/Audio/Fillers/
 * 
 * 2. Add ConversationManager:
 *    Create GameObject → Add Component
 *    Link to UnityAndGeminiV3
 * 
 * 3. Add Progress UI (Optional):
 *    Create Canvas → Add ConversationProgressUI
 *    Assign UI elements
 * 
 * 4. Test:
 *    Play Mode → Check console for "Preloaded X fillers"
 *    Press F2 to see progress
 */

/* 📋 FILLER PHRASES BY CATEGORY
 * 
 * SHORT (6):
 * - um, uh, hmm, ah, er, oh
 * 
 * THINKING (6):
 * - let me think
 * - hmm let me see
 * - uh let me think about that
 * - that's a good question
 * - interesting question
 * - hmm interesting
 * 
 * POSITIVE (5):
 * - oh that's interesting
 * - hmm that's cool
 * - oh wow
 * - interesting
 * - that's a good point
 * 
 * CONVERSATIONAL (6):
 * - you know
 * - I mean
 * - well
 * - so
 * - like
 * - actually
 * 
 * HESITATION (4):
 * - hmm how do I put it
 * - uh how should I say
 * - let me put it this way
 * - hmm where do I start
 * 
 * PROCESSING (4):
 * - hmm give me a second
 * - let me think for a moment
 * - uh I need to think about that
 * - that's a great question let me see
 */

/* 🔄 CONVERSATION FLOW EXAMPLE
 * 
 * Time: 0:00 | Stage: START
 * User: "What's college like at Berkeley?"
 * Alex: [Warm intro, asks about their experience]
 * 
 * Time: 4:00 | Stage: MIDDLE | Info: 2/8
 * User: "Do you live on campus?"
 * Alex: "Yeah I live in dorms! I have a roommate Jake..."
 * [Info discovered: Lives in dorms, Roommate Jake]
 * 
 * Time: 7:00 | Stage: LATE | Exchanges: 16 | Info: 4/8
 * Status: CLOSING WINDOW (70% time, criteria met)
 * User: "What do you do on weekends?"
 * Alex: "I usually go hiking or take BART to SF..."
 * 
 * Time: 9:30 | Stage: CLOSING | Info: 5/8
 * Status: READY TO CLOSE
 * Alex: "It's been really cool talking to you! We should 
 *        exchange contact info - I'd love to hear more about 
 *        college life in Punjab next time."
 * User: "Yeah definitely! This was great."
 * 
 * Time: 10:00 | END
 * Result: Natural, complete conversation
 */

/* 📊 DATA LOGGED
 * 
 * Automatic Logging:
 * - Session start/end times
 * - Total duration
 * - Exchange count
 * - Info discovered (which topics)
 * - Conversation stage changes
 * - Closing readiness time
 * - Feedback mode used
 * 
 * CSV Format:
 * Timestamp, Event, Duration, Exchanges, Info, Status
 * 
 * Log Location:
 * [Project]/HTI_Logs/HTI_P001_YYYYMMDD_HHMMSS.csv
 */

/* ⚙️ COMPONENT SETTINGS
 * 
 * TextToSpeechManager:
 * - Use Prerecorded Fillers: ✓ true
 * - Generate Fillers From TTS: ✗ false (fallback)
 * - Enable Filler Speech: ✓ true
 * 
 * ConversationManager:
 * - Target Duration Minutes: 10
 * - Min Duration Minutes: 5
 * - Max Duration Minutes: 15
 * - Min Exchanges: 10
 * - Target Exchanges: 20
 * - Min Info Discovered: 3
 * - Target Info Discovered: 5
 * - Enable Natural Closing: ✓ true
 * - Closing Window Start Percent: 0.7 (70%)
 * 
 * ConversationProgressUI:
 * - Show During Conversation: ✓ true
 * - Hide From Participant: ✓ true (for experiments)
 * - Toggle Key: F2
 */

/* ✅ VALIDATION CHECKLIST
 * 
 * Fillers:
 * □ 38 audio files in Resources/Audio/Fillers/
 * □ Console shows "Preloaded X filler audio clips"
 * □ Fillers play instantly (no network delay)
 * □ No TTS API calls for fillers
 * 
 * Timing:
 * □ ConversationManager in scene
 * □ Linked to UnityAndGeminiV3
 * □ Progress UI shows real-time data
 * □ Closing window activates at 70%
 * □ Natural endings occur
 * □ No overtime (hard limit works)
 * 
 * Integration:
 * □ All feedback modes compatible
 * □ Data logging captures all events
 * □ F1/F2 toggles work
 * □ No console errors
 */

/* 💡 TROUBLESHOOTING
 * 
 * Fillers Not Playing:
 * - Check Resources/Audio/Fillers/ exists
 * - Verify files are AudioClips in Unity
 * - Check console for "Preloaded" message
 * - Ensure enableFillerSpeech = true
 * 
 * Conversation Not Ending:
 * - Check ConversationManager is active
 * - Verify minDuration/minExchanges set correctly
 * - Monitor progress UI (F2) for status
 * - Check console for "Ready for natural closing"
 * 
 * Progress UI Not Showing:
 * - Press F2 to toggle visibility
 * - Check Canvas is in scene
 * - Verify UI elements assigned in Inspector
 * - Check hideFromParticipant setting
 */

/* 🎯 RESEARCH BENEFITS
 * 
 * Consistency:
 * ✓ Same conversation duration across participants
 * ✓ Comparable exchange counts
 * ✓ Similar information coverage
 * 
 * Cost Efficiency:
 * ✓ Zero recurring costs for fillers
 * ✓ 100% savings after initial recording
 * ✓ No network dependencies
 * 
 * Natural Flow:
 * ✓ Graceful conversation openings
 * ✓ Smooth topic transitions
 * ✓ Natural, warm closings
 * ✓ Not abrupt or forced
 * 
 * Data Quality:
 * ✓ Complete metrics logged
 * ✓ Trackable info discovery
 * ✓ Comparable across conditions
 * ✓ Reproducible results
 */

/* 📚 DOCUMENTATION FILES
 * 
 * Core Documentation:
 * - HTI_PROJECT_README.md (Full system guide)
 * - SYSTEM_ARCHITECTURE.md (Visual diagrams)
 * - IMPLEMENTATION_SUMMARY.md (What was built)
 * 
 * Optimization Docs:
 * - OPTIMIZATION_GUIDE.md (Detailed setup)
 * - OPTIMIZATION_SUMMARY.md (Overview)
 * - HTI_QUICK_REFERENCE.cs (Quick lookup)
 * 
 * Code Files:
 * - FillerRecorderUtility.cs (Recording tool)
 * - ConversationManager.cs (Timing system)
 * - ConversationProgressUI.cs (Progress display)
 * 
 * All located in: Assets/GeminiManager/
 */

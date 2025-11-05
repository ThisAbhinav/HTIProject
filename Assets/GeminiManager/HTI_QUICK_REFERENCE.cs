// HTI EXPERIMENT - QUICK REFERENCE GUIDE
// =====================================

/* CHARACTER BACKGROUND - Alex Thompson (LLM)
 * 
 * Basic Info:
 * - 20 years old, UC Berkeley junior
 * - Computer Science major (AI/ML focus)
 * - From San Francisco, California
 * 
 * Background to Discover Through Conversation:
 * - Lives in dorms (roommate: Jake, mechanical engineering)
 * - Works as CS teaching assistant
 * - Member of Computer Science club and Robotics team
 * - Enjoys hiking on weekends
 * - Favorite campus spot: Library overlooking the bay
 * - Eats at dining hall + food trucks on Telegraph Ave
 * - Takes BART to SF on weekends
 * 
 * Conversation Style:
 * - Casual, friendly, thoughtful
 * - Uses filler words when thinking ("um", "hmm", "you know")
 * - Genuinely curious about Indian college life
 * - Asks follow-up questions
 * - Shares info gradually, not all at once
 * - Compares Berkeley experiences with user's Punjab experience
 */

/* USER CHARACTER (Participant)
 * 
 * - College student from NI Punjab, India
 * - Learning about American college life
 * - Cultural exchange conversation
 * - Should discover Alex's background through natural dialogue
 */

/* FEEDBACK MODES (Select one per experiment session)
 * 
 * 1. VERBAL FILLER
 *    - Audio: "um", "hmm", "uh", "let me think", "well"
 *    - Generated via Google TTS in real-time
 *    - Mimics human thinking sounds
 * 
 * 2. GESTURE
 *    - Visual: Head tilt, hand on chin, thinking pose
 *    - Avatar animations or procedural movements
 *    - Silent visual feedback
 * 
 * 3. VISUAL CUE
 *    - UI: Thought bubble, loading icon, "Thinking..." text
 *    - On-screen indicator
 *    - Non-intrusive display
 * 
 * 4. NONE (Control)
 *    - No feedback
 *    - Pure latency experience
 *    - Baseline comparison
 */

/* KEYBOARD SHORTCUTS (Test Runners)
 * 
 * Mode Selection:
 * - Press 1: Verbal Filler Mode
 * - Press 2: Gesture Mode
 * - Press 3: Visual Cue Mode
 * - Press 4: None (Control)
 * 
 * Controls:
 * - Press F1: Toggle settings panel
 * - Hold Space: Record voice input (user)
 * - VR Trigger: Record voice input (VR mode)
 */

/* CONVERSATION TOPICS (Natural Flow)
 * 
 * Academic:
 * - Class schedules and workload
 * - Major differences (CS vs others)
 * - Teaching assistant experience
 * - Study habits and group projects
 * 
 * Campus Life:
 * - Dorm living vs living at home
 * - Dining halls vs home-cooked food
 * - Campus facilities (library, gym, clubs)
 * - Making friends and social activities
 * 
 * Cultural:
 * - Food culture differences
 * - Weekend activities (hiking vs ?)
 * - Festivals and celebrations
 * - Cost of education and part-time work
 * 
 * Career:
 * - Future plans after graduation
 * - Internships and job hunting
 * - Technology/coding culture
 * - Startup scene in SF/Bay Area
 */

/* NATURAL CONVERSATION EXAMPLES
 * 
 * ✅ GOOD (Natural & Engaging):
 * "Oh that's interesting! Hmm, let me think... so you don't have dorms? 
 *  Do you commute to college then?"
 * 
 * "Wait really? That's so different from here! Um, how do you manage 
 *  with such long commutes?"
 * 
 * "Haha yeah, dining hall food gets old fast. What kind of food do 
 *  you usually have at home?"
 * 
 * ❌ BAD (Too Robotic):
 * "I am a student at UC Berkeley. I study Computer Science."
 * "The answer to your question is that I live in a dormitory."
 * "Thank you for asking. Here is my complete background information..."
 */

/* TIMING CONFIGURATION
 * 
 * Natural Thinking Delay:
 * - Min: 0.5 seconds
 * - Max: 1.5 seconds
 * - Randomized per response for realism
 * 
 * Filler Delay:
 * - 0.1 seconds before filler plays
 * - Allows natural gap
 * 
 * Response Length:
 * - Target: 2-3 sentences
 * - Max: 30 words (enforced by brevity system)
 * - Natural conversational chunks
 */

/* SYSTEM PROMPTS KEY POINTS
 * 
 * 1. BE CONVERSATIONAL - Talk like a college student
 * 2. THINK NATURALLY - Use filler words, pause to think
 * 3. ASK BACK - Show genuine interest in their experience
 * 4. SHARE GRADUALLY - Don't info dump, reveal naturally
 * 5. BE CURIOUS - Ask about Punjab and Indian college life
 * 6. RELATE & COMPARE - Connect Berkeley to their experience
 */

/* EXPERIMENT METRICS TO TRACK
 * 
 * Quantitative:
 * - Response latency (perceived vs actual)
 * - Number of conversational turns
 * - Duration of conversation
 * - User satisfaction score (1-5)
 * - Immersion rating (1-5)
 * 
 * Qualitative:
 * - Preferred feedback modality
 * - Comments on naturalness
 * - Cultural exchange quality
 * - Comfort with avatar interaction
 */

/* SETUP CHECKLIST
 * 
 * Before Each Session:
 * □ Select feedback mode (1, 2, 3, or 4)
 * □ Verify API keys are working
 * □ Test microphone/VR controller input
 * □ Check avatar TTS is functioning
 * □ Brief participant on scenario
 * □ Start recording/logging (if applicable)
 * 
 * After Each Session:
 * □ Save conversation logs
 * □ Collect user feedback
 * □ Document mode used
 * □ Note any technical issues
 * □ Reset for next participant
 */

/* TROUBLESHOOTING QUICK FIXES
 * 
 * No Fillers Playing:
 * - Check: TextToSpeechManager.enableFillerSpeech = true
 * - Check: FeedbackModeManager mode = VerbalFiller
 * - Check: Google TTS API key valid
 * 
 * No Gestures:
 * - Check: GestureController.gesturesEnabled = true
 * - Check: Avatar Animator assigned
 * - Check: Head/hand transforms linked
 * 
 * No Visual Cues:
 * - Check: VisualCueController.visualCuesEnabled = true
 * - Check: UI Panel assigned and active
 * - Check: Canvas exists in scene
 * 
 * LLM Not Natural:
 * - Check: addNaturalThinkingDelay = true
 * - Review: System prompt in UnityAndGeminiV3
 * - Verify: Gemini API key and model (gemini-2.5-flash)
 */

/* API ENDPOINTS & MODELS
 * 
 * Gemini LLM:
 * - Model: gemini-2.5-flash
 * - Endpoint: generativelanguage.googleapis.com/v1beta
 * 
 * Google TTS:
 * - API: Cloud Text-to-Speech
 * - Voice: Configure in VoiceScriptableObject
 * 
 * Google STT:
 * - API: Cloud Speech-to-Text
 * - Language: en-US
 * - Encoding: LINEAR16
 */

/* RESEARCH HYPOTHESIS
 * 
 * H1: Verbal fillers will most effectively reduce perceived latency
 *     due to mimicking natural human conversation patterns
 * 
 * H2: Gesture feedback will enhance immersion through embodied
 *     avatar behavior without audio distraction
 * 
 * H3: Visual cues provide clear feedback but may reduce immersion
 *     by drawing attention away from avatar
 * 
 * H0: No significant difference between modalities (control needed)
 */

# 4-Session Counterbalanced Design Summary

## Overview
The system now properly tracks questions across exactly 4 sessions per participant, with counterbalanced feedback conditions based on a Latin Square design.

## Counterbalanced Design

### Feedback Types
1. **Baseline** - No feedback
2. **Gestures** - Avatar thinking animation
3. **Visual** - Loading icon with text
4. **Verbal** - Audio fillers ("um", "hmm")

### Session Order by Participant
Based on Latin Square counterbalancing:

| Participant | Session 1 | Session 2 | Session 3 | Session 4 |
|-------------|-----------|-----------|-----------|-----------|
| P01, P05, P09, P13, P17 | Baseline | Gestures | Visual | Verbal |
| P02, P06, P10, P14, P18 | Gestures | Visual | Verbal | Baseline |
| P03, P07, P11, P15, P19 | Visual | Verbal | Baseline | Gestures |
| P04, P08, P12, P16, P20 | Verbal | Baseline | Gestures | Visual |

## Question Tracking Across All 4 Sessions

### Example: P01's Full Study Progression

**Session 1 (Baseline):**
- Available questions: All 20
- Selected: [3, 7, 12, 15]
- Saved to: `P01_Session1_Config.json`
- Feedback: Baseline (none)

**Session 2 (Gestures):**
- Available questions: 16 (excludes 3, 7, 12, 15)
- Selected: [1, 8, 14, 19]
- Saved to: `P01_Session2_Config.json`
- Feedback: Gestures (thinking animation)

**Session 3 (Visual):**
- Available questions: 12 (excludes 3, 7, 12, 15, 1, 8, 14, 19)
- Selected: [0, 5, 9, 17]
- Saved to: `P01_Session3_Config.json`
- Feedback: Visual (loading icon + text)

**Session 4 (Verbal):**
- Available questions: 8 (excludes previous 12)
- Selected: [2, 6, 10, 13]
- Saved to: `P01_Session4_Config.json`
- Feedback: Verbal (audio fillers)

**Result:** P01 has now used 16/20 questions across 4 sessions with no repetition.

### Example: P03's Full Study Progression

**Session 1 (Visual):**
- Questions: [4, 11, 16, 18]
- Feedback: Visual

**Session 2 (Verbal):**
- Questions: [0, 2, 8, 13] (excludes 4, 11, 16, 18)
- Feedback: Verbal

**Session 3 (Baseline):**
- Questions: [1, 7, 10, 15] (excludes previous 8)
- Feedback: Baseline

**Session 4 (Gestures):**
- Questions: [3, 6, 9, 14] (excludes previous 12)
- Feedback: Gestures

## Key Features

### Automatic Question Exclusion
- System automatically reads all previous session configs for a participant
- Builds list of used question indices
- Only selects from unused questions
- **Guaranteed no repetition across all 4 sessions**

### Session Config Files
Each saved config contains:
```json
{
  "participantId": "P01",
  "sessionNumber": 3,
  "questionIndices": [0, 5, 9, 17],
  "feedbackType": "Visual",
  "timestamp": "2025-11-29 14:30:00"
}
```

### Replay Protection
If Session 2 crashes and needs replay:
- System loads exact questions from `P01_Session2_Config.json`
- Sessions 1, 3, and 4 are unaffected
- No questions repeat

## Maximum Sessions
- Each participant completes exactly **4 sessions**
- Each session uses **4 unique questions**
- Total: **16 unique questions per participant** (out of 20 available)
- **4 questions remain unused** per participant

## Validation

### Participant IDs
- Valid: P01 through P20
- Case-insensitive (p01 = P01)
- Invalid IDs default to Baseline feedback

### Session Numbers
- Valid: 1, 2, 3, 4
- Must be set before starting game
- Feedback type automatically determined

## Viewing Progress

### In Editor Window
**HTI Experiment → Session Config Manager** shows:
- Current participant's completed sessions (X/4)
- Questions used (X/20)
- Questions available (X/20)
- List of all used question indices
- Which sessions are completed

### In Console Logs
```
[SessionConfig] Set to P01 - Session 3 (Visual)
[TaskManager] Available questions for P01: 12/20
[TaskManager] Previously used questions: [3, 7, 12, 15, 1, 8, 14, 19]
[TaskManager] New random questions selected: [0, 5, 9, 17]
```

## Benefits

1. **Counterbalancing**: Each feedback type appears equally in each session position
2. **No Question Repetition**: Guaranteed unique questions across all 4 sessions
3. **Crash Recovery**: Can replay any session without affecting others
4. **Automatic Tracking**: No manual tracking needed
5. **Validation**: Built-in checks for participant IDs and session numbers
6. **Transparency**: Full logging of question selection and exclusions

# Session Configuration System

## Overview
This system allows you to save and replay participant sessions with the exact same questions, preventing data loss if a session crashes or needs to be repeated.

## How It Works

### Automatic Saving & Cross-Session Tracking
When a session starts:
1. The system checks if questions were previously saved for this participant/session combination
2. If saved questions exist, they are loaded
3. If no saved questions exist:
   - System checks which questions were used in OTHER sessions by this participant
   - Excludes those questions from selection
   - Randomly selects 4 questions from the remaining available pool
   - Automatically saves the selection

### No Question Repetition Across Sessions
Each participant will never see the same question twice across all their sessions:
- P01 Session 1 uses questions [3, 7, 12, 15]
- P01 Session 2 will randomly select from [0, 1, 2, 4, 5, 6, 8, 9, 10, 11, 13, 14, 16, 17, 18, 19]
- P01 Session 3 will exclude questions from both Session 1 and Session 2
- And so on...

### Configuration Files
- Saved to: `Assets/ExperimentLogs/SessionConfigs/`
- Format: `P01_Session1_Config.json`
- Contains: Question indices from the master pool (0-19)

## Usage Methods

### Method 1: In-Game UI (Recommended for Experiments)
1. Add `SessionSetupUI` component to your scene
2. Before starting the experiment, enter:
   - Participant ID (e.g., P01)
   - Session Number (1-4)
3. Click "Start"
4. Questions are automatically saved/loaded

### Method 2: Editor Window (Recommended for Testing)
1. In Unity, go to: **HTI Experiment → Session Config Manager**
2. Set Participant ID and Session Number
3. Click "Set Session"
4. Start the game normally
5. View/delete saved configurations from this window

### Method 3: Code/Inspector
```csharp
// In your script's Start or before starting the game:
SessionConfiguration.Instance.SetSession("P01", 3);
```

## Example Scenarios

### Scenario 1: First Time Running P01 Session 1
```
Used questions: None
Available questions: All 20
Result: Questions [3, 7, 12, 15] are randomly selected and saved
```

### Scenario 2: P01 Session 2 (New Session)
```
Used questions: [3, 7, 12, 15] from Session 1
Available questions: 16 remaining (excludes 3, 7, 12, 15)
Result: New questions [1, 8, 14, 19] are selected from available pool
```

### Scenario 3: Replaying P01 Session 1 (Crashed/Retry)
```
Result: Same questions [3, 7, 12, 15] are loaded from saved config
Note: Session 2 questions are NOT affected
```

### Scenario 4: P01 Session 3
```
Used questions: [3, 7, 12, 15, 1, 8, 14, 19] from Sessions 1 & 2
Available questions: 12 remaining
Result: New questions [0, 5, 9, 17] are selected from available pool
```

### Scenario 5: P01 Session 4
```
Used questions: 12 indices from previous 3 sessions
Available questions: 8 remaining
Result: New questions [2, 6, 10, 13] are selected
```

### Scenario 6: P01 Session 5
```
Used questions: 16 indices from previous 4 sessions
Available questions: 4 remaining
Result: Final 4 questions [4, 11, 16, 18] are selected
All 20 questions now used - participant cannot do more sessions
```

## Managing Configurations

### View All Saved Configs
Use the Editor Window: `HTI Experiment → Session Config Manager`

### Delete a Configuration
To force new random questions for a participant/session:
1. Open Session Config Manager
2. Find the configuration in the list
3. Click "Delete"
4. Next time that session runs, new random questions will be generated

### Manual File Management
Navigate to `Assets/ExperimentLogs/SessionConfigs/` and delete specific JSON files

## Question Pool Reference
The system uses 20 questions (indices 0-19):
- 0: Alex's major
- 1: Dorm
- 2: Favorite hobby
- 3: Best coffee spot
- 4: Hometown
- 5: Year in college
- 6: Favorite food
- 7: Club membership
- 8: Pet
- 9: Favorite library
- 10: Transportation method
- 11: Favorite movie
- 12: Music taste
- 13: Siblings
- 14: Favorite sport
- 15: Summer plans
- 16: Favorite drink (non-coffee)
- 17: Favorite professor
- 18: Current game
- 19: Campus landmark

## Technical Details

### Files Created
- `SessionConfiguration.cs` - Core configuration manager
- `SessionSetupUI.cs` - UI for setting participant/session
- `Editor/SessionConfigManager.cs` - Editor window for management

### Integration
- Modified `TaskManager.cs` to check for saved configs before generating random questions
- Uses singleton pattern for easy access throughout the project
- Persists across scene loads with `DontDestroyOnLoad`

## Troubleshooting

**Q: Questions are changing even though I set the participant/session**
A: Make sure you call `SetSession()` BEFORE starting the game/conversation

**Q: A participant got the same question in different sessions**
A: This shouldn't happen. Check that:
   - Participant ID is set correctly and consistently (case-insensitive)
   - Session configs are being saved properly in `Assets/ExperimentLogs/SessionConfigs/`

**Q: Where are the config files stored?**
A: `Assets/ExperimentLogs/SessionConfigs/`

**Q: Can I manually edit the question indices?**
A: Yes, edit the JSON file directly. Use indices 0-19 corresponding to the master question pool

**Q: How many sessions can one participant do?**
A: Maximum 5 sessions (20 questions ÷ 4 per session)

**Q: What happens if a participant has done 5 sessions already?**
A: The system will show a warning that only 0 questions are available. Delete some session configs if you need to redo sessions.

**Q: How do I reset everything for a participant?**
A: Delete all their config files in `Assets/ExperimentLogs/SessionConfigs/` (e.g., all P01_Session*_Config.json files)

**Q: How do I reset everything for all participants?**
A: Delete all files in `Assets/ExperimentLogs/SessionConfigs/`

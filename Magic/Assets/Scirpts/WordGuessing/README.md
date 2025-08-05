# VR English Speaking Game - Word Guessing Challenge

## Overview
This is a complete implementation of a multiplayer cooperative word-guessing VR game where players help each other guess English words through voice interaction. The system integrates with your existing STT/TTS systems and provides a modular, extensible framework.

## 🎮 Game Features
- **Multiplayer Cooperative Gameplay**: 2 players work together to guess words
- **Voice-Driven Interaction**: Uses STT for questions and guesses
- **Role-Based System**: Guesser and Describer roles that switch each round
- **VR/XR Support**: Full integration with Unity XR Interaction Toolkit
- **Cross-Room Communication**: Players in separate rooms communicate via magic books
- **Visual Feedback**: Room lighting changes based on feedback
- **Progress Tracking**: Score system and stage progression
- **Modular Design**: Easy to integrate with existing systems

## 📁 File Structure

### Core Systems
- `WordGuessingGameManager.cs` - Main game state management
- `PlayerRoleManager.cs` - Player role tracking and permissions
- `WordDatabase.cs` - Word storage and difficulty management
- `AnswerValidator.cs` - Advanced answer validation with typo tolerance

### Communication Systems
- `WordGuessingVoiceHandler.cs` - Voice recognition integration
- `MagicBookTextDisplay.cs` - Text display for magic books
- `PlayerMessageRelay.cs` - Cross-player message system

### Interaction Systems
- `MagicBookInteraction.cs` - Magic book hand interaction
- `FeedbackStoneInteraction.cs` - Blue/Red stone feedback system
- `PictureFrameDisplay.cs` - Word display for describers
- `RoomFeedbackController.cs` - Room lighting effects

### UI Systems
- `WordGuessingUIManager.cs` - Game UI and notifications
- `WordGuessingGameSetup.cs` - Automated setup and integration

## 🚀 Quick Setup

### 1. Automatic Setup (Recommended)
1. Add `WordGuessingGameSetup.cs` to an empty GameObject
2. Check "Auto Setup On Start" in the inspector
3. Run the scene - everything will be configured automatically

### 2. Manual Setup
1. Create the core system GameObjects:
   ```
   - WordGuessingGameManager (with WordDatabase, AnswerValidator)
   - PlayerRoleManager
   - WordGuessingVoiceHandler
   - PlayerMessageRelay
   - WordGuessingUIManager
   ```

2. Set up Room 1 (Guesser's Room):
   ```
   - MagicBook GameObject with MagicBookInteraction
   - TextDisplay GameObject with MagicBookTextDisplay
   - RoomFeedbackController for lighting
   ```

3. Set up Room 2 (Describer's Room):
   ```
   - BlueStone GameObject with FeedbackStoneInteraction (type: Blue)
   - RedStone GameObject with FeedbackStoneInteraction (type: Red)
   - PictureFrame GameObject with PictureFrameDisplay
   - RoomFeedbackController for lighting
   ```

## 🎯 Game Flow

### Phase 1: Round Start
- System selects a random word
- Word appears in describer's picture frame
- Players are assigned roles (alternating each round)

### Phase 2: Question Phase
- Guesser places hand on magic book to activate voice
- Guesser asks yes/no questions in English
- Questions appear on both players' magic books

### Phase 3: Feedback Phase
- Describer uses blue stone (YES) or red stone (NO)
- Room lighting changes to indicate feedback
- System returns to question phase for more questions

### Phase 4: Guess Phase
- When guesser speaks only the target word, system validates
- Correct guess advances to next round with role switch
- Incorrect guess returns to question phase

### Phase 5: Stage Complete
- After both players guess 2 words each (4 total), stage completes
- Players advance to next difficulty level

## 🔧 Integration with Existing Systems

### Voice Recognition Integration
```csharp
// Connect your existing VoiceRecognitionManager
WordGuessingVoiceHandler.Instance.SetVoiceRecognitionManager(yourVoiceManager);

// Or use the setup script
gameSetup.SetVoiceRecognitionManager(yourVoiceManager);
```

### Player System Integration
```csharp
// Set player positions for room assignment
gameSetup.SetPlayerPosition(1, room1Position);
gameSetup.SetPlayerPosition(2, room2Position);

// Connect to your player controllers
PlayerRoleManager.Instance.ConnectPlayer(1, "Player Name");
```

### Network Integration
The system is designed to work with your existing Photon networking:
```csharp
// Relay voice messages across network
PlayerMessageRelay.Instance.RelayVoiceMessage(playerId, message);

// Sync game state
WordGuessingGameManager.Instance.GetGameState(); // Get current state for sync
```

## 🎨 Customization

### Word Database
```csharp
// Add custom words
WordDatabase wordDB = FindObjectOfType<WordDatabase>();
wordDB.AddCustomWord("example", DifficultyLevel.Medium, "category", "hint");

// Set difficulty and category filters
wordDB.SetDifficulty(DifficultyLevel.Hard);
wordDB.SetCategoryFilter("animals");
```

### Visual Customization
```csharp
// Customize room feedback colors
RoomFeedbackController roomFeedback = GetComponent<RoomFeedbackController>();
roomFeedback.SetFeedbackColors(Color.blue, Color.red, Color.white);

// Customize magic book text appearance
MagicBookTextDisplay textDisplay = GetComponent<MagicBookTextDisplay>();
textDisplay.SetTypewriterSpeed(0.03f);
```

### Voice Settings
```csharp
// Adjust voice recognition settings
WordGuessingVoiceHandler.Instance.SetMinimumConfidence(0.8f);
WordGuessingVoiceHandler.Instance.SetVoiceTimeout(15f);
```

## 🏷️ Required Tags
Make sure your GameObjects have these tags for auto-discovery:
- `Room1MagicBook`
- `Room1TextDisplay`
- `Room1Feedback`
- `Room2BlueStone`
- `Room2RedStone`
- `Room2PictureFrame`
- `Room2Feedback`

## 📋 Dependencies
- Unity XR Interaction Toolkit
- TextMeshPro
- Your existing VoiceRecognitionManager
- Unity UI System

## 🐛 Debug Features
- Comprehensive debug logging (enable in inspector)
- Visual gizmos for interaction zones
- Debug UI panel showing game state
- Simulation mode for testing without VR

## 🔄 Event System
The system uses a comprehensive event system for loose coupling:

```csharp
// Game Events
WordGuessingGameManager.OnGamePhaseChanged += HandlePhaseChange;
WordGuessingGameManager.OnNewWordSelected += HandleNewWord;
WordGuessingGameManager.OnStageComplete += HandleStageComplete;

// Player Events
PlayerRoleManager.OnPlayerRoleChanged += HandleRoleChange;
PlayerRoleManager.OnPlayerPermissionsChanged += HandlePermissions;

// Voice Events
WordGuessingVoiceHandler.OnQuestionAsked += HandleQuestion;
WordGuessingVoiceHandler.OnGuessAttempted += HandleGuess;
```

## ⚡ Performance Considerations
- Object pooling for UI notifications
- Efficient text animation system
- Minimal per-frame updates
- Smart event subscription management

## 🚀 Getting Started
1. Import all scripts into `Assets/Scripts/WordGuessing/`
2. Add `WordGuessingGameSetup` to a GameObject
3. Configure the setup component in inspector
4. Hit play and call `StartGame()` or use the context menu
5. Test with keyboard/mouse or VR controllers

## 📞 Support
The system is designed to be self-contained and integrate seamlessly with your existing Magic VR project. All components are documented and include comprehensive debug output.

For advanced customization, modify the configuration in `WordGuessingGameSetup` or extend the individual component classes.
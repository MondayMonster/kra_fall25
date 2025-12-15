# Game State Management System - Setup Guide

## 🎮 Overview
Centralized system to manage game flow, UI states, GameObject activation, and interaction systems.

**Flow:** Welcome UI → Tutorial UI → Gameplay → (Paused/GameOver)

---

## 📦 Components Created

### 1. **GameStateManager.cs**
- Singleton pattern - accessible globally
- Manages game states (Welcome, Tutorial, Gameplay, Paused, GameOver)
- Controls UI visibility per state
- Controls GameObject activation per state
- Controls interaction systems per state

### 2. **WelcomeUIController.cs**
- Handles Welcome UI buttons (Play, Settings, Quit)
- Transitions to Tutorial state when Play clicked

### 3. **TutorialUIController.cs**
- Multi-step tutorial system
- Next/Previous/Skip buttons
- Transitions to Gameplay when complete

---

## 🛠️ Scene Setup

### Step 1: Create GameStateManager GameObject

1. **Create Empty GameObject:**
   - Hierarchy → Right-click → Create Empty
   - Name: `GameStateManager`

2. **Add Component:**
   - Add Component → `GameStateManager` script

3. **Configure Settings:**
   - Log State Changes: ✓ (for debugging)

---

### Step 2: Configure State: WELCOME

1. **In GameStateManager Inspector:**
   - State Configurations → Size: 5 (Welcome, Tutorial, Gameplay, Paused, GameOver)

2. **Element 0 - Welcome State:**
   ```
   State: Welcome
   
   UI To Enable:
   └─ WelcomeUI (Canvas)
   
   UI To Disable:
   └─ TutorialUI (Canvas)
   └─ (Any other UIs)
   
   Objects To Enable:
   └─ (Leave empty - nothing needed at welcome)
   
   Objects To Disable:
   └─ DetectionManager (disable detection)
   └─ WordDrawingManager (disable drawing)
   └─ (Any gameplay objects)
   
   Interaction Settings:
   ├─ Enable Stylus Ray: ✓ (TRUE - needed for UI buttons)
   ├─ Enable Detection: ✗ (FALSE)
   └─ Enable Drawing: ✗ (FALSE)
   ```

---

### Step 3: Configure State: TUTORIAL

1. **Element 1 - Tutorial State:**
   ```
   State: Tutorial
   
   UI To Enable:
   └─ TutorialUI (Canvas)
   
   UI To Disable:
   └─ WelcomeUI (Canvas)
   └─ (Any gameplay UIs)
   
   Objects To Enable:
   └─ (Tutorial-specific objects if any)
   
   Objects To Disable:
   └─ DetectionManager
   └─ WordDrawingManager
   └─ (Gameplay objects)
   
   Interaction Settings:
   ├─ Enable Stylus Ray: ✓ (TRUE - for tutorial UI)
   ├─ Enable Detection: ✗ (FALSE)
   └─ Enable Drawing: ✗ (FALSE)
   ```

---

### Step 4: Configure State: GAMEPLAY

1. **Element 2 - Gameplay State:**
   ```
   State: Gameplay
   
   UI To Enable:
   └─ (Gameplay HUD if any)
   
   UI To Disable:
   └─ WelcomeUI (Canvas)
   └─ TutorialUI (Canvas)
   
   Objects To Enable:
   └─ DetectionManager
   └─ WordDrawingManager
   └─ SentisInferenceManager
   └─ (All gameplay systems)
   
   Objects To Disable:
   └─ (Nothing - everything needed for gameplay)
   
   Interaction Settings:
   ├─ Enable Stylus Ray: ✓ (TRUE - for markers and UIs)
   ├─ Enable Detection: ✓ (TRUE - spawn markers)
   └─ Enable Drawing: ✓ (TRUE - when Writer UI active)
   ```

---

### Step 5: Configure State: PAUSED

1. **Element 3 - Paused State:**
   ```
   State: Paused
   
   UI To Enable:
   └─ PauseMenuUI (Canvas) - if you have one
   
   UI To Disable:
   └─ (Gameplay UIs)
   
   Objects To Enable:
   └─ (None)
   
   Objects To Disable:
   └─ (None - keep objects active but disable interactions)
   
   Interaction Settings:
   ├─ Enable Stylus Ray: ✓ (TRUE - for pause menu)
   ├─ Enable Detection: ✗ (FALSE - pause detection)
   └─ Enable Drawing: ✗ (FALSE - pause drawing)
   ```

---

### Step 6: Configure State: GAMEOVER

1. **Element 4 - GameOver State:**
   ```
   State: GameOver
   
   UI To Enable:
   └─ GameOverUI (Canvas) - if you have one
   
   UI To Disable:
   └─ (All gameplay UIs)
   
   Objects To Enable:
   └─ (None)
   
   Objects To Disable:
   └─ DetectionManager
   └─ WordDrawingManager
   
   Interaction Settings:
   ├─ Enable Stylus Ray: ✓ (TRUE - for restart/quit buttons)
   ├─ Enable Detection: ✗ (FALSE)
   └─ Enable Drawing: ✗ (FALSE)
   ```

---

### Step 7: Assign System References

In GameStateManager Inspector:

```
System References:
├─ Stylus Handler: [Drag MXInkStylusHandler GameObject]
├─ Detection Manager: [Drag DetectionManager GameObject]
└─ Drawing Manager: [Drag WordDrawingManager GameObject]
```

*(Or leave empty - auto-finds on Start)*

---

### Step 8: Setup Welcome UI

1. **Select WelcomeUI Canvas**
2. **Add Component:** `WelcomeUIController` script
3. **Assign References:**
   ```
   Play Button: [Drag Play Button]
   Settings Button: [Drag Settings Button]
   Quit Button: [Drag Quit Button (optional)]
   ```

---

### Step 9: Setup Tutorial UI

1. **Select TutorialUI Canvas**
2. **Add Component:** `TutorialUIController` script
3. **Create Tutorial Steps:**
   - Create child GameObjects for each tutorial step (e.g., Step1Panel, Step2Panel, etc.)
   - Each step should have text/images explaining controls

4. **Assign References:**
   ```
   Tutorial Steps:
   ├─ Size: [Number of steps, e.g., 5]
   ├─ Element 0: Step1Panel
   ├─ Element 1: Step2Panel
   ├─ Element 2: Step3Panel
   ├─ Element 3: Step4Panel
   └─ Element 4: Step5Panel
   
   Next Button: [Drag Next Button]
   Previous Button: [Drag Previous Button]
   Skip Button: [Drag Skip Button]
   Step Counter Text: [Drag TextMeshPro text]
   ```

---

## 🎯 Usage Examples

### From Code:

```csharp
// Transition to different states
GameStateManager.Instance.StartTutorial();
GameStateManager.Instance.StartGameplay();
GameStateManager.Instance.PauseGame();
GameStateManager.Instance.ReturnToWelcome();

// Check current state
if (GameStateManager.Instance.IsInState(GameState.Gameplay))
{
    // Do something only in gameplay
}

// Listen to state changes
GameStateManager.Instance.OnStateChanged += OnGameStateChanged;

void OnGameStateChanged(GameState oldState, GameState newState)
{
    Debug.Log($"State changed: {oldState} → {newState}");
}
```

### From UI Buttons:

You can also call state transitions directly from UI buttons using Unity Events:
1. Select button
2. OnClick() → Add event
3. Drag GameStateManager GameObject
4. Select `GameStateManager.StartGameplay()` (or other methods)

---

## 🔄 Workflow Example

**Your Specific Flow:**

1. **Game Starts:**
   - GameStateManager initializes
   - Applies Welcome state
   - WelcomeUI enabled
   - All gameplay systems disabled

2. **User Clicks "Play":**
   - WelcomeUIController.OnPlayButtonClicked()
   - Calls GameStateManager.StartTutorial()
   - Welcome UI disabled
   - Tutorial UI enabled
   - Stylus ray enabled for tutorial navigation

3. **User Completes Tutorial:**
   - TutorialUIController detects completion
   - Calls GameStateManager.StartGameplay()
   - Tutorial UI disabled
   - Gameplay systems enabled:
     - DetectionManager (spawns markers)
     - WordDrawingManager (drawing system)
     - Stylus interactions (ray, buttons, etc.)

4. **Gameplay Flow:**
   - User interacts with markers (existing system)
   - Marker UI → Practice UI → Writer UI (existing flow)
   - All managed within Gameplay state

---

## 🐛 Debugging

### Enable Logs:
- GameStateManager → Log State Changes: ✓

### Console Output Examples:
```
[GameStateManager] State transition: Welcome → Tutorial
[GameStateManager] Applying state: Tutorial
[GameStateManager] Enabled UI: TutorialUI
[GameStateManager] Disabled UI: WelcomeUI
[GameStateManager] Stylus ray: Enabled
[GameStateManager] Detection: Disabled
```

### Inspector Debug:
- Right-click GameStateManager component
- Select "Log Current State"
- Shows detailed state info

---

## ✨ Advanced Features

### Add New States:
1. Edit GameStateManager.cs
2. Add to `GameState` enum
3. Create new StateConfiguration in Inspector
4. Configure UI/Objects/Interactions

### Conditional Transitions:
```csharp
// Only allow gameplay if tutorial completed
bool tutorialCompleted = PlayerPrefs.GetInt("TutorialComplete", 0) == 1;
if (tutorialCompleted)
{
    GameStateManager.Instance.StartGameplay();
}
else
{
    GameStateManager.Instance.StartTutorial();
}
```

### Persist State Across Scenes:
- GameStateManager uses DontDestroyOnLoad()
- State persists when loading new scenes
- Useful for multi-scene games

---

## 📋 Checklist

Before testing:

- [ ] GameStateManager GameObject created
- [ ] All 5 states configured in Inspector
- [ ] WelcomeUI has WelcomeUIController
- [ ] TutorialUI has TutorialUIController
- [ ] Tutorial steps created and assigned
- [ ] System references assigned (Stylus, Detection, Drawing)
- [ ] Welcome state set as initial state
- [ ] Log State Changes enabled for debugging

---

## 🚀 Next Steps

After basic setup works:

1. **Create Pause Menu UI**
   - Add pause button in gameplay
   - Implement resume/quit functionality

2. **Create GameOver UI**
   - Show when learning session complete
   - Display stats (words learned, time spent, etc.)

3. **Add Transitions/Animations**
   - Fade between UI states
   - Smooth GameObject activation

4. **Save/Load Progress**
   - Save which words user learned
   - Resume from last state

---

**Setup Complete!** Your game now has a robust state management system.

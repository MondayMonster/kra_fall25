using UnityEngine;
using System.Collections.Generic;
using PassthroughCameraSamples.MultiObjectDetection;

/// <summary>
/// Manages game states and controls UI, GameObjects, and interactions
/// Singleton pattern for global access
/// </summary>
public class GameStateManager : MonoBehaviour
{
    #region Singleton
    public static GameStateManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Optional: persist across scenes
    }
    #endregion

    #region Game States
    public enum GameState
    {
        Welcome,              // Initial welcome screen
        Tutorial,             // Tutorial/instructions
        Gameplay,             // Main gameplay (detection, markers visible)
        GameplayObjectOverlay, // Marker/Speaker/Writer UI workflow (markers hidden)
        Paused,               // Game paused
        GameOver              // End state
    }

    [Header("Current State")]
    [SerializeField] private GameState currentState = GameState.Welcome;
    [SerializeField] private bool logStateChanges = true;

    public GameState CurrentState => currentState;
    #endregion

    #region State Configuration
    [System.Serializable]
    public class StateConfiguration
    {
        [Header("State Settings")]
        public GameState state;
        
        [Header("UI Management")]
        [Tooltip("Canvases to ENABLE when entering this state")]
        public GameObject[] uiToEnable;
        [Tooltip("Canvases to DISABLE when entering this state")]
        public GameObject[] uiToDisable;
        
        [Header("GameObject Management")]
        [Tooltip("GameObjects to ENABLE when entering this state")]
        public GameObject[] objectsToEnable;
        [Tooltip("GameObjects to DISABLE when entering this state")]
        public GameObject[] objectsToDisable;
        
        [Header("Interaction Settings")]
        [Tooltip("Enable stylus ray pointer in this state")]
        public bool enableStylusRay = false;
        [Tooltip("Enable detection/marker spawning in this state")]
        public bool enableDetection = false;
        [Tooltip("Enable drawing system in this state")]
        public bool enableDrawing = false;
    }

    [Header("State Configurations")]
    [SerializeField] private StateConfiguration[] stateConfigurations;
    
    // Quick lookup dictionary
    private Dictionary<GameState, StateConfiguration> stateConfigDict;
    #endregion

    #region System References
    [Header("System References")]
    [SerializeField] private MXInkStylusHandler stylusHandler;
    [SerializeField] private DetectionManager detectionManager;
    [SerializeField] private WordDrawingManager drawingManager;
    #endregion

    #region Initialization
    private void Start()
    {
        // Build state configuration dictionary for fast lookup
        stateConfigDict = new Dictionary<GameState, StateConfiguration>();
        foreach (var config in stateConfigurations)
        {
            if (!stateConfigDict.ContainsKey(config.state))
            {
                stateConfigDict.Add(config.state, config);
            }
        }

        // Auto-find system references if not assigned
        if (stylusHandler == null)
            stylusHandler = FindFirstObjectByType<MXInkStylusHandler>();
        if (detectionManager == null)
            detectionManager = FindFirstObjectByType<DetectionManager>();
        if (drawingManager == null)
            drawingManager = FindFirstObjectByType<WordDrawingManager>();

        // Apply initial state
        ApplyState(currentState);
    }
    #endregion

    #region State Transitions
    /// <summary>
    /// Transition to a new game state
    /// </summary>
    public void TransitionToState(GameState newState)
    {
        if (currentState == newState)
        {
            Debug.LogWarning($"[GameStateManager] Already in state: {newState}");
            return;
        }

        if (logStateChanges)
        {
            Debug.Log($"[GameStateManager] State transition: {currentState} → {newState}");
        }

        // Exit current state (optional cleanup)
        ExitState(currentState);

        // Update state
        GameState previousState = currentState;
        currentState = newState;

        // Enter new state
        ApplyState(newState);

        // Invoke state change event (optional)
        OnStateChanged?.Invoke(previousState, newState);
    }

    /// <summary>
    /// Public event for other systems to listen to state changes
    /// </summary>
    public System.Action<GameState, GameState> OnStateChanged;

    /// <summary>
    /// Shortcut methods for common transitions
    /// </summary>
    public void StartTutorial() => TransitionToState(GameState.Tutorial);
    public void StartGameplay() => TransitionToState(GameState.Gameplay);
    public void StartObjectOverlay() => TransitionToState(GameState.GameplayObjectOverlay);
    public void EndObjectOverlay() => TransitionToState(GameState.Gameplay);
    public void PauseGame() => TransitionToState(GameState.Paused);
    public void EndGame() => TransitionToState(GameState.GameOver);
    public void ReturnToWelcome() => TransitionToState(GameState.Welcome);
    #endregion

    #region State Application
    /// <summary>
    /// Apply all settings for a given state
    /// </summary>
    private void ApplyState(GameState state)
    {
        if (!stateConfigDict.TryGetValue(state, out StateConfiguration config))
        {
            Debug.LogWarning($"[GameStateManager] No configuration found for state: {state}");
            return;
        }

        if (logStateChanges)
        {
            Debug.Log($"[GameStateManager] Applying state: {state}");
        }

        // Apply UI changes
        ApplyUIChanges(config);

        // Apply GameObject changes
        ApplyGameObjectChanges(config);

        // Apply interaction settings
        ApplyInteractionSettings(config);
    }

    /// <summary>
    /// Exit current state (cleanup)
    /// </summary>
    private void ExitState(GameState state)
    {
        // Optional: Add cleanup logic here
        // e.g., Stop all sounds, clear temporary data, etc.
    }

    /// <summary>
    /// Enable/disable UI elements
    /// </summary>
    private void ApplyUIChanges(StateConfiguration config)
    {
        // Enable UIs
        foreach (var ui in config.uiToEnable)
        {
            if (ui != null)
            {
                ui.SetActive(true);
                if (logStateChanges)
                    Debug.Log($"[GameStateManager] Enabled UI: {ui.name}");
            }
        }

        // Disable UIs
        foreach (var ui in config.uiToDisable)
        {
            if (ui != null)
            {
                ui.SetActive(false);
                if (logStateChanges)
                    Debug.Log($"[GameStateManager] Disabled UI: {ui.name}");
            }
        }
    }

    /// <summary>
    /// Enable/disable GameObjects
    /// </summary>
    private void ApplyGameObjectChanges(StateConfiguration config)
    {
        // Enable GameObjects
        foreach (var obj in config.objectsToEnable)
        {
            if (obj != null)
            {
                obj.SetActive(true);
                if (logStateChanges)
                    Debug.Log($"[GameStateManager] Enabled GameObject: {obj.name}");
            }
        }

        // Disable GameObjects
        foreach (var obj in config.objectsToDisable)
        {
            if (obj != null)
            {
                obj.SetActive(false);
                if (logStateChanges)
                    Debug.Log($"[GameStateManager] Disabled GameObject: {obj.name}");
            }
        }
    }

    /// <summary>
    /// Apply interaction system settings
    /// </summary>
    private void ApplyInteractionSettings(StateConfiguration config)
    {
        // Stylus ray control
        if (stylusHandler != null)
        {
            stylusHandler.SetUIActive(!config.enableStylusRay);
            if (logStateChanges)
                Debug.Log($"[GameStateManager] Stylus ray: {(config.enableStylusRay ? "Enabled" : "Disabled")}");
        }

        // Detection control
        if (detectionManager != null)
        {
            detectionManager.OnPause(!config.enableDetection);
            if (logStateChanges)
                Debug.Log($"[GameStateManager] Detection: {(config.enableDetection ? "Enabled" : "Disabled")}");
        }

        // Drawing control
        if (drawingManager != null)
        {
            drawingManager.SetDrawingEnabled(config.enableDrawing);
            if (logStateChanges)
                Debug.Log($"[GameStateManager] Drawing: {(config.enableDrawing ? "Enabled" : "Disabled")}");
        }
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// Check if currently in a specific state
    /// </summary>
    public bool IsInState(GameState state) => currentState == state;

    /// <summary>
    /// Check if interaction is allowed in current state
    /// </summary>
    public bool IsInteractionAllowed()
    {
        if (stateConfigDict.TryGetValue(currentState, out StateConfiguration config))
        {
            return config.enableStylusRay || config.enableDetection || config.enableDrawing;
        }
        return false;
    }
    #endregion

    #region Debug
    [ContextMenu("Log Current State")]
    private void LogCurrentState()
    {
        Debug.Log($"[GameStateManager] Current State: {currentState}");
        if (stateConfigDict.TryGetValue(currentState, out StateConfiguration config))
        {
            Debug.Log($"  - Stylus Ray: {config.enableStylusRay}");
            Debug.Log($"  - Detection: {config.enableDetection}");
            Debug.Log($"  - Drawing: {config.enableDrawing}");
            Debug.Log($"  - Active UIs: {config.uiToEnable.Length}");
            Debug.Log($"  - Active Objects: {config.objectsToEnable.Length}");
        }
    }
    #endregion
}

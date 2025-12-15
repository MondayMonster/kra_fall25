using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the Welcome UI and transitions to Tutorial
/// </summary>
public class WelcomeUIController : MonoBehaviour
{
    [Header("Button References")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    private void Start()
    {
        Debug.Log("[WelcomeUI] Starting initialization...");
        
        // Setup button listeners
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayButtonClicked);
            Debug.Log("[WelcomeUI] ✓ Play button listener added");
        }
        else
        {
            Debug.LogError("[WelcomeUI] ✗ Play button is NULL! Please assign in Inspector.");
        }
        
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsButtonClicked);
            Debug.Log("[WelcomeUI] ✓ Settings button listener added");
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitButtonClicked);
            Debug.Log("[WelcomeUI] ✓ Quit button listener added");
        }
        
        Debug.Log("[WelcomeUI] Initialization complete");
    }

    private void OnPlayButtonClicked()
    {
        Debug.Log("========================================");
        Debug.Log("[WelcomeUI] ✓ Play button clicked - Starting tutorial");
        Debug.Log("========================================");
        
        // Check if GameStateManager exists
        if (GameStateManager.Instance == null)
        {
            Debug.LogError("[WelcomeUI] ✗ GameStateManager.Instance is NULL! Make sure GameStateManager GameObject exists in scene.");
            return;
        }
        
        Debug.Log("[WelcomeUI] ✓ GameStateManager found, calling StartTutorial()");
        
        // Transition to Tutorial state
        GameStateManager.Instance.StartTutorial();
        
        Debug.Log("[WelcomeUI] ✓ StartTutorial() called successfully");
    }

    private void OnSettingsButtonClicked()
    {
        Debug.Log("[WelcomeUI] Settings button clicked");
        // TODO: Open settings menu
    }

    private void OnQuitButtonClicked()
    {
        Debug.Log("[WelcomeUI] Quit button clicked");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}

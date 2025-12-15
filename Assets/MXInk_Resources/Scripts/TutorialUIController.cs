using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the Tutorial UI and transitions to Gameplay
/// </summary>
public class TutorialUIController : MonoBehaviour
{
    [Header("Tutorial Steps")]
    [SerializeField] private GameObject[] tutorialSteps; // Array of tutorial step panels
    [SerializeField] private int currentStepIndex = 0;

    [Header("UI References")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button playButton; // Start gameplay button
    [SerializeField] private TextMeshProUGUI stepCounterText; // e.g., "Step 1 of 5"

    [Header("Settings")]
    [SerializeField] private bool autoProgressOnComplete = false;

    private void Start()
    {
        // Setup button listeners
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }
        if (previousButton != null)
        {
            previousButton.onClick.AddListener(OnPreviousButtonClicked);
        }
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayButtonClicked);
        }

        // Show first step
        ShowStep(0);
    }

    private void OnEnable()
    {
        // Reset to first step when tutorial UI is enabled
        ShowStep(0);
    }

    private void ShowStep(int stepIndex)
    {
        // Validate index
        if (tutorialSteps == null || tutorialSteps.Length == 0)
        {
            Debug.LogWarning("[TutorialUI] No tutorial steps configured!");
            return;
        }

        // Clamp index
        currentStepIndex = Mathf.Clamp(stepIndex, 0, tutorialSteps.Length - 1);

        // Show only current step
        for (int i = 0; i < tutorialSteps.Length; i++)
        {
            if (tutorialSteps[i] != null)
            {
                tutorialSteps[i].SetActive(i == currentStepIndex);
            }
        }

        // Update step counter text
        if (stepCounterText != null)
        {
            stepCounterText.text = $"{currentStepIndex + 1}/{tutorialSteps.Length}";
        }

        // Update button states
        UpdateButtonStates();

        Debug.Log($"[TutorialUI] Showing step {currentStepIndex + 1}/{tutorialSteps.Length}");
    }

    private void UpdateButtonStates()
    {
        // Disable previous button on first step
        if (previousButton != null)
        {
            previousButton.interactable = currentStepIndex > 0;
        }

        // Disable next button on last step (user must click Play)
        if (nextButton != null)
        {
            bool isLastStep = currentStepIndex >= tutorialSteps.Length - 1;
            nextButton.interactable = !isLastStep;
        }
        
        // Enable play button only on last step
        if (playButton != null)
        {
            bool isLastStep = currentStepIndex >= tutorialSteps.Length - 1;
            playButton.gameObject.SetActive(isLastStep);
        }
    }

    private void OnNextButtonClicked()
    {
        if (currentStepIndex < tutorialSteps.Length - 1)
        {
            // Go to next step
            ShowStep(currentStepIndex + 1);
        }
        else
        {
            // Last step reached - but don't auto-start gameplay
            // User must click Play button to start
            Debug.Log("[TutorialUI] Reached last tutorial step. Click Play to start gameplay.");
        }
    }

    private void OnPreviousButtonClicked()
    {
        if (currentStepIndex > 0)
        {
            ShowStep(currentStepIndex - 1);
        }
    }

    private void OnPlayButtonClicked()
    {
        Debug.Log("[TutorialUI] Play button clicked - Starting gameplay");
        StartGameplay();
    }

    private void StartGameplay()
    {
        Debug.Log("[TutorialUI] Starting gameplay");

        // Transition to Gameplay state
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.StartGameplay();
        }
        else
        {
            Debug.LogError("[TutorialUI] GameStateManager not found!");
        }
    }

    #region Public Methods
    /// <summary>
    /// Call this from external scripts to advance tutorial (e.g., when user completes an action)
    /// </summary>
    public void AdvanceToNextStep()
    {
        OnNextButtonClicked();
    }

    /// <summary>
    /// Jump to a specific step
    /// </summary>
    public void GoToStep(int stepIndex)
    {
        ShowStep(stepIndex);
    }
    #endregion
}

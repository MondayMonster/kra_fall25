using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Controls the Practice UI for speech recognition
/// User practices saying the Spanish word before writing it
/// </summary>
public class PracticeUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI spanishWordText;
    [SerializeField] private TextMeshProUGUI transcribedText;
    [SerializeField] private GameObject recordingIndicator;
    [SerializeField] private Button recordButton;  // Single record button (auto-stops after 5s)
    [SerializeField] private Button nextButton;    // Go to Writer UI

    [Header("Service References")]
    [SerializeField] private STTManager sttManager;
    [SerializeField] private MXInkStylusHandler stylusHandler;
    
    [Header("Recording Settings")]
    [SerializeField] private float autoStopDuration = 5f;  // Auto-stop recording after 5 seconds

    private string currentObjectName;
    private string currentSpanishWord;
    private bool isRecording = false;
    private float recordingStartTime;
    
    // Event for workflow progression
    public System.Action OnNextClicked;

    private void Start()
    {
        // Find services if not assigned
        if (sttManager == null)
        {
            sttManager = FindFirstObjectByType<STTManager>();
        }
        if (stylusHandler == null)
        {
            stylusHandler = FindFirstObjectByType<MXInkStylusHandler>();
        }

        // Setup button listeners
        if (recordButton != null)
        {
            recordButton.onClick.AddListener(OnRecordButtonClicked);
            Debug.Log("[PracticeUIController] Record button assigned and listener added");
        }
        else
        {
            Debug.LogWarning("[PracticeUIController] Record button is NOT assigned!");
        }
        
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextButtonClicked);
            Debug.Log("[PracticeUIController] Next button assigned and listener added");
        }
        else
        {
            Debug.LogWarning("[PracticeUIController] Next button is NOT assigned!");
        }

        // Hide recording indicator initially
        if (recordingIndicator != null)
        {
            recordingIndicator.SetActive(false);
        }

        // Subscribe to STT callback
        if (sttManager != null)
        {
            sttManager.OnTranscriptionComplete += OnTranscriptionReceived;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from STT callback
        if (sttManager != null)
        {
            sttManager.OnTranscriptionComplete -= OnTranscriptionReceived;
        }
    }

    private void Update()
    {
        // Auto-stop recording after autoStopDuration seconds
        if (isRecording && Time.time - recordingStartTime >= autoStopDuration)
        {
            Debug.Log($"[PracticeUIController] Auto-stopping recording after {autoStopDuration} seconds");
            StopRecording();
        }
    }

    /// <summary>
    /// Initialize the Practice UI with Spanish word
    /// </summary>
    public void Initialize(string objectName, string spanishWord)
    {
        currentObjectName = objectName;
        currentSpanishWord = spanishWord;

        // Set UI texts
        if (instructionText != null)
        {
            instructionText.text = "Practice saying:";
        }
        if (spanishWordText != null)
        {
            spanishWordText.text = spanishWord;
        }
        if (transcribedText != null)
        {
            transcribedText.text = "Press record or front button to start...";
        }

        Debug.Log($"[PracticeUIController] Initialized with word: {spanishWord}");
    }

    private void OnRecordButtonClicked()
    {
        if (!isRecording)
        {
            StartRecording();
        }
        else
        {
            StopRecording();
        }
    }

    /// <summary>
    /// Handle Next button click - transition to Writer UI
    /// </summary>
    private void OnNextButtonClicked()
    {
        Debug.Log("[PracticeUIController] Next button clicked - triggering workflow transition");
        OnNextClicked?.Invoke();
    }

    /// <summary>
    /// Start recording audio (auto-stops after 5 seconds)
    /// </summary>
    private void StartRecording()
    {
        if (sttManager == null)
        {
            Debug.LogError("[PracticeUIController] STT Manager not found!");
            return;
        }

        if (isRecording)
        {
            Debug.LogWarning("[PracticeUIController] Already recording!");
            return;
        }

        Debug.Log("[PracticeUIController] Starting recording...");
        
        sttManager.StartRecording();
        isRecording = true;
        recordingStartTime = Time.time;

        // Update UI
        if (recordingIndicator != null)
        {
            recordingIndicator.SetActive(true);
        }
        if (recordButton != null)
        {
            // Change button to show it's recording (could change text/color in inspector)
            recordButton.interactable = true;
        }
        if (transcribedText != null)
        {
            transcribedText.text = $"Recording... ({autoStopDuration}s)";
        }
    }

    /// <summary>
    /// Stop recording and process transcription
    /// </summary>
    private void StopRecording()
    {
        if (!isRecording)
        {
            return;
        }

        Debug.Log("[PracticeUIController] Stopping recording...");
        
        sttManager.StopRecordingAndTranscribe();
        isRecording = false;

        // Update UI
        if (recordingIndicator != null)
        {
            recordingIndicator.SetActive(false);
        }
        if (recordButton != null)
        {
            recordButton.interactable = true;
        }
        if (transcribedText != null)
        {
            transcribedText.text = "Processing...";
        }
    }

    /// <summary>
    /// Handle transcription result from STT Manager
    /// </summary>
    private void OnTranscriptionReceived(string transcribedWord, bool success)
    {
        if (success)
        {
            Debug.Log($"[PracticeUIController] Transcribed: \"{transcribedWord}\"");
            
            if (transcribedText != null)
            {
                transcribedText.text = $"You said: {transcribedWord}";
            }

            // Optional: Check if it matches (case-insensitive, trimmed)
            string normalizedTranscribed = transcribedWord.Trim().ToLower();
            string normalizedExpected = currentSpanishWord.Trim().ToLower();

            if (normalizedTranscribed == normalizedExpected)
            {
                if (transcribedText != null)
                {
                    transcribedText.text = $"✓ Perfect! {transcribedWord}";
                }
                Debug.Log("[PracticeUIController] Pronunciation match!");
            }
            else
            {
                if (transcribedText != null)
                {
                    transcribedText.text = $"You said: {transcribedWord}\nTry again!";
                }
                Debug.Log($"[PracticeUIController] Pronunciation mismatch. Expected: {currentSpanishWord}");
            }
        }
        else
        {
            Debug.LogError("[PracticeUIController] Transcription failed!");
            
            if (transcribedText != null)
            {
                transcribedText.text = "Error! Please try again.";
            }
        }
    }
}

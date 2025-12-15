using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the UI that appears when interacting with a marker
/// Displays Spanish translation and example sentences from GPT
/// </summary>
public class MarkerUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI objectNameText;
    [SerializeField] private TextMeshProUGUI spanishTranslationText;
    [SerializeField] private TextMeshProUGUI simpleSentenceSpanishText;
    [SerializeField] private TextMeshProUGUI simpleSentenceEnglishText;
    [SerializeField] private TextMeshProUGUI positionalSentenceSpanishText;
    [SerializeField] private TextMeshProUGUI positionalSentenceEnglishText;
    [SerializeField] private GameObject loadingIndicator;
    
    [Header("Buttons")]
    [SerializeField] private Button playPronunciationButton;   // Play pronunciation TTS
    [SerializeField] private Button playSimpleSentenceButton;  // Play simple sentence TTS
    [SerializeField] private Button playPositionalButton;      // Play positional sentence TTS
    [SerializeField] private Button nextButton;                // Go to Speaker UI

    [Header("TTS Configuration")]
    [SerializeField] private float delayBetweenSpeech = 2f; // Delay between each speech in sequence

    [Header("Service References")]
    [SerializeField] private GPTLanguageService gptService;
    [SerializeField] private TTSManager ttsManager;
    [SerializeField] private MXInkStylusHandler stylusHandler;

    private string currentObjectName;
    private string currentEnvironment;
    
    // Public property to access Spanish translation
    public string SpanishTranslation { get; private set; }
    
    // Event for workflow progression
    public System.Action OnNextClicked;

    private void Start()
    {
        // Hook up button listeners
        if (playPronunciationButton != null)
            playPronunciationButton.onClick.AddListener(OnPlayPronunciationClicked);
        
        if (playSimpleSentenceButton != null)
            playSimpleSentenceButton.onClick.AddListener(OnPlaySimpleSentenceClicked);
        
        if (playPositionalButton != null)
            playPositionalButton.onClick.AddListener(OnPlayPositionalClicked);
        
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextButtonClicked);

        // Find services if not assigned
        if (gptService == null)
        {
            gptService = FindFirstObjectByType<GPTLanguageService>();
        }
        if (ttsManager == null)
        {
            ttsManager = FindFirstObjectByType<TTSManager>();
        }
        if (stylusHandler == null)
        {
            stylusHandler = FindFirstObjectByType<MXInkStylusHandler>();
        }
    }

    /// <summary>
    /// Initialize the UI with object data and fetch GPT content
    /// </summary>
    public void Initialize(string objectName, string environment)
    {
        currentObjectName = objectName;
        currentEnvironment = environment;

        // Set object name
        if (objectNameText != null)
        {
            objectNameText.text = objectName.ToUpper();
        }

        // Show loading state
        ShowLoadingState();

        // Fetch data from GPT
        if (gptService != null)
        {
            gptService.GetLanguageData(objectName, environment, OnLanguageDataReceived);
        }
        else
        {
            Debug.LogError("[MarkerUIController] GPT Language Service not found!");
            ShowError("Service not available");
        }
    }

    private void ShowLoadingState()
    {
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);

        SetText(spanishTranslationText, "Loading...");
        SetText(simpleSentenceSpanishText, "Cargando...");
        SetText(simpleSentenceEnglishText, "Loading...");
        SetText(positionalSentenceSpanishText, "Cargando...");
        SetText(positionalSentenceEnglishText, "Loading...");
    }

    private void OnLanguageDataReceived(GPTLanguageService.LanguageResponse response)
    {
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);

        if (response.success)
        {
            Debug.Log($"[MarkerUIController] Received data for {currentObjectName}");
            
            SpanishTranslation = response.spanishTranslation; // Store for Writer UI
            
            SetText(spanishTranslationText, response.spanishTranslation);
            SetText(simpleSentenceSpanishText, response.simpleSentenceSpanish);
            SetText(simpleSentenceEnglishText, response.simpleSentenceEnglish);
            SetText(positionalSentenceSpanishText, response.positionalSentenceSpanish);
            SetText(positionalSentenceEnglishText, response.positionalSentenceEnglish);
        }
        else
        {
            Debug.LogError($"[MarkerUIController] Error: {response.errorMessage}");
            ShowError(response.errorMessage);
        }
    }

    private void ShowError(string errorMessage)
    {
        SetText(spanishTranslationText, "Error");
        SetText(simpleSentenceSpanishText, errorMessage);
        SetText(simpleSentenceEnglishText, errorMessage);
        SetText(positionalSentenceSpanishText, "Por favor, inténtalo de nuevo");
        SetText(positionalSentenceEnglishText, "Please try again");
    }

    private void SetText(TextMeshProUGUI textComponent, string text)
    {
        if (textComponent != null)
        {
            textComponent.text = text;
        }
    }

    #region TTS Speech

    /// <summary>
    /// Play pronunciation (Spanish translation word only)
    /// </summary>
    private void OnPlayPronunciationClicked()
    {
        if (ttsManager == null)
        {
            Debug.LogWarning("[MarkerUIController] Cannot speak - TTS Manager missing");
            return;
        }

        if (!string.IsNullOrEmpty(SpanishTranslation))
        {
            Debug.Log($"[MarkerUIController] Playing pronunciation: {SpanishTranslation}");
            ttsManager.SpeakSpanish(SpanishTranslation);
        }
    }

    /// <summary>
    /// Play simple sentence in Spanish
    /// </summary>
    private void OnPlaySimpleSentenceClicked()
    {
        if (ttsManager == null)
        {
            Debug.LogWarning("[MarkerUIController] Cannot speak - TTS Manager missing");
            return;
        }

        if (simpleSentenceSpanishText != null && !string.IsNullOrEmpty(simpleSentenceSpanishText.text))
        {
            Debug.Log($"[MarkerUIController] Playing simple sentence: {simpleSentenceSpanishText.text}");
            ttsManager.SpeakSpanish(simpleSentenceSpanishText.text);
        }
    }

    /// <summary>
    /// Play positional sentence in Spanish
    /// </summary>
    private void OnPlayPositionalClicked()
    {
        if (ttsManager == null)
        {
            Debug.LogWarning("[MarkerUIController] Cannot speak - TTS Manager missing");
            return;
        }

        if (positionalSentenceSpanishText != null && !string.IsNullOrEmpty(positionalSentenceSpanishText.text))
        {
            Debug.Log($"[MarkerUIController] Playing positional sentence: {positionalSentenceSpanishText.text}");
            ttsManager.SpeakSpanish(positionalSentenceSpanishText.text);
        }
    }

    #endregion

    #region Button Handlers

    /// <summary>
    /// Handle Next button click - transition to Speaker UI
    /// </summary>
    private void OnNextButtonClicked()
    {
        Debug.Log("[MarkerUIController] Next button clicked - triggering workflow transition");
        OnNextClicked?.Invoke();
    }

    #endregion
}

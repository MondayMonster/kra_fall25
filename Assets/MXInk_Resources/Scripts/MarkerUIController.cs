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
    [SerializeField] private Button closeButton;

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

    private void Start()
    {
        // Hook up close button
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

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

    private void Update()
    {
        // Check for front button press to speak all Spanish texts
        // Only allow TTS when this UI is active and enabled
        if (stylusHandler != null && stylusHandler.FrontButtonPressedThisFrame && gameObject.activeInHierarchy)
        {
            SpeakAllSpanishTexts();
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
    /// Speak all texts sequentially in Spanish using OpenAI TTS
    /// Speaks: Spanish translation word, Spanish simple sentence, Spanish positional sentence
    /// </summary>
    private void SpeakAllSpanishTexts()
    {
        if (ttsManager == null)
        {
            Debug.LogWarning("[MarkerUIController] Cannot speak - TTS Manager missing");
            return;
        }

        // Speak Spanish texts: pronunciation, simple sentence Spanish, positional sentence Spanish
        string[] textsToSpeak = new string[3];
        textsToSpeak[0] = SpanishTranslation; // Spanish word pronunciation
        textsToSpeak[1] = simpleSentenceSpanishText != null ? simpleSentenceSpanishText.text : "";
        textsToSpeak[2] = positionalSentenceSpanishText != null ? positionalSentenceSpanishText.text : "";

        Debug.Log($"[MarkerUIController] Front button pressed - speaking Spanish texts with {delayBetweenSpeech}s delay");
        ttsManager.SpeakSequence(textsToSpeak, delayBetweenSpeech);
    }

    #endregion

    private void OnCloseButtonClicked()
    {
        Debug.Log("[MarkerUIController] Close button clicked");
        
        // Show markers again
        var detectionManager = FindFirstObjectByType<PassthroughCameraSamples.MultiObjectDetection.DetectionManager>();
        if (detectionManager != null)
        {
            detectionManager.ShowAllMarkers();
        }

        // Destroy this UI (DetectionManager will handle clearing m_currentSpawnedUI reference)
        Destroy(gameObject);
    }

    /// <summary>
    /// Public method to close UI (can be called from stylus interaction)
    /// </summary>
    public void CloseUI()
    {
        OnCloseButtonClicked();
    }
}

using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

/// <summary>
/// Manages Text-to-Speech functionality using OpenAI TTS API for Spanish support
/// </summary>
public class TTSManager : MonoBehaviour
{
    [Header("TTS Configuration")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private string openAIApiKey = ""; // Set your OpenAI API key here
    [SerializeField] private string voiceModel = "tts-1"; // or "tts-1-hd" for higher quality
    [SerializeField] private string voiceName = "nova"; // nova, alloy, echo, fable, onyx, shimmer
    [Range(0.25f, 4.0f)]
    [SerializeField] private float speechSpeed = 0.85f; // 0.25-4.0, default 1.0, lower = slower
    
    [Header("Audio Feedback")]
    [SerializeField] private bool logSpeechEvents = true;
    
    private bool isSpeaking = false;
    private const string OpenAITTSUrl = "https://api.openai.com/v1/audio/speech";

    private void Start()
    {
        // If no AudioSource assigned, add one
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D audio
        }
        
        if (string.IsNullOrEmpty(openAIApiKey))
        {
            Debug.LogWarning("[TTSManager] OpenAI API Key not set! Please set it in the Inspector.");
        }
    }

    private void OnDisable()
    {
        StopSpeaking();
    }

    /// <summary>
    /// Speak text in Spanish using OpenAI TTS
    /// </summary>
    public void SpeakSpanish(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("[TTSManager] Cannot speak empty text");
            return;
        }

        if (string.IsNullOrEmpty(openAIApiKey))
        {
            Debug.LogError("[TTSManager] OpenAI API Key not set!");
            return;
        }

        if (logSpeechEvents)
        {
            Debug.Log($"[TTSManager] Speaking Spanish: \"{text}\"");
        }

        StartCoroutine(RequestOpenAITTS(text));
    }

    /// <summary>
    /// Request TTS audio from OpenAI API
    /// </summary>
    private IEnumerator RequestOpenAITTS(string text)
    {
        isSpeaking = true;

        // Build JSON request
        string jsonPayload = $@"{{
            ""model"": ""{voiceModel}"",
            ""input"": ""{EscapeJson(text)}"",
            ""voice"": ""{voiceName}"",
            ""speed"": {speechSpeed.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}
        }}";

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest request = new UnityWebRequest(OpenAITTSUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerAudioClip(OpenAITTSUrl, AudioType.MPEG);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {openAIApiKey}");

            if (logSpeechEvents)
            {
                Debug.Log("[TTSManager] Requesting TTS audio from OpenAI...");
            }

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                
                if (clip != null)
                {
                    audioSource.clip = clip;
                    audioSource.Play();
                    
                    if (logSpeechEvents)
                    {
                        Debug.Log($"[TTSManager] Playing TTS audio (length: {clip.length}s)");
                    }

                    // Wait for audio to finish
                    yield return new WaitForSeconds(clip.length);
                }
                else
                {
                    Debug.LogError("[TTSManager] Failed to get audio clip from response");
                }
            }
            else
            {
                Debug.LogError($"[TTSManager] TTS API Error: {request.error}\nResponse: {request.downloadHandler.text}");
            }

            isSpeaking = false;
        }
    }

    /// <summary>
    /// Escape special characters for JSON
    /// </summary>
    private string EscapeJson(string text)
    {
        return text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }

    /// <summary>
    /// Stop current speech
    /// </summary>
    public void StopSpeaking()
    {
        StopAllCoroutines();
        
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            isSpeaking = false;
            
            if (logSpeechEvents)
            {
                Debug.Log("[TTSManager] Speech stopped");
            }
        }
    }

    /// <summary>
    /// Check if currently speaking
    /// </summary>
    public bool IsSpeaking => isSpeaking;

    #region Sequential Speech

    /// <summary>
    /// Speak multiple texts sequentially with delays between them
    /// </summary>
    public void SpeakSequence(string[] texts, float delayBetween = 2f)
    {
        if (texts == null || texts.Length == 0)
        {
            Debug.LogWarning("[TTSManager] Cannot speak empty sequence");
            return;
        }

        StopAllCoroutines();
        StartCoroutine(SpeakSequenceCoroutine(texts, delayBetween));
    }

    private IEnumerator SpeakSequenceCoroutine(string[] texts, float delayBetween)
    {
        Debug.Log($"[TTSManager] Starting speech sequence with {texts.Length} items");

        foreach (string text in texts)
        {
            if (!string.IsNullOrEmpty(text) && text != "Loading..." && text != "Cargando...")
            {
                yield return StartCoroutine(RequestOpenAITTS(text));

                // Wait for delay before next speech
                yield return new WaitForSeconds(delayBetween);
            }
        }

        Debug.Log("[TTSManager] Speech sequence completed");
    }

    #endregion
}

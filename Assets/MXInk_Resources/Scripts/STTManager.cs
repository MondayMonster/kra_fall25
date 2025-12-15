using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Manages Speech-to-Text using OpenAI Whisper API for Spanish language
/// Records audio from microphone and transcribes it to text
/// </summary>
public class STTManager : MonoBehaviour
{
    [Header("OpenAI Configuration")]
    [SerializeField] private string openAIApiKey = "";
    [SerializeField] private string whisperModel = "whisper-1";
    [SerializeField] private string language = "es"; // Spanish

    [Header("Recording Settings")]
    [SerializeField] private int recordingLengthSeconds = 5;
    [SerializeField] private int recordingFrequency = 44100;
    
    [Header("Debug")]
    [SerializeField] private bool logSTTEvents = true;

    private const string OpenAIWhisperUrl = "https://api.openai.com/v1/audio/transcriptions";
    
    private AudioClip recordedClip;
    private string microphoneDevice;
    private bool isRecording = false;
    private bool isProcessing = false;

    // Callback for transcription results
    public System.Action<string, bool> OnTranscriptionComplete; // (transcribedText, success)

    private void Start()
    {
        // Check for microphone
        if (Microphone.devices.Length > 0)
        {
            microphoneDevice = Microphone.devices[0];
            if (logSTTEvents)
            {
                Debug.Log($"[STTManager] Microphone found: {microphoneDevice}");
            }
        }
        else
        {
            Debug.LogError("[STTManager] No microphone detected!");
        }

        if (string.IsNullOrEmpty(openAIApiKey))
        {
            Debug.LogWarning("[STTManager] OpenAI API Key not set! Please set it in the Inspector.");
        }
    }

    /// <summary>
    /// Start recording audio from microphone
    /// </summary>
    public void StartRecording()
    {
        if (isRecording)
        {
            Debug.LogWarning("[STTManager] Already recording!");
            return;
        }

        if (string.IsNullOrEmpty(microphoneDevice))
        {
            Debug.LogError("[STTManager] No microphone available!");
            return;
        }

        if (logSTTEvents)
        {
            Debug.Log($"[STTManager] Starting recording for {recordingLengthSeconds} seconds...");
        }

        recordedClip = Microphone.Start(microphoneDevice, false, recordingLengthSeconds, recordingFrequency);
        isRecording = true;
    }

    /// <summary>
    /// Stop recording and send to Whisper API for transcription
    /// </summary>
    public void StopRecordingAndTranscribe()
    {
        if (!isRecording)
        {
            Debug.LogWarning("[STTManager] Not currently recording!");
            return;
        }

        if (logSTTEvents)
        {
            Debug.Log("[STTManager] Stopping recording...");
        }

        int position = Microphone.GetPosition(microphoneDevice);
        Microphone.End(microphoneDevice);
        isRecording = false;

        // Trim the audio clip to actual recorded length
        if (position > 0 && recordedClip != null)
        {
            float[] samples = new float[position * recordedClip.channels];
            recordedClip.GetData(samples, 0);

            AudioClip trimmedClip = AudioClip.Create("TrimmedRecording", position, recordedClip.channels, recordedClip.frequency, false);
            trimmedClip.SetData(samples, 0);
            recordedClip = trimmedClip;

            if (logSTTEvents)
            {
                Debug.Log($"[STTManager] Recording complete. Length: {recordedClip.length:F2}s");
            }

            // Send to Whisper API
            StartCoroutine(TranscribeAudio(recordedClip));
        }
        else
        {
            Debug.LogError("[STTManager] Recording failed or no audio captured!");
            OnTranscriptionComplete?.Invoke("", false);
        }
    }

    /// <summary>
    /// Cancel recording without transcribing
    /// </summary>
    public void CancelRecording()
    {
        if (isRecording)
        {
            Microphone.End(microphoneDevice);
            isRecording = false;
            
            if (logSTTEvents)
            {
                Debug.Log("[STTManager] Recording cancelled");
            }
        }
    }

    /// <summary>
    /// Transcribe audio using OpenAI Whisper API
    /// </summary>
    private IEnumerator TranscribeAudio(AudioClip clip)
    {
        if (string.IsNullOrEmpty(openAIApiKey))
        {
            Debug.LogError("[STTManager] OpenAI API Key not set!");
            OnTranscriptionComplete?.Invoke("", false);
            yield break;
        }

        isProcessing = true;

        // Convert AudioClip to WAV bytes
        byte[] wavData = ConvertAudioClipToWav(clip);

        if (logSTTEvents)
        {
            Debug.Log($"[STTManager] Sending {wavData.Length} bytes to Whisper API...");
        }

        // Create multipart form data
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavData, "recording.wav", "audio/wav");
        form.AddField("model", whisperModel);
        form.AddField("language", language);

        using (UnityWebRequest request = UnityWebRequest.Post(OpenAIWhisperUrl, form))
        {
            request.SetRequestHeader("Authorization", $"Bearer {openAIApiKey}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Parse JSON response
                string jsonResponse = request.downloadHandler.text;
                WhisperResponse response = JsonUtility.FromJson<WhisperResponse>(jsonResponse);

                if (logSTTEvents)
                {
                    Debug.Log($"[STTManager] Transcription: \"{response.text}\"");
                }

                OnTranscriptionComplete?.Invoke(response.text, true);
            }
            else
            {
                Debug.LogError($"[STTManager] Whisper API Error: {request.error}\nResponse: {request.downloadHandler.text}");
                OnTranscriptionComplete?.Invoke("", false);
            }

            isProcessing = false;
        }
    }

    /// <summary>
    /// Convert AudioClip to WAV format bytes
    /// </summary>
    private byte[] ConvertAudioClipToWav(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        short[] intData = new short[samples.Length];
        byte[] bytesData = new byte[samples.Length * 2];

        // Convert float samples to 16-bit PCM
        int rescaleFactor = 32767;
        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * rescaleFactor);
            byte[] byteArr = System.BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        // Create WAV file with header
        int hz = clip.frequency;
        int channels = clip.channels;
        int samples_count = samples.Length;

        byte[] wav = new byte[44 + bytesData.Length];

        // WAV Header
        System.Buffer.BlockCopy(System.Text.Encoding.UTF8.GetBytes("RIFF"), 0, wav, 0, 4);
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(wav.Length - 8), 0, wav, 4, 4);
        System.Buffer.BlockCopy(System.Text.Encoding.UTF8.GetBytes("WAVE"), 0, wav, 8, 4);
        System.Buffer.BlockCopy(System.Text.Encoding.UTF8.GetBytes("fmt "), 0, wav, 12, 4);
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(16), 0, wav, 16, 4);
        System.Buffer.BlockCopy(System.BitConverter.GetBytes((short)1), 0, wav, 20, 2);
        System.Buffer.BlockCopy(System.BitConverter.GetBytes((short)channels), 0, wav, 22, 2);
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(hz), 0, wav, 24, 4);
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(hz * channels * 2), 0, wav, 28, 4);
        System.Buffer.BlockCopy(System.BitConverter.GetBytes((short)(channels * 2)), 0, wav, 32, 2);
        System.Buffer.BlockCopy(System.BitConverter.GetBytes((short)16), 0, wav, 34, 2);
        System.Buffer.BlockCopy(System.Text.Encoding.UTF8.GetBytes("data"), 0, wav, 36, 4);
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(bytesData.Length), 0, wav, 40, 4);
        System.Buffer.BlockCopy(bytesData, 0, wav, 44, bytesData.Length);

        return wav;
    }

    /// <summary>
    /// Check if currently recording
    /// </summary>
    public bool IsRecording => isRecording;

    /// <summary>
    /// Check if currently processing transcription
    /// </summary>
    public bool IsProcessing => isProcessing;

    [System.Serializable]
    private class WhisperResponse
    {
        public string text;
    }
}

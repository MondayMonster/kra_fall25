using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Service for calling OpenAI GPT API to generate Spanish translations and example sentences
/// </summary>
public class GPTLanguageService : MonoBehaviour
{
    [Header("API Configuration")]
    [SerializeField] private string apiKey = ""; // Set in Inspector or via code
    [SerializeField] private string model = "gpt-4o-mini"; // or "gpt-4o"
    [SerializeField] private int maxTokens = 150;
    [SerializeField] private float temperature = 0.7f;
    [SerializeField] private bool useMockData = false; // Enable for testing without API
    
    private const string API_URL = "https://api.openai.com/v1/chat/completions";
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    /// <summary>
    /// Response structure from GPT API
    /// </summary>
    [Serializable]
    public class LanguageResponse
    {
        public string spanishTranslation;
        public string simpleSentenceSpanish;
        public string simpleSentenceEnglish;
        public string positionalSentenceSpanish;
        public string positionalSentenceEnglish;
        public bool success;
        public string errorMessage;
    }

    /// <summary>
    /// Call GPT API to get Spanish translation and sentences for a detected object
    /// </summary>
    /// <param name="objectName">The detected object (e.g., "laptop")</param>
    /// <param name="environment">The environment/room (e.g., "office")</param>
    /// <param name="callback">Callback with the response</param>
    public void GetLanguageData(string objectName, string environment, Action<LanguageResponse> callback)
    {
        // Mock mode for testing without API
        if (useMockData)
        {
            Debug.Log($"[GPTLanguageService] Using MOCK data for: {objectName}");
            StartCoroutine(ReturnMockData(objectName, callback));
            return;
        }

        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("[GPTLanguageService] API Key is not set!");
            callback?.Invoke(new LanguageResponse 
            { 
                success = false, 
                errorMessage = "API Key not configured" 
            });
            return;
        }

        StartCoroutine(CallGPTAPI(objectName, environment, callback));
    }

    private IEnumerator ReturnMockData(string objectName, Action<LanguageResponse> callback)
    {
        // Simulate API delay
        yield return new WaitForSeconds(0.5f);

        var response = new LanguageResponse
        {
            spanishTranslation = $"el/la {objectName}",
            simpleSentenceSpanish = $"El {objectName} es grande.",
            simpleSentenceEnglish = $"The {objectName} is big.",
            positionalSentenceSpanish = $"El {objectName} está aquí.",
            positionalSentenceEnglish = $"The {objectName} is here.",
            success = true,
            errorMessage = null
        };

        Debug.Log($"[GPTLanguageService] Mock data returned for: {objectName}");
        callback?.Invoke(response);
    }

    private IEnumerator CallGPTAPI(string objectName, string environment, Action<LanguageResponse> callback)
    {
        if (enableDebugLogs)
            Debug.Log($"[GPTLanguageService] Requesting data for: {objectName} in {environment}");

        // Build the prompt
        string prompt = BuildPrompt(objectName, environment);

        // Create the request body
        var requestBody = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = "You are a helpful Spanish language learning assistant. Provide responses in valid JSON format only." },
                new { role = "user", content = prompt }
            },
            max_tokens = maxTokens,
            temperature = temperature
        };

        string jsonBody = JsonUtility.ToJson(requestBody);
        // Note: JsonUtility doesn't handle nested objects well, so we'll build it manually
        jsonBody = BuildRequestJson(prompt);

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        // Create web request
        using (UnityWebRequest request = new UnityWebRequest(API_URL, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            // Send request
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                if (enableDebugLogs)
                    Debug.Log($"[GPTLanguageService] API Response: {request.downloadHandler.text}");

                // Parse response
                LanguageResponse response = ParseGPTResponse(request.downloadHandler.text);
                callback?.Invoke(response);
            }
            else
            {
                Debug.LogError($"[GPTLanguageService] API Error: {request.error}");
                Debug.LogError($"[GPTLanguageService] Response: {request.downloadHandler.text}");
                
                callback?.Invoke(new LanguageResponse 
                { 
                    success = false, 
                    errorMessage = $"API Error: {request.error}" 
                });
            }
        }
    }

    private string BuildPrompt(string objectName, string environment)
    {
        return $@"For the object ""{objectName}"" in a ""{environment}"" environment, provide the following in JSON format:

1. Spanish translation of ""{objectName}""
2. A simple descriptive sentence about ""{objectName}"" in Spanish (e.g., color, size, or state)
3. The same simple sentence translated to English
4. A positional/prepositional sentence in Spanish describing where ""{objectName}"" typically is in a ""{environment}""
5. The same positional sentence translated to English

Return ONLY valid JSON in this exact format:
{{
  ""spanishTranslation"": ""the Spanish word for {objectName}"",
  ""simpleSentenceSpanish"": ""El {objectName} es [description in Spanish]."",
  ""simpleSentenceEnglish"": ""The {objectName} is [description in English]."",
  ""positionalSentenceSpanish"": ""El {objectName} está [position/location in Spanish]."",
  ""positionalSentenceEnglish"": ""The {objectName} is [position/location in English].""
}}

Example for laptop in office:
{{
  ""spanishTranslation"": ""el portátil"",
  ""simpleSentenceSpanish"": ""El portátil es negro."",
  ""simpleSentenceEnglish"": ""The laptop is black."",
  ""positionalSentenceSpanish"": ""El portátil está sobre el escritorio."",
  ""positionalSentenceEnglish"": ""The laptop is on the desk.""
}}";
    }

    private string BuildRequestJson(string prompt)
    {
        // Manually build JSON to avoid JsonUtility limitations
        prompt = prompt.Replace("\"", "\\\"").Replace("\n", "\\n");
        
        return $@"{{
  ""model"": ""{model}"",
  ""messages"": [
    {{
      ""role"": ""system"",
      ""content"": ""You are a helpful Spanish language learning assistant. Provide responses in valid JSON format only.""
    }},
    {{
      ""role"": ""user"",
      ""content"": ""{prompt}""
    }}
  ],
  ""max_tokens"": {maxTokens},
  ""temperature"": {temperature}
}}";
    }

    private LanguageResponse ParseGPTResponse(string jsonResponse)
    {
        try
        {
            // Parse the OpenAI response structure
            var apiResponse = JsonUtility.FromJson<OpenAIResponse>(jsonResponse);
            
            if (apiResponse?.choices == null || apiResponse.choices.Length == 0)
            {
                return new LanguageResponse 
                { 
                    success = false, 
                    errorMessage = "Invalid API response structure" 
                };
            }

            string content = apiResponse.choices[0].message.content.Trim();
            
            // Remove markdown code blocks if present
            if (content.StartsWith("```json"))
            {
                content = content.Substring(7);
                int endIndex = content.LastIndexOf("```");
                if (endIndex > 0)
                    content = content.Substring(0, endIndex);
            }
            else if (content.StartsWith("```"))
            {
                content = content.Substring(3);
                int endIndex = content.LastIndexOf("```");
                if (endIndex > 0)
                    content = content.Substring(0, endIndex);
            }
            
            content = content.Trim();

            if (enableDebugLogs)
                Debug.Log($"[GPTLanguageService] Parsed content: {content}");

            // Parse the language data from content
            var languageData = JsonUtility.FromJson<LanguageDataJson>(content);
            
            return new LanguageResponse
            {
                spanishTranslation = languageData.spanishTranslation,
                simpleSentenceSpanish = languageData.simpleSentenceSpanish,
                simpleSentenceEnglish = languageData.simpleSentenceEnglish,
                positionalSentenceSpanish = languageData.positionalSentenceSpanish,
                positionalSentenceEnglish = languageData.positionalSentenceEnglish,
                success = true,
                errorMessage = null
            };
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GPTLanguageService] Parse error: {ex.Message}");
            return new LanguageResponse 
            { 
                success = false, 
                errorMessage = $"Parse error: {ex.Message}" 
            };
        }
    }

    // Helper classes for JSON parsing
    [Serializable]
    private class OpenAIResponse
    {
        public Choice[] choices;
    }

    [Serializable]
    private class Choice
    {
        public Message message;
    }

    [Serializable]
    private class Message
    {
        public string content;
    }

    [Serializable]
    private class LanguageDataJson
    {
        public string spanishTranslation;
        public string simpleSentenceSpanish;
        public string simpleSentenceEnglish;
        public string positionalSentenceSpanish;
        public string positionalSentenceEnglish;
    }

    /// <summary>
    /// Set API key at runtime (more secure than Inspector)
    /// </summary>
    public void SetAPIKey(string key)
    {
        apiKey = key;
    }

    /// <summary>
    /// Test the service with sample data
    /// </summary>
    [ContextMenu("Test API Call")]
    public void TestAPICall()
    {
        GetLanguageData("laptop", "office", (response) =>
        {
            if (response.success)
            {
                Debug.Log($"✓ Spanish: {response.spanishTranslation}");
                Debug.Log($"✓ Simple (ES): {response.simpleSentenceSpanish}");
                Debug.Log($"✓ Simple (EN): {response.simpleSentenceEnglish}");
                Debug.Log($"✓ Position (ES): {response.positionalSentenceSpanish}");
                Debug.Log($"✓ Position (EN): {response.positionalSentenceEnglish}");
            }
            else
            {
                Debug.LogError($"✗ Error: {response.errorMessage}");
            }
        });
    }
}

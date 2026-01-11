using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System;

[System.Serializable]
public class LLMOptions
{
    public string welcome;
    public string[] options = new string[3];
    public bool isReady = false;
    public string rawResponse = "";  // 儲存原始 LLM 回應（用於純文字生成的情況）
}

public class LLMOptionGenerator : MonoBehaviour
{
    public LLMOptions generatedOptions = new LLMOptions();

    // private string apiKey = "AIzaSyAQTdy1nEgjCKzqhNCsw82YdOjOo70J0E8";
    private string apiKey = "AIzaSyBzvAm3r9qjbeK88BISlEeggH7XbY1oVJ4";
    
    private string model = "gemini-2.5-flash";

    // System prompt - therapist persona
    private string systemPrompt = LLMPromptManager.GetSystemPrompt();

    void Start()
    {
        // 初始化日誌系統
        LLMLogger.Initialize();
    }

    public IEnumerator GenerateOptionsAsync(string userPrompt, Action<bool> callback = null)
    {
        generatedOptions.isReady = false;

        yield return StartCoroutine(CallGeminiAPI(userPrompt));

        if (callback != null)
        {
            callback.Invoke(generatedOptions.isReady);
        }
    }

    private IEnumerator CallGeminiAPI(string userPrompt)
    {
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        // Combine system prompt and user prompt
        string combinedPrompt = $"{systemPrompt}\n\n{userPrompt}";

        // Create request payload
        var requestBody = new GeminiRequest
        {
            contents = new[]
            {
                new Content
                {
                    parts = new[] { new Part { text = combinedPrompt } }
                }
            },
            generationConfig = new GenerationConfig
            {
                maxOutputTokens = 8192,
                temperature = 0.7f
            }
        };

        string jsonBody = JsonUtility.ToJson(requestBody);
        byte[] rawBody = Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(rawBody);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                GeminiResponse response = JsonUtility.FromJson<GeminiResponse>(jsonResponse);

                if (response.candidates != null && response.candidates.Length > 0)
                {
                    var candidate = response.candidates[0];
                    if (candidate.content != null && candidate.content.parts != null && candidate.content.parts.Length > 0)
                    {
                        string generatedText = candidate.content.parts[0].text;

                        // 記錄 Prompt 和回應到日誌
                        LLMLogger.LogLLMCall(combinedPrompt, generatedText);

                        ParseOptions(generatedText);
                    }
                    else
                    {
                        Debug.LogError($"Gemini API response structure is invalid. Raw Response: {jsonResponse}");
                        LLMLogger.LogLLMCall(combinedPrompt, $"[ERROR] Invalid response structure. Raw: {jsonResponse}");
                        SetDefaultOptions();
                    }
                }
                else
                {
                    Debug.LogError("No candidates in Gemini API response");
                    LLMLogger.LogLLMCall(combinedPrompt, "[ERROR] No candidates in response");
                    SetDefaultOptions();
                }
            }
            else
            {
                Debug.LogError("Error calling Gemini API: " + request.error);
                LLMLogger.LogLLMCall(combinedPrompt, $"[ERROR] API Call Failed: {request.error}");
                SetDefaultOptions();
            }
        }
    }
    private void ParseOptions(string responseText)
    {
        try
        {
            // 儲存原始回應，用於純文字情況
            generatedOptions.rawResponse = responseText.Trim();

            string[] lines = responseText.Split(new char[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

            int parsedCount = 0;
            generatedOptions.options = new string[3];
            bool hasFormattedResponse = false;

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine)) continue;

                if (trimmedLine.StartsWith("OPTION_1|"))
                {
                    generatedOptions.options[0] = trimmedLine.Replace("OPTION_1|", "").Trim();
                    parsedCount++;
                    hasFormattedResponse = true;
                }
                else if (trimmedLine.StartsWith("OPTION_2|"))
                {
                    generatedOptions.options[1] = trimmedLine.Replace("OPTION_2|", "").Trim();
                    parsedCount++;
                }
                else if (trimmedLine.StartsWith("OPTION_3|"))
                {
                    generatedOptions.options[2] = trimmedLine.Replace("OPTION_3|", "").Trim();
                    parsedCount++;
                }
                else if (trimmedLine.StartsWith("WELCOME|"))
                {
                    generatedOptions.welcome = trimmedLine.Replace("WELCOME|", "").Trim();
                }
            }

            // 如果找到完整的格式化回應（3個選項 + welcome）
            if (parsedCount == 3 && !string.IsNullOrEmpty(generatedOptions.welcome))
            {
                generatedOptions.isReady = true;
                Debug.Log("[LLMOptionGenerator] Successfully parsed formatted options");
            }
            // 如果沒有找到格式化的回應，使用原始文本作為 welcome（純文字模式）
            else if (!hasFormattedResponse && !string.IsNullOrEmpty(generatedOptions.rawResponse))
            {
                generatedOptions.welcome = generatedOptions.rawResponse;
                generatedOptions.isReady = true;
                Debug.Log("[LLMOptionGenerator] Using raw text response as welcome");
            }
            else
            {
                Debug.LogWarning("[LLMOptionGenerator] Failed to parse options, using defaults");
                SetDefaultOptions();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[LLMOptionGenerator] Parsing error: " + e.Message);
            SetDefaultOptions();
        }
    }

    private void SetDefaultOptions()
    {
        generatedOptions.welcome = "Welcome to HarmoSphere! Many students face various challenges. What's on your mind today?";
        generatedOptions.options[0] = "Academic pressure and exam anxiety";
        generatedOptions.options[1] = "Social isolation and loneliness";
        generatedOptions.options[2] = "Work-life balance and burnout";
        generatedOptions.isReady = true;
    }

    // JSON serialization classes
    [System.Serializable]
    private class GeminiRequest
    {
        public Content[] contents;
        public GenerationConfig generationConfig;
    }

    [System.Serializable]
    private class GenerationConfig
    {
        public int maxOutputTokens = 8192; // 增加 Token 限制，避免 MAX_TOKENS 錯誤
        public float temperature = 0.7f;
    }    

    [System.Serializable]
    private class Content
    {
        public Part[] parts;
    }

    [System.Serializable]
    private class Part
    {
        public string text;
    }

    [System.Serializable]
    private class GeminiResponse
    {
        public Candidate[] candidates;
    }

    [System.Serializable]
    private class Candidate
    {
        public Content content;
    }
}

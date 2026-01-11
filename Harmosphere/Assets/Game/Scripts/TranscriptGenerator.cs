using System.Collections;
using System.Text; // Required for encoding
using UnityEngine;
using UnityEngine.Networking; // Required for UnityWebRequest
using TMPro; // Add this if you are using UI Text

public class TranscriptGenerator : MonoBehaviour
{
    // public TextMeshProUGUI transcriptText; // Use TextMeshProUGUI for UI
    public string transcriptText;

    private string apiKey = "AIzaSyAQTdy1nEgjCKzqhNCsw82YdOjOo70J0E8"; 
    
    private string model = "gemini-2.5-flash"; // Or any other generative model

    private string prompt = "Generate a inspiring paragraph that encourage people and make people feel relax and confident, about 10 second long";

    public string GenerateTranscript()
    {
        // Display a "loading" message
        transcriptText = "Generating...";
        // transcriptText.text = "Generating...";
        
        // Start the API call as a Coroutine
        StartCoroutine(CallGeminiAPI());
        return transcriptText;
    }

    /// <summary>
    /// This Coroutine handles the web request to the Gemini API
    /// </summary>
    private IEnumerator CallGeminiAPI()
    {
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        // 1. Create the Request Body (JSON)
        // We need to format the request in the way the Gemini API expects
        var requestBody = new GeminiRequest
        {
            contents = new[]
            {
                new Content
                {
                    parts = new[] { new Part { text = this.prompt } }
                }
            }
        };
        string jsonBody = JsonUtility.ToJson(requestBody);
        byte[] rawBody = Encoding.UTF8.GetBytes(jsonBody);

        // 2. Create the UnityWebRequest
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(rawBody);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // 3. Send the request and wait for a response
            yield return request.SendWebRequest();

            // 4. Handle the response
            if (request.result == UnityWebRequest.Result.Success)
            {
                // Success! Parse the JSON response
                string jsonResponse = request.downloadHandler.text;
                GeminiResponse response = JsonUtility.FromJson<GeminiResponse>(jsonResponse);
                
                // Extract the generated text and update the UI
                string generatedText = response.candidates[0].content.parts[0].text;
                transcriptText = generatedText;
            }
            else
            {
                // Error! Log the error and update the UI
                Debug.LogError("Error calling Gemini API: " + request.error);
                Debug.LogError("Response: " + request.downloadHandler.text);
                transcriptText = "Error: Could not generate text.";
            }
        }
    }

    // --- C# Classes for JSON Serialization/Deserialization ---
    // These classes match the structure of the Gemini API's JSON

    [System.Serializable]
    private class GeminiRequest
    {
        public Content[] contents;
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
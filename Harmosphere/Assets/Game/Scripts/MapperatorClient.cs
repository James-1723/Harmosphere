using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class MapperatorClient : MonoBehaviour
{
    private const string ApiUrl = "http://34.69.37.250:7860/upload_infer";

    [Header("API Settings")]
    [Tooltip("安全的 API Key，不含特殊字元（可於 Inspector 設定）。")]
    [SerializeField] private string apiKey = "YOUR_API_KEY";

    [Header("Output Settings")]
    [Tooltip("產生的 .osu 檔案儲存目錄（相對於專案根目錄）。")]
    [SerializeField] private string saveDirectory = "Assets/StreamingAssets";

    /// <summary>
    /// 上傳 MP3 並下載對應的 .osu 檔案。
    /// </summary>
    public void GenerateBeatmap(string audioFilePath, Action<string> onSuccess = null, Action<string> onError = null)
    {
        StartCoroutine(UploadAudioCoroutine(audioFilePath, onSuccess, onError));
    }

    private IEnumerator UploadAudioCoroutine(string audioFilePath, Action<string> onSuccess, Action<string> onError)
    {
        if (!File.Exists(audioFilePath))
        {
            string errorMsg = $"Audio file not found at: {audioFilePath}";
            Debug.LogError(errorMsg);
            onError?.Invoke(errorMsg);
            yield break;
        }

        Debug.Log($"[MapperatorClient] Starting upload for: {audioFilePath}");

        byte[] fileData = File.ReadAllBytes(audioFilePath);
        string fileName = Path.GetFileName(audioFilePath);
        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        string savePath = Path.Combine(saveDirectory, $"{fileNameWithoutExt}.osu");

        string dir = Path.GetDirectoryName(savePath);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", fileData, fileName, "audio/mpeg");

        using (UnityWebRequest www = UnityWebRequest.Post(ApiUrl, form))
        {
            www.timeout = 120;

            if (!string.IsNullOrEmpty(apiKey))
            {
                www.SetRequestHeader("x-api-key", apiKey.Trim());
            }

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                string errorMsg = $"[MapperatorClient] API Error: {www.error}\nResponse Code: {www.responseCode}\nResponse: {www.downloadHandler.text}";
                Debug.LogError(errorMsg);
                onError?.Invoke(errorMsg);
            }
            else
            {
                try
                {
                    File.WriteAllBytes(savePath, www.downloadHandler.data);
                    Debug.Log($"[MapperatorClient] Beatmap saved to: {savePath}");
                    onSuccess?.Invoke(savePath);
                }
                catch (Exception e)
                {
                    string errorMsg = $"[MapperatorClient] File Write Error: {e.Message}";
                    Debug.LogError(errorMsg);
                    onError?.Invoke(errorMsg);
                }
            }
        }
    }

    [ContextMenu("Test Upload (Select File in Inspector)")]
    public void TestUpload()
    {
        Debug.Log("Call GenerateBeatmap() with a valid file path to test.");
    }
}

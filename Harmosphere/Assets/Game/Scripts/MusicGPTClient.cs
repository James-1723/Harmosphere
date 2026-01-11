using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 精簡版 MusicGPT API 客戶端 (優化版)
/// 包含防重複觸發鎖定、智慧重試與延遲輪詢機制
/// </summary>
public class MusicGPTClient : MonoBehaviour
{
    [Header("API Settings")]
    // 請在此填入你的 API Key
    [SerializeField] private string apiKey = "7i_G-KWX9DC9jkbhQobVulC47sOOvcGKphT5YMu11Tlj14KFU2kWZdYhCLaSVxkMQi2SSb_1TyGGaRt7I_zocA";
    [SerializeField] private string outputDirectory = "GeneratedMusic";
    
    [Header("Polling Settings")]
    [SerializeField] private float pollInterval = 5f;       // 每次查詢間隔
    [SerializeField] private float pollTimeout = 600f;      // 最長等待生成時間
    [SerializeField] private float initialPollDelay = 3f;   // 提交後等待多久才開始第一次查詢 (重要防 429)
    
    [Header("Retry Settings")]
    [SerializeField] private int maxRetries = 3;
    [SerializeField] private float[] retryDelays = { 5f, 15f, 30f };

    [Header("Music Settings")]
    [SerializeField] private RhythmGameManager rhythmGameManager;
    
    // API Endpoints
    private const string BASE_URL = "https://api.musicgpt.com/api/public/v1";
    private const string URL_MUSICAI = BASE_URL + "/MusicAI";
    private const string URL_BYID = BASE_URL + "/byId";
    
    // Events
    public event Action<string> OnStatusUpdate;
    public event Action<string, float> OnAudioReady; // (filePath, duration)
    public event Action<string> OnError;
    
    // 狀態鎖：防止連點或重複觸發
    private bool isGenerating = false; 
    
    #region Data Classes
    
    [Serializable]
    private class SubmitRequest
    {
        public string music_style;
    }
    
    [Serializable]
    private class SubmitResponse
    {
        public bool success;
        public string task_id;
    }
    
    [Serializable]
    private class PollResponse
    {
        public ConversionData conversion;
        public string status;
    }
    
    [Serializable]
    private class ConversionData
    {
        public string status;
        public string conversion_path_1;
        public string conversion_path_2;
        public string audio_url;
        public float conversion_duration_1;
        public float conversion_duration_2;
        public float conversion_duration;
    }
    
    private class CoroutineResult<T>
    {
        public T value;
        public string error;
    }
    
    #endregion
    
    /// <summary>
    /// 根據遊戲數據生成音樂（45-60秒）
    /// </summary>
    public void GenerateMusic(int scoreGet, int comboGet)
    {
        // 1. 檢查是否正在生成中
        if (isGenerating)
        {
            Debug.LogWarning("[MusicGPT] 上一個生成任務仍在進行中，忽略本次請求。");
            OnStatusUpdate?.Invoke("系統忙碌中，請稍候...");
            return;
        }

        StartCoroutine(GenerateMusicCoroutine(scoreGet, comboGet));
    }
    
    private IEnumerator GenerateMusicCoroutine(int scoreGet, int comboGet)
    {
        isGenerating = true; // 鎖定狀態
        
        // 生成 prompt
        string prompt = GeneratePrompt(scoreGet, comboGet);
        OnStatusUpdate?.Invoke("開始生成音樂...");
        Debug.Log($"[MusicGPT] 開始生成音樂: {prompt}");
        
        // 1) 提交生成請求
        var submitResult = new CoroutineResult<SubmitResponse>();
        yield return SubmitMusicRequest(prompt, submitResult);
        
        if (submitResult.error != null)
        {
            OnError?.Invoke($"提交失敗: {submitResult.error}");
            Debug.LogError($"[MusicGPT] 提交失敗: {submitResult.error}");
            isGenerating = false; // 解鎖
            yield break;
        }
        
        string taskId = submitResult.value.task_id;
        OnStatusUpdate?.Invoke($"任務已提交 (Task ID: {taskId})");
        Debug.Log($"[MusicGPT] 任務已提交 (Task ID: {taskId})");
        
        // 新增：緩衝時間，避免提交後瞬間查詢導致 429
        yield return new WaitForSeconds(initialPollDelay);
        
        // 2) 輪詢直到完成
        var pollResult = new CoroutineResult<ConversionData>();
        yield return PollUntilComplete(taskId, pollResult);
        
        if (pollResult.error != null)
        {
            OnError?.Invoke($"生成失敗: {pollResult.error}");
            Debug.LogError($"[MusicGPT] 生成失敗: {pollResult.error}");
            isGenerating = false; // 解鎖
            yield break;
        }
        
        // 3) 獲取音頻 URL 和時長
        var (audioUrl, duration) = PickAudioFromConversion(pollResult.value);
        OnStatusUpdate?.Invoke($"音樂生成完成: {duration:F1}秒，準備下載...");
        Debug.Log($"[MusicGPT] 音樂生成完成: {duration:F1}秒");

        // 4) 下載音樂檔案
        var downloadResult = new CoroutineResult<string>();
        yield return DownloadAudio(audioUrl, downloadResult);
        
        if (downloadResult.error != null)
        {
            OnError?.Invoke($"下載失敗: {downloadResult.error}");
            Debug.LogError($"[MusicGPT] 下載失敗: {downloadResult.error}");
        }
        else
        {
            string filePath = downloadResult.value;
            OnStatusUpdate?.Invoke($"✅ 完成！已儲存至: {filePath}");
            Debug.Log($"[MusicGPT] ✅ 完成！已儲存至: {filePath}");
            OnAudioReady?.Invoke(filePath, duration);
        }

        isGenerating = false; // 全部完成後解鎖
    }
    
    private string GeneratePrompt(int scoreGet, int comboGet)
    {
        int targetSeconds = UnityEngine.Random.Range(180, 200); 
        string mood;

        // float scoreRatio = (float)scoreGet / rhythmGameManager.GetSongMaxScoreAndCombo().maxScore;
        // int bpm = (int)(160 * scoreRatio);
        // int combo = (int)((float)comboGet / rhythmGameManager.GetSongMaxScoreAndCombo().maxCombo * 100);

        // for test
        int combo = 20;
        int bpm = 160;
        float scoreRatio = 1;
        
        switch (combo){
            case >=66:
                mood = "intense and energetic";
                break;
            case >=33:
                mood = "stable and focused";
                break;
            default:
                mood = "relaxed and calm";
                break;
        }
        Debug.Log($"scoreGet: {scoreGet}, comboGet: {comboGet}, scoreRatio: {scoreRatio}, bpm: {bpm}, combo: {combo}, mood: {mood}");

        return $"Create a {targetSeconds}-second {mood} classical/ambient instrumental track at {bpm} BPM. " +
               $"Style: piano with atmospheric elements.";
    }
    
    #region API Methods
    
    private IEnumerator SubmitMusicRequest(string musicStyle, CoroutineResult<SubmitResponse> result)
    {
        SubmitRequest requestData = new SubmitRequest
        {
            music_style = SquashDescription(musicStyle, 270)
        };
        
        string jsonData = JsonUtility.ToJson(requestData);
        
        // 重試機制 loop
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            using (UnityWebRequest www = new UnityWebRequest(URL_MUSICAI, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("Authorization", apiKey);
                www.SetRequestHeader("Accept", "application/json");
                
                // 優化：增加 Timeout 為 60 秒，避免伺服器處理慢時 Unity 誤判斷線
                www.timeout = 60;
                
                yield return www.SendWebRequest();
                
                // 處理 429 Too Many Requests
                if (www.responseCode == 429)
                {
                    if (attempt < maxRetries)
                    {
                        float delay = (attempt < retryDelays.Length) ? retryDelays[attempt] : 10f * (attempt + 1);
                        OnStatusUpdate?.Invoke($"請求頻繁 (429)，等待 {delay} 秒後重試... ({attempt + 1}/{maxRetries})");
                        Debug.LogWarning($"[MusicGPT] 429 Error, waiting {delay}s to retry.");
                        yield return new WaitForSeconds(delay);
                        continue; // 重試
                    }
                    else
                    {
                        result.error = "請求過於頻繁，請稍後再試";
                        yield break;
                    }
                }
                
                if (www.result != UnityWebRequest.Result.Success)
                {
                    // 如果是其他網路錯誤，紀錄錯誤並結束 (或視需求決定是否重試 500 錯誤)
                    result.error = $"HTTP {www.responseCode}: {www.error}";
                    yield break;
                }
                else
                {
                    try
                    {
                        result.value = JsonUtility.FromJson<SubmitResponse>(www.downloadHandler.text);
                        
                        // 雙重檢查
                        if (result.value == null || string.IsNullOrEmpty(result.value.task_id))
                        {
                            result.error = "API 回傳成功但沒有 Task ID";
                            yield break;
                        }
                        
                        yield break; // 成功，退出重試迴圈
                    }
                    catch (Exception e)
                    {
                        result.error = $"JSON 解析失敗: {e.Message}";
                        yield break;
                    }
                }
            }
        }
        
        if (result.error == null) result.error = "所有重試嘗試都失敗了";
    }
    
    private IEnumerator PollUntilComplete(string taskId, CoroutineResult<ConversionData> result)
    {
        float startTime = Time.time;
        
        while (Time.time - startTime < pollTimeout)
        {
            string url = URL_BYID + $"?conversionType=MUSIC_AI&task_id={taskId}";
            
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                www.SetRequestHeader("Authorization", apiKey);
                www.SetRequestHeader("Accept", "application/json");
                www.timeout = 30;
                
                yield return www.SendWebRequest();
                
                // 處理輪詢時的 429：不需要報錯，只需要等待久一點
                if (www.responseCode == 429)
                {
                    OnStatusUpdate?.Invoke("查詢過快，延長等待時間...");
                    Debug.LogWarning("[MusicGPT] Polling 429, waiting longer.");
                    yield return new WaitForSeconds(pollInterval * 2); // 雙倍等待時間
                    continue;
                }
                
                if (www.result != UnityWebRequest.Result.Success)
                {
                    // 輪詢過程中的短暫網路錯誤可以容忍，不直接 break
                    Debug.LogWarning($"[MusicGPT] Polling error: {www.error}, retrying next cycle.");
                }
                else
                {
                    string responseText = www.downloadHandler.text;
                    try
                    {
                        PollResponse response = JsonUtility.FromJson<PollResponse>(responseText);
                        ConversionData conv = response.conversion;
                        
                        if (conv == null)
                        {
                            // 兼容舊格式或直接格式
                            conv = JsonUtility.FromJson<ConversionData>(responseText);
                        }
                        
                        string status = (conv?.status ?? response.status ?? "").ToUpper();
                        OnStatusUpdate?.Invoke($"生成狀態: {status}");
                        
                        if (status == "COMPLETED")
                        {
                            result.value = conv;
                            yield break;
                        }
                        else if (status == "FAILED" || status == "ERROR")
                        {
                            result.error = "音樂生成失敗 (Server returned FAILED)";
                            yield break;
                        }
                        // PENDING 或 PROCESSING 會繼續迴圈
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[MusicGPT] 解析回應失敗: {e.Message}");
                    }
                }
            }
            
            yield return new WaitForSeconds(pollInterval);
        }
        
        result.error = "等待生成超時 (Timeout)";
    }
    
    private IEnumerator DownloadAudio(string audioUrl, CoroutineResult<string> result)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(audioUrl))
        {
            www.timeout = 180; // 下載給予較長時間
            yield return www.SendWebRequest();
            
            if (www.result != UnityWebRequest.Result.Success)
            {
                result.error = www.error;
                yield break;
            }
            
            try
            {
                string directory = Path.Combine(Application.persistentDataPath, outputDirectory);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                
                // 判斷副檔名
                string ext = audioUrl.ToLower().EndsWith(".mp3") ? ".mp3" : 
                            (audioUrl.ToLower().EndsWith(".wav") ? ".wav" : ".bin");
                
                string filename = $"music_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{ext}";
                string filePath = Path.Combine(directory, filename);
                
                File.WriteAllBytes(filePath, www.downloadHandler.data);
                result.value = filePath;
            }
            catch (Exception e)
            {
                result.error = $"儲存檔案失敗: {e.Message}";
            }
        }
    }
    
    #endregion
    
    #region Helper Methods
    
    private string SquashDescription(string desc, int limit = 270)
    {
        desc = Regex.Replace(desc, @"[`*_#>]", " ");
        desc = Regex.Replace(desc, @"\s+", " ").Trim();
        return desc.Length <= limit ? desc : desc.Substring(0, limit).Trim();
    }
    
    private (string, float) PickAudioFromConversion(ConversionData conv)
    {
        // 優先選擇最長的版本 (有些API會回傳預覽版和完整版)
        if (!string.IsNullOrEmpty(conv.conversion_path_1) || !string.IsNullOrEmpty(conv.conversion_path_2))
        {
            float d1 = conv.conversion_duration_1;
            float d2 = conv.conversion_duration_2;
            
            if (d2 > d1 && !string.IsNullOrEmpty(conv.conversion_path_2))
                return (conv.conversion_path_2, Mathf.Max(d2, 0f));
            if (!string.IsNullOrEmpty(conv.conversion_path_1))
                return (conv.conversion_path_1, Mathf.Max(d1, 0f));
        }
        
        // 備選：使用 audio_url
        string url = conv.audio_url;
        if (string.IsNullOrEmpty(url))
            throw new Exception("在回應中找不到音頻 URL");
        
        float duration = conv.conversion_duration > 0 ? conv.conversion_duration : 0f;
        return (url, duration);
    }
    
    #endregion
}
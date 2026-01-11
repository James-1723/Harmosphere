using System.Collections;
using System.Text;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json; // 記得安裝 Newtonsoft.Json

public class SunoMusicGenerator : MonoBehaviour
{
    [Header("API Settings")]
    // 請注意資安，實際專案不要直接把 Key 寫在 Inspector
    public string apiKey = "1d299998a77ac15263095b88643e29b2"; 
    
    // 根據 Suno API 官方文檔的正確端點
    private string generateUrl = "https://api.sunoapi.org/api/v1/generate";
    private string statusUrl = "https://api.sunoapi.org/api/v1/generate/record-info"; 

    [Header("Save Settings")]
    public string saveFolder = "GeneratedMusic";

    [ContextMenu("Test Generate Music")]
    public void TestGeneration()
    {
        StartCoroutine(GenerateMusicRoutine());
    }

    IEnumerator GenerateMusicRoutine()
    {
        // 1. 準備請求資料 (Payload)
        var requestData = new
        {
            prompt = "A 200-seconds calm and relaxing piano track with soft melodies and 60 bpm",
            style = "Classical",
            title = "Peaceful Piano Meditation",
            customMode = true,
            instrumental = true,
            model = "V3_5",
            negativeTags = "Heavy Metal, Upbeat Drums",
            callBackUrl = "https://api.sunoapi.org/api/v1/get_task_ids"
            // 由於我們改用輪詢，callBackUrl 可以留空或移除
            // callBackUrl = "" 
        };

        string jsonBody = JsonConvert.SerializeObject(requestData);

        // 2. 發送 POST 請求
        using (UnityWebRequest request = new UnityWebRequest(generateUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            Debug.Log("正在發送生成請求...");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"生成請求失敗: {request.error}\n{request.downloadHandler.text}");
            }
            else
            {
                Debug.Log($"生成請求成功: {request.downloadHandler.text}");
                
                // 解析回應取得 Task ID
                var response = JsonConvert.DeserializeObject<SunoGenerateResponse>(request.downloadHandler.text);
                
                if (response != null && response.data != null)
                {
                    string taskId = response.data.taskId; // API 回傳為 taskId (camelCase)
                    Debug.Log($"取得 Task ID: {taskId}，開始輪詢狀態...");
                    
                    // 3. 開始輪詢 (Polling) 直到完成
                    yield return StartCoroutine(PollForCompletion(taskId));
                }
            }
        }
    }

    IEnumerator PollForCompletion(string taskId)
    {
        bool isCompleted = false;
        int maxRetries = 60; // 防止無窮迴圈，設定最大嘗試次數 (例如 5分鐘)
        int currentTry = 0;

        Debug.Log($"[輪詢] 開始輪詢 Task ID: {taskId}");

        while (!isCompleted && currentTry < maxRetries)
        {
            yield return new WaitForSeconds(5f); // 每 5 秒檢查一次
            currentTry++;

            // 嘗試使用 GET 請求帶 taskId 參數（不同的參數名）
            string url = $"{statusUrl}?taskId={taskId}";
            Debug.Log($"[輪詢 {currentTry}/{maxRetries}] 查詢 URL: {url}");

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", "Bearer " + apiKey);
                yield return request.SendWebRequest();

                Debug.Log($"[輪詢] 請求結果: {request.result}");

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = request.downloadHandler.text;
                    Debug.Log($"[輪詢] 收到回應: {jsonResponse}");

                    // 嘗試解析為陣列格式
                    SunoStatusResponse statusData = null;
                    SunoStatusResponseSingle statusDataSingle = null;
                    TaskData taskData = null;
                    
                    try
                    {
                        // 先嘗試解析為陣列格式
                        statusData = JsonConvert.DeserializeObject<SunoStatusResponse>(jsonResponse);
                        Debug.Log($"[輪詢] JSON 解析成功（陣列格式）");
                        Debug.Log($"[輪詢] statusData.code: {statusData?.code ?? -1}");
                        Debug.Log($"[輪詢] statusData.msg: {statusData?.msg ?? "null"}");
                        Debug.Log($"[輪詢] dataList 是否為 null: {statusData?.dataList == null}");
                        Debug.Log($"[輪詢] dataList.Count: {statusData?.dataList?.Count ?? 0}");
                        
                        if (statusData != null && statusData.dataList != null && statusData.dataList.Count > 0)
                        {
                            // 從陣列中找到對應的 taskId
                            foreach (var task in statusData.dataList)
                            {
                                if (task.taskId == taskId)
                                {
                                    taskData = task;
                                    Debug.Log($"[輪詢] 從陣列中找到對應的任務");
                                    break;
                                }
                            }
                            
                            if (taskData == null)
                            {
                                Debug.LogWarning($"[輪詢] 陣列中找不到 taskId: {taskId}，使用第一個任務");
                                taskData = statusData.dataList[0];
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[輪詢] 陣列格式解析失敗: {ex.Message}，嘗試單個物件格式");
                    }
                    
                    // 如果陣列格式失敗，嘗試單個物件格式
                    if (taskData == null)
                    {
                        try
                        {
                            statusDataSingle = JsonConvert.DeserializeObject<SunoStatusResponseSingle>(jsonResponse);
                            Debug.Log($"[輪詢] JSON 解析成功（單個物件格式）");
                            Debug.Log($"[輪詢] statusDataSingle.code: {statusDataSingle?.code ?? -1}");
                            Debug.Log($"[輪詢] statusDataSingle.msg: {statusDataSingle?.msg ?? "null"}");
                            Debug.Log($"[輪詢] data 是否為 null: {statusDataSingle?.data == null}");
                            
                            if (statusDataSingle != null && statusDataSingle.data != null)
                            {
                                taskData = statusDataSingle.data;
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"[輪詢] 單個物件格式也解析失敗: {ex.Message}");
                            continue;
                        }
                    }
                    
                    // 處理任務資料
                    if (taskData != null)
                    {
                        Debug.Log($"[輪詢] Task ID: {taskData.taskId}");
                        Debug.Log($"[輪詢] Task Status: {taskData.status}");
                        Debug.Log($"[輪詢] Error Message: {taskData.errorMessage ?? "null"}");
                        Debug.Log($"[輪詢] response 是否為 null: {taskData.response == null}");
                        
                        // 檢查狀態（可能是 SUCCESS 或 TEXT_SUCCESS）
                        bool isSuccess = taskData.status == "SUCCESS" || taskData.status == "TEXT_SUCCESS" || taskData.status.Contains("SUCCESS");
                        
                        if (isSuccess && taskData.response != null && taskData.response.sunoData != null && taskData.response.sunoData.Count > 0)
                        {
                            var track = taskData.response.sunoData[0];
                            
                            Debug.Log($"[輪詢] Track ID: {track.id}");
                            Debug.Log($"[輪詢] Track Title: {track.title}");
                            Debug.Log($"[輪詢] Audio URL: {track.audioUrl ?? "null"}");
                            Debug.Log($"[輪詢] Stream Audio URL: {track.streamAudioUrl ?? "null"}");
                            Debug.Log($"[輪詢] Duration: {track.duration?.ToString() ?? "null"}");
                            
                            // 優先使用 audioUrl，如果沒有則使用 streamAudioUrl
                            string downloadUrl = !string.IsNullOrEmpty(track.audioUrl) ? track.audioUrl : track.streamAudioUrl;
                            
                            if (!string.IsNullOrEmpty(downloadUrl))
                            {
                                isCompleted = true;
                                Debug.Log($"音樂生成完成！開始下載... URL: {downloadUrl}");
                                yield return StartCoroutine(DownloadAudio(downloadUrl, track.title, taskId));
                            }
                            else
                            {
                                Debug.Log($"音樂還在處理中，audioUrl 和 streamAudioUrl 都為空 ({currentTry}/{maxRetries})");
                            }
                        }
                        else if (taskData.status == "FAILED" || taskData.status == "ERROR")
                        {
                            Debug.LogError($"音樂生成失敗: {taskData.errorMessage}");
                            yield break;
                        }
                        else
                        {
                            Debug.Log($"生成中... 狀態: {taskData.status} ({currentTry}/{maxRetries})");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[輪詢] 無法解析回應或 data 為空");
                    }
                }
                else
                {
                    Debug.LogError($"[輪詢] 請求失敗: {request.error}\n回應: {request.downloadHandler.text}");
                }
            }
        }

        if (currentTry >= maxRetries)
        {
            Debug.LogError($"[輪詢] 達到最大重試次數 ({maxRetries})，輪詢結束");
        }
    }

    IEnumerator DownloadAudio(string url, string title, string id)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            Debug.Log($"下載音檔中: {url}");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // 設定存檔路徑
                // 建議存到 Application.persistentDataPath (跨平台安全路徑)
                // 或是專案內的 Assets/StreamingAssets (僅限編輯器)
                
                string folderPath = Path.Combine(Application.dataPath, saveFolder); 
                // 如果你想存到專案外，可以改用:
                // string folderPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyMusic), "SunoGen");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // 清理檔名中的非法字元
                string safeTitle = string.Join("_", title.Split(Path.GetInvalidFileNameChars()));
                string fileName = $"{safeTitle}_{id}.mp3";
                string fullPath = Path.Combine(folderPath, fileName);

                // 寫入檔案
                File.WriteAllBytes(fullPath, request.downloadHandler.data);

                Debug.Log($"<color=green>下載成功！檔案已儲存至: {fullPath}</color>");
                
                // (選用) 如果你想直接在 Unity 播放，可以轉換成 AudioClip
                // AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
            }
            else
            {
                Debug.LogError($"下載失敗: {request.error}");
            }
        }
    }

    // --- JSON 資料結構 (根據 Suno API 回傳定義) ---

    // 用於解析 Generate 的回應
    public class SunoGenerateResponse
    {
        [JsonProperty("code")] public int code;
        [JsonProperty("msg")] public string msg;
        [JsonProperty("data")] public GenerateData data;
    }

    public class GenerateData
    {
        [JsonProperty("taskId")] public string taskId;
    }

    // 用於解析 Get Status 的回應（根據官方文檔結構）
    // 注意：API 可能返回單個 TaskData 或 TaskData 陣列，我們先嘗試陣列
    public class SunoStatusResponse
    {
        [JsonProperty("code")] public int code;
        [JsonProperty("msg")] public string msg;
        [JsonProperty("data")] public System.Collections.Generic.List<TaskData> dataList;
    }
    
    // 如果 API 返回單個物件，使用這個類別
    public class SunoStatusResponseSingle
    {
        [JsonProperty("code")] public int code;
        [JsonProperty("msg")] public string msg;
        [JsonProperty("data")] public TaskData data;
    }

    public class TaskData
    {
        [JsonProperty("taskId")] public string taskId;
        [JsonProperty("response")] public TaskResponse response;
        [JsonProperty("status")] public string status; // SUCCESS, PROCESSING, FAILED
        [JsonProperty("errorMessage")] public string errorMessage;
    }

    public class TaskResponse
    {
        [JsonProperty("sunoData")] public System.Collections.Generic.List<MusicTrack> sunoData;
    }

    public class MusicTrack
    {
        [JsonProperty("id")] public string id;
        [JsonProperty("title")] public string title;
        [JsonProperty("audioUrl")] public string audioUrl; // 注意是 audioUrl，不是 audio_url
        [JsonProperty("streamAudioUrl")] public string streamAudioUrl; // 流式音訊 URL
        [JsonProperty("imageUrl")] public string imageUrl;
        [JsonProperty("duration")] public double? duration; // 改為可空類型，因為生成中時為 null
    }
}
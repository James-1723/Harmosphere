using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;

[System.Serializable]
public class SkyboxRequest
{
    public string prompt;
    public int skybox_style_id = 10; // SciFi style
    public string webhook_url;
}

[System.Serializable]
public class SkyboxResponse
{
    public int id;
    public string status;
    public int queue_position;
    public string file_url;
    public string thumb_url;
    public string title;
    public int user_id;
    public string username;
    public string error_message;
    public string obfuscated_id;
    public string pusher_channel;
    public string pusher_event;
    public string created_at;
    public string updated_at;
}

[System.Serializable]
public class SkyboxRequestWrapper
{
    public SkyboxResponse request;
}

public class SkyboxAPI : MonoBehaviour
{
    public LoadingScreenManager loadingAnimation;
    public GameObject loadingScreen;

    [Header("API 設定")]
    [SerializeField] private string apiKey = "YOUR_API_KEY_HERE"; // 請替換為您的 API Key
    [SerializeField] private string baseUrl = "https://backend.blockadelabs.com/api/v1";
    
    [Header("Skybox 設定")]
    [SerializeField] private Material skyboxMaterial;
    [SerializeField] private bool autoApplyToScene = true;
    
    [Header("調試")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // 事件
    public event Action<SkyboxResponse> OnSkyboxGenerated;
    public event Action<string> OnGenerationStarted;
    public event Action<string> OnStatusUpdate;
    public event Action<string> OnError;
    
    private Dictionary<int, Coroutine> activeGenerations = new Dictionary<int, Coroutine>();
    private Dictionary<int, SkyboxResponse> generationResponses = new Dictionary<int, SkyboxResponse>();
    
    // 快取下載完成但尚未套用的 Skybox
    private Texture2D cachedTexture;
    private SkyboxResponse cachedResponse;
    
    void Start()
    {
        if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_API_KEY_HERE")
        {
            LogError("請在 Inspector 中設定您的 Blockade Labs API Key!");
        }
    }
    
    /// <summary>
    /// 生成新的 skybox
    /// </summary>
    /// <param name="prompt">描述 skybox 的文字提示</param>
    /// <param name="styleId">skybox 風格 ID (預設: 10 = SciFi)</param>
    public void GenerateSkybox(string prompt, int styleId = 10)
    {
        if (string.IsNullOrEmpty(prompt))
        {
            LogError("提示文字不能為空!");
            return;
        }
        
        if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_API_KEY_HERE")
        {
            LogError("請先設定 API Key!");
            return;
        }
        
        StartCoroutine(GenerateSkyboxCoroutine(prompt, styleId));
    }

    public void revealLoader(bool finishTranscript)
    {
        if (loadingAnimation != null)
        {
            loadingAnimation.RevealLoadingScreen();
        }
    }
    
    private IEnumerator GenerateSkyboxCoroutine(string prompt, int styleId)
    {
        LogDebug($"開始生成 skybox: {prompt}");
        OnGenerationStarted?.Invoke(prompt);
        
        // if (loadingAnimation != null)
        // {
        //     loadingScreen.SetActive(true);
        //     loadingAnimation.RevealLoadingScreen();
        // }

        // 建立請求資料
        SkyboxRequest request = new SkyboxRequest
        {
            prompt = prompt,
            skybox_style_id = styleId
        };
        
        string jsonData = JsonUtility.ToJson(request);
        
        // 建立 HTTP 請求
        using (UnityWebRequest webRequest = new UnityWebRequest($"{baseUrl}/skybox", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("x-api-key", apiKey);
            
            yield return webRequest.SendWebRequest();
            
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string responseText = webRequest.downloadHandler?.text;
                if (string.IsNullOrEmpty(responseText))
                {
                    LogError("API 回應為空");
                    OnError?.Invoke("API 回應為空");
                    yield break;
                }
                
                LogDebug($"API 回應: {responseText}");
                
                try
                {
                    SkyboxResponse response = JsonUtility.FromJson<SkyboxResponse>(responseText);
                    
                    if (response != null && response.id > 0)
                    {
                        LogDebug($"Skybox 生成已開始 (ID: {response.id})");
                        LogDebug($"Obfuscated ID: {response.obfuscated_id}");
                        string title = !string.IsNullOrEmpty(response.title) ? response.title : $"生成 #{response.id}";
                        OnStatusUpdate?.Invoke($"生成開始: {title}");
                        
                        // 儲存完整回應資訊
                        generationResponses[response.id] = response;
                        
                        // 開始追蹤生成進度
                        Coroutine trackingCoroutine = StartCoroutine(TrackGenerationProgress(response.id));
                        activeGenerations[response.id] = trackingCoroutine;
                    }
                    else
                    {
                        LogError("無法解析 API 回應或回應格式不正確");
                        LogError($"回應內容: {responseText}");
                        OnError?.Invoke("API 回應格式不正確");
                    }
                }
                catch (Exception e)
                {
                    LogError($"解析回應時發生錯誤: {e.Message}");
                    LogError($"回應內容: {responseText}");
                    OnError?.Invoke($"解析回應失敗: {e.Message}");
                }
            }
            else
            {
                LogError($"API 請求失敗: {webRequest.error}");
                OnError?.Invoke($"API 請求失敗: {webRequest.error}");
            }
        }
    }
    
    private IEnumerator TrackGenerationProgress(int generationId)
    {
        // 根據官方文件，狀態輪詢建議使用 imagine/requests/{id}
        string statusUrl = $"{baseUrl}/imagine/requests/{generationId}";
        bool isComplete = false;
        float pollInterval = 5f; // 每 5 秒檢查一次
        
        while (!isComplete)
        {
            yield return new WaitForSeconds(pollInterval);
            
            using (UnityWebRequest webRequest = UnityWebRequest.Get(statusUrl))
            {
                webRequest.SetRequestHeader("x-api-key", apiKey);
                
                yield return webRequest.SendWebRequest();
                
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string responseText = webRequest.downloadHandler?.text;
                    if (string.IsNullOrEmpty(responseText))
                    {
                        LogError("API 回應為空");
                        continue;
                    }
                    
                    LogDebug($"狀態檢查回應: {responseText}");
                    
                    bool needsAlternativeCheck = false;
                    
                    try
                    {
                        // 嘗試解析為 SkyboxRequestWrapper 或 SkyboxResponse
                        SkyboxResponse response = null;
                        try
                        {
                            var wrapped = JsonUtility.FromJson<SkyboxRequestWrapper>(responseText);
                            if (wrapped != null && wrapped.request != null)
                            {
                                response = wrapped.request;
                            }
                            else
                            {
                                response = JsonUtility.FromJson<SkyboxResponse>(responseText);
                            }
                        }
                        catch (Exception parseEx)
                        {
                            LogError($"JSON 解析失敗: {parseEx.Message}");
                            LogError($"嘗試解析的 JSON: {responseText}");
                            needsAlternativeCheck = true;
                        }
                        
                        if (response != null && !string.IsNullOrEmpty(response.status))
                        {
                            LogDebug($"狀態更新 (ID: {generationId}): {response.status}");
                            OnStatusUpdate?.Invoke($"狀態: {response.status}");
                            
                            string status = response.status.ToLower();
                            switch (status)
                            {
                                case "complete":
                                    LogDebug($"Skybox 生成完成! 圖片 URL: {response.file_url}");
                                    if (!string.IsNullOrEmpty(response.file_url))
                                    {
                                        StartCoroutine(DownloadAndApplySkybox(response));
                                    }
                                    else
                                    {
                                        LogError("生成完成但沒有圖片 URL");
                                        OnError?.Invoke("生成完成但沒有圖片 URL");
                                    }
                                    isComplete = true;
                                    break;
                                    
                                case "error":
                                    string errorMsg = !string.IsNullOrEmpty(response.error_message) 
                                        ? response.error_message 
                                        : "未知錯誤";
                                    LogError($"生成失敗: {errorMsg}");
                                    OnError?.Invoke($"生成失敗: {errorMsg}");
                                    isComplete = true;
                                    break;
                                    
                                case "abort":
                                case "aborted":
                                    LogError("生成被取消");
                                    OnError?.Invoke("生成被取消");
                                    isComplete = true;
                                    break;
                                    
                                case "pending":
                                case "dispatched":
                                case "processing":
                                    // 繼續等待
                                    LogDebug($"生成進行中: {status}");
                                    break;
                                    
                                default:
                                    LogDebug($"未知狀態: {status}，繼續等待...");
                                    break;
                            }
                        }
                        else
                        {
                            // 如果無法解析，標記需要備用檢查
                            LogError("標準格式解析失敗，嘗試檢查原始回應");
                            LogError($"API 回應: {responseText}");
                            
                            // 檢查是否包含 "complete" 等關鍵字
                            if (responseText.Contains("\"status\""))
                            {
                                LogDebug("回應包含狀態欄位，但格式可能不符預期");
                            }
                            
                            needsAlternativeCheck = true;
                        }
                    }
                    catch (Exception e)
                    {
                        LogError($"解析狀態回應時發生錯誤: {e.Message}");
                        LogError($"回應內容: {responseText}");
                        needsAlternativeCheck = true;
                    }
                    
                    // 在 try-catch 外執行備用檢查
                    if (needsAlternativeCheck)
                    {
                        yield return StartCoroutine(TryAlternativeStatusCheck(generationId));
                    }
                }
                else
                {
                    // 非 2xx，嘗試備用端點
                    LogError($"狀態檢查失敗: {webRequest.error}");
                    yield return StartCoroutine(TryAlternativeStatusCheck(generationId));
                }
            }
        }
        
        // 清理活動生成記錄
        if (activeGenerations.ContainsKey(generationId))
        {
            activeGenerations.Remove(generationId);
        }
        
        // 清理儲存的回應資料
        if (generationResponses.ContainsKey(generationId))
        {
            generationResponses.Remove(generationId);
        }
    }
    
    private IEnumerator TryAlternativeStatusCheck(int generationId)
    {
        // 嘗試使用 imagine 端點
        string alternativeUrl = $"{baseUrl}/imagine/requests/{generationId}";
        LogDebug($"嘗試備用狀態檢查: {alternativeUrl}");
        
        using (UnityWebRequest webRequest = UnityWebRequest.Get(alternativeUrl))
        {
            webRequest.SetRequestHeader("x-api-key", apiKey);
            yield return webRequest.SendWebRequest();
            
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string responseText = webRequest.downloadHandler?.text;
                LogDebug($"備用端點回應: {responseText}");
                
                if (!string.IsNullOrEmpty(responseText))
                {
                    try
                    {
                        SkyboxResponse response = JsonUtility.FromJson<SkyboxResponse>(responseText);
                        if (response != null && !string.IsNullOrEmpty(response.status))
                        {
                            LogDebug($"備用端點成功解析狀態: {response.status}");
                            
                            if (response.status.ToLower() == "complete" && !string.IsNullOrEmpty(response.file_url))
                            {
                                LogDebug("通過備用端點發現生成已完成");
                                StartCoroutine(DownloadAndApplySkybox(response));
                                yield break;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        LogError($"備用端點解析失敗: {e.Message}");
                    }
                }
            }
            else
            {
                LogError($"備用端點請求失敗: {webRequest.error}");
            }
        }
        
        // 如果備用方法也失敗，嘗試直接到 Blockade Labs 網站檢查
        if (generationResponses.ContainsKey(generationId))
        {
            var originalResponse = generationResponses[generationId];
            if (!string.IsNullOrEmpty(originalResponse.obfuscated_id))
            {
                LogDebug($"嘗試使用 obfuscated_id 檢查: {originalResponse.obfuscated_id}");
                // 可以在這裡新增其他檢查方法
            }
        }
    }
    
    private IEnumerator DownloadAndApplySkybox(SkyboxResponse response)
    {
        if (response == null || string.IsNullOrEmpty(response.file_url))
        {
            LogError("無法下載 skybox：回應或圖片 URL 為空");
            OnError?.Invoke("無法下載 skybox：圖片 URL 為空");
            yield break;
        }
        
        LogDebug($"開始下載 skybox 圖片: {response.file_url}");
        
        using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(response.file_url))
        {
            webRequest.timeout = 60; // 設定 60 秒超時
            yield return webRequest.SendWebRequest();
            
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(webRequest);
                    
                    if (texture != null && texture.width > 0 && texture.height > 0)
                    {
                        // 若設定為延遲套用，先快取等待外部呼叫
                        if (!autoApplyToScene)
                        {
                            cachedTexture = texture;
                            cachedResponse = response;
                            LogDebug($"Skybox 圖片下載完成 ({texture.width}x{texture.height})，已快取等待套用");
                            OnStatusUpdate?.Invoke("Skybox 已生成並快取，等待套用時機");
                        }
                        else
                        {
                            LogDebug($"Skybox 圖片下載完成 ({texture.width}x{texture.height})，正在應用到場景...");
                            ApplySkyboxToScene(texture, response);
                            OnSkyboxGenerated?.Invoke(response);
                            OnStatusUpdate?.Invoke("Skybox 已成功應用到場景!");
                        }
                    }
                    else
                    {
                        LogError("下載的圖片無效或尺寸為零");
                        OnError?.Invoke("下載的圖片無效");
                    }
                }
                catch (Exception e)
                {
                    LogError($"處理下載圖片時發生錯誤: {e.Message}");
                    OnError?.Invoke($"處理圖片失敗: {e.Message}");
                }
            }
            else
            {
                LogError($"下載圖片失敗: {webRequest.error}");
                LogError($"HTTP 狀態碼: {webRequest.responseCode}");
                OnError?.Invoke($"下載圖片失敗: {webRequest.error}");
            }
        }
    }
    
    private void ApplySkyboxToScene(Texture2D texture, SkyboxResponse response)
    {
        if (!autoApplyToScene)
        {
            LogDebug("自動應用 skybox 已停用");
            return;
        }
        
        if (texture == null)
        {
            LogError("無法應用 skybox：貼圖為空");
            return;
        }
        
        try
        {
            // 建立新的 skybox material 或使用現有的
            Material material = skyboxMaterial;
            if (material == null)
            {
                Shader skyboxShader = Shader.Find("Skybox/Panoramic");
                if (skyboxShader == null)
                {
                    LogError("找不到 Skybox/Panoramic 著色器");
                    return;
                }
                material = new Material(skyboxShader);
            }
            
            // 設定貼圖
            material.SetTexture("_MainTex", texture);
            material.SetFloat("_Rotation", 0f);
            material.SetFloat("_Exposure", 1f);
            
            // 應用到場景
            RenderSettings.skybox = material;
            DynamicGI.UpdateEnvironment();
            
            if (loadingAnimation != null)
            {
                LogDebug("正在呼叫 HideLoadingScreen...");
                loadingAnimation.HideLoadingScreen();
            }
            else
            {
                LogDebug("loadingAnimation 為空，無法呼叫 HideLoadingScreen");
            }
            
            string title = !string.IsNullOrEmpty(response?.title) ? response.title : "未知";
            LogDebug($"Skybox '{title}' 已成功應用到場景");
        }
        catch (Exception e)
        {
            LogError($"應用 skybox 到場景時發生錯誤: {e.Message}");
            OnError?.Invoke($"應用 skybox 失敗: {e.Message}");
        }
    }
    
    /// <summary>
    /// 取消指定的生成請求
    /// </summary>
    /// <param name="generationId">生成 ID</param>
    public void CancelGeneration(int generationId)
    {
        if (activeGenerations.ContainsKey(generationId))
        {
            StopCoroutine(activeGenerations[generationId]);
            activeGenerations.Remove(generationId);
            LogDebug($"已取消生成 (ID: {generationId})");
        }
    }
    
    /// <summary>
    /// 取消所有活動中的生成請求
    /// </summary>
    public void CancelAllGenerations()
    {
        foreach (var generation in activeGenerations)
        {
            StopCoroutine(generation.Value);
        }
        activeGenerations.Clear();
        generationResponses.Clear();
        LogDebug("已取消所有生成請求");
    }
    
    /// <summary>
    /// 手動下載並應用 skybox（用於測試或當自動檢查失敗時）
    /// </summary>
    /// <param name="imageUrl">skybox 圖片的完整 URL</param>
    /// <param name="title">可選的標題</param>
    public void ManuallyApplySkybox(string imageUrl, string title = "手動 Skybox")
    {
        if (string.IsNullOrEmpty(imageUrl))
        {
            LogError("圖片 URL 不能為空");
            return;
        }
        
        LogDebug($"手動下載並應用 skybox: {imageUrl}");
        
        SkyboxResponse mockResponse = new SkyboxResponse
        {
            file_url = imageUrl,
            title = title,
            status = "complete"
        };
        
        StartCoroutine(DownloadAndApplySkybox(mockResponse));
    }
    
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[SkyboxAPI] {message}");
        }
    }
    
    private void LogError(string message)
    {
        Debug.LogError($"[SkyboxAPI] {message}");
    }
    
    // 外部控制：是否自動套用
    public void SetAutoApplyToScene(bool enabled)
    {
        autoApplyToScene = enabled;
    }
    
    // 查詢是否已有可套用的快取
    public bool HasCachedSkybox()
    {
        return cachedTexture != null && cachedResponse != null;
    }
    
    // 手動套用已快取的 Skybox
    public bool ApplyCachedSkybox()
    {
        if (cachedTexture == null || cachedResponse == null)
        {
            LogError("沒有可套用的快取 Skybox");
            return false;
        }
        // 強制套用：暫時開啟自動應用，避免被旗標擋掉
        bool prev = autoApplyToScene;
        autoApplyToScene = true;
        ApplySkyboxToScene(cachedTexture, cachedResponse);
        autoApplyToScene = prev;
        OnSkyboxGenerated?.Invoke(cachedResponse);
        OnStatusUpdate?.Invoke("已套用快取的 Skybox!");
        // 清空快取
        cachedTexture = null;
        cachedResponse = null;
        return true;
    }

    void OnDestroy()
    {
        CancelAllGenerations();
    }
}

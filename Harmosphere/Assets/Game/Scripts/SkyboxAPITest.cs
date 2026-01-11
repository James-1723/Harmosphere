using UnityEngine;
using UnityEngine.UI;

public class SkyboxAPITest : MonoBehaviour
{
    
    [Header("UI 元件")]
    [SerializeField] private InputField promptInput;
    [SerializeField] private Button generateButton;
    [SerializeField] private Text statusText;
    [SerializeField] private Slider styleSlider;
    [SerializeField] private Text styleText;

    
    [Header("預設設定")]
    [SerializeField] private string[] presetPrompts = {
        "未來感科幻城市，霓虹燈光照亮夜空",
        "寧靜的森林，陽光透過樹葉灑下",
        "壯麗的山峰，雲霧繚繞",
        "海底世界，珊瑚礁和熱帶魚",
        "太空站內部，星星點點的宇宙背景"
    };
    
    private SkyboxAPI skyboxAPI;
    private int currentPresetIndex = 0;
    
    void Start()
    {
        // 取得 SkyboxAPI 元件
        skyboxAPI = FindObjectOfType<SkyboxAPI>();
        if (skyboxAPI == null)
        {
            Debug.LogError("找不到 SkyboxAPI 元件！請確保場景中有 GameObject 附加了 SkyboxAPI 腳本。");
            return;
        }
        
        // 設定 UI
        SetupUI();
        
        // 註冊事件
        RegisterEvents();
        
        // 設定預設提示
        if (promptInput != null && presetPrompts.Length > 0)
        {
            promptInput.text = presetPrompts[0];
        }
    }
    
    private void SetupUI()
    {
        // 設定生成按鈕
        if (generateButton != null)
        {
            generateButton.onClick.AddListener(OnGenerateButtonClicked);
        }
        
        // 設定風格滑桿
        if (styleSlider != null)
        {
            styleSlider.minValue = 1;
            styleSlider.maxValue = 20;
            styleSlider.value = 10; // SciFi 風格
            styleSlider.onValueChanged.AddListener(OnStyleChanged);
            UpdateStyleText((int)styleSlider.value);
        }
        
        // 初始化狀態文字
        if (statusText != null)
        {
            statusText.text = "準備就緒 - 請輸入提示文字並點擊生成";
        }
    }
    
    private void RegisterEvents()
    {
        if (skyboxAPI != null)
        {
            skyboxAPI.OnGenerationStarted += OnGenerationStarted;
            skyboxAPI.OnStatusUpdate += OnStatusUpdate;
            skyboxAPI.OnSkyboxGenerated += OnSkyboxGenerated;
            skyboxAPI.OnError += OnError;
        }
    }
    
    private void OnGenerateButtonClicked()
    {
        if (skyboxAPI == null)
        {
            UpdateStatus("錯誤：找不到 SkyboxAPI 元件");
            return;
        }
        
        string prompt = promptInput != null ? promptInput.text : presetPrompts[0];
        int styleId = styleSlider != null ? (int)styleSlider.value : 10;
        
        if (string.IsNullOrEmpty(prompt))
        {
            UpdateStatus("請輸入提示文字");
            return;
        }
        
        // 禁用按鈕防止重複點擊
        if (generateButton != null)
        {
            generateButton.interactable = false;
        }
        
        UpdateStatus($"開始生成 skybox...");
        skyboxAPI.GenerateSkybox(prompt, styleId);
    }
    
    private void OnGenerationStarted(string prompt)
    {
        UpdateStatus($"已提交生成請求: {prompt}");
    }
    
    private void OnStatusUpdate(string status)
    {
        UpdateStatus(status);
    }
    
    private void OnSkyboxGenerated(SkyboxResponse response)
    {
        UpdateStatus($"✓ Skybox 生成完成: {response.title}");
        
        // 重新啟用按鈕
        if (generateButton != null)
        {
            generateButton.interactable = true;
        }
    }
    
    private void OnError(string error)
    {
        UpdateStatus($"❌ 錯誤: {error}");
        
        // 重新啟用按鈕
        if (generateButton != null)
        {
            generateButton.interactable = true;
        }
    }
    
    private void OnStyleChanged(float value)
    {
        UpdateStyleText((int)value);
    }
    
    private void UpdateStyleText(int styleId)
    {
        if (styleText != null)
        {
            string styleName = GetStyleName(styleId);
            styleText.text = $"風格: {styleName} (ID: {styleId})";
        }
    }
    
    private string GetStyleName(int styleId)
    {
        // 根據 Blockade Labs 的風格 ID 返回對應名稱
        switch (styleId)
        {
            case 1: return "夢幻";
            case 2: return "動漫";
            case 3: return "寫實";
            case 4: return "數位繪畫";
            case 5: return "幻想藝術";
            case 6: return "低多邊形";
            case 7: return "像素藝術";
            case 8: return "復古未來";
            case 9: return "蒸氣龐克";
            case 10: return "科幻";
            case 11: return "霓虹龐克";
            case 12: return "末世廢土";
            case 13: return "內部空間";
            case 14: return "自然";
            case 15: return "城市";
            case 16: return "水下";
            case 17: return "太空";
            case 18: return "抽象";
            case 19: return "卡通";
            case 20: return "寫實攝影";
            default: return "未知風格";
        }
    }
    
    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"[SkyboxAPITest] {message}");
    }
    
    // 測試方法 - 可以在 Inspector 中呼叫或透過其他腳本呼叫
    [ContextMenu("生成測試 Skybox")]
    public void GenerateTestSkybox()
    {
        if (skyboxAPI != null && presetPrompts.Length > 0)
        {
            string prompt = presetPrompts[currentPresetIndex];
            skyboxAPI.GenerateSkybox(prompt);
            currentPresetIndex = (currentPresetIndex + 1) % presetPrompts.Length;
        }
    }
    
    [ContextMenu("下一個預設提示")]
    public void NextPresetPrompt()
    {
        if (presetPrompts.Length > 0 && promptInput != null)
        {
            currentPresetIndex = (currentPresetIndex + 1) % presetPrompts.Length;
            promptInput.text = presetPrompts[currentPresetIndex];
        }
    }
    
    void OnDestroy()
    {
        // 取消註冊事件
        if (skyboxAPI != null)
        {
            skyboxAPI.OnGenerationStarted -= OnGenerationStarted;
            skyboxAPI.OnStatusUpdate -= OnStatusUpdate;
            skyboxAPI.OnSkyboxGenerated -= OnSkyboxGenerated;
            skyboxAPI.OnError -= OnError;
        }
    }
}

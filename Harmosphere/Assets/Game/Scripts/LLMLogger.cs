using System;
using System.IO;
using UnityEngine;

/// <summary>
/// LLM日誌系統，記錄所有 Prompt 和 LLM 回應
/// 每次運行 LLM 都會記錄到一個檔案中，方便調試和改進 Prompt
/// </summary>
public static class LLMLogger
{
    private static string logDirectory;
    private static string logFilePath;
    private static int callCount = 0;

    /// <summary>
    /// 初始化日誌系統，創建日誌目錄和檔案
    /// </summary>
    public static void Initialize()
    {
        // 設定日誌目錄（在 Unity 專案外部的易存取位置）
        logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "HarmoSphere_LLM_Logs");

        // 確保目錄存在
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        // 創建當日的日誌檔案
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        logFilePath = Path.Combine(logDirectory, $"LLM_Log_{timestamp}.txt");

        // 寫入日誌檔案開頭
        WriteToLog($"========== HarmoSphere LLM 日誌 ==========");
        WriteToLog($"記錄開始時間: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        WriteToLog($"========================================\n");

        Debug.Log($"[LLMLogger] 日誌已初始化，路徑: {logFilePath}");
    }

    /// <summary>
    /// 記錄一次 LLM 呼叫，包括 Prompt 和回應
    /// </summary>
    public static void LogLLMCall(string prompt, string response, string callType = "Standard")
    {
        callCount++;

        string logEntry = $"\n========== LLM 呼叫 #{callCount} ==========\n";
        logEntry += $"時間: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
        logEntry += $"類型: {callType}\n";
        logEntry += $"\n--- Prompt ---\n{prompt}\n";
        logEntry += $"\n--- LLM 回應 ---\n{response}\n";
        logEntry += $"========================================\n";

        WriteToLog(logEntry);

        // 同時在 Console 輸出摘要
        Debug.Log($"[LLMLogger] 已記錄 LLM 呼叫 #{callCount} ({callType})");
    }

    /// <summary>
    /// 私有方法，寫入實際的日誌內容
    /// </summary>
    private static void WriteToLog(string content)
    {
        try
        {
            if (string.IsNullOrEmpty(logFilePath))
            {
                Initialize();
            }

            File.AppendAllText(logFilePath, content + "\n");
        }
        catch (Exception e)
        {
            Debug.LogError($"[LLMLogger] 寫入日誌失敗: {e.Message}");
        }
    }

    /// <summary>
    /// 取得當前日誌檔案路徑
    /// </summary>
    public static string GetLogFilePath()
    {
        return logFilePath;
    }
}

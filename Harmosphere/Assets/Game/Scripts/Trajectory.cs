using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Trajectory : MonoBehaviour
{
    [Header("BPM Settings")]
    [SerializeField] private float[] availableBpms = { 60f, 70f, 80f, 90f, 100f };
    [SerializeField] private float startingBpm = 80f;

    // 已使用過的 BPM（不能重複）
    private HashSet<float> usedBpms = new HashSet<float>();
    
    // 當前使用的 BPM
    private float currentBpm;
    
    // 上一次的分數（用於比較）
    private float previousScore = 0f;
    
    // 歷史成績記錄（經過公式轉換，用於顯示）
    private List<float> performanceHistory = new List<float>();

    private void Start()
    {
        currentBpm = startingBpm;
        usedBpms.Add(currentBpm);
        Debug.Log($"[Trajectory] Initialized with starting BPM: {currentBpm}");
    }

    /// <summary>
    /// 記錄玩家表現並決定下一首歌的 BPM
    /// </summary>
    /// <param name="scoreRate">得分率 (0~1)</param>
    /// <returns>下一首歌的 BPM</returns>
    public float RecordPerformance(float scoreRate)
    {
        // 計算玩家表現分數（放大10倍用於顯示）
        float performance = scoreRate * 160f * 10f;
        performanceHistory.Add(performance);
        
        // 決定下一首歌的 BPM
        float nextBpm = DecideNextBpm(scoreRate);
        
        // 更新上一次分數
        previousScore = scoreRate;
        
        Debug.Log($"Score Rate: {scoreRate} => Performance: {performance}, Next BPM: {nextBpm}");
        return nextBpm;
    }

    /// <summary>
    /// 根據分數進步情況決定下一首歌的 BPM
    /// </summary>
    private float DecideNextBpm(float currentScore)
    {
        bool improved = currentScore > previousScore;
        
        // 取得可用的 BPM 選項（排除已使用過的）
        List<float> availableOptions;
        
        if (improved)
        {
            // 分數進步 -> 選擇比當前更高的 BPM
            availableOptions = availableBpms
                .Where(bpm => bpm > currentBpm && !usedBpms.Contains(bpm))
                .ToList();
            Debug.Log($"[Trajectory] Score improved! Looking for BPM > {currentBpm}");
        }
        else
        {
            // 分數沒進步或退步 -> 選擇比當前更低的 BPM
            availableOptions = availableBpms
                .Where(bpm => bpm < currentBpm && !usedBpms.Contains(bpm))
                .ToList();
            Debug.Log($"[Trajectory] Score not improved. Looking for BPM < {currentBpm}");
        }
        
        // 如果沒有符合條件的選項，嘗試選擇任何未使用過的 BPM
        if (availableOptions.Count == 0)
        {
            availableOptions = availableBpms
                .Where(bpm => !usedBpms.Contains(bpm))
                .ToList();
            Debug.Log($"[Trajectory] No options in preferred direction, checking all unused BPMs");
        }
        
        // 如果所有 BPM 都用過了，重置已使用列表（但保留當前 BPM）
        if (availableOptions.Count == 0)
        {
            Debug.Log($"[Trajectory] All BPMs used! Resetting used list.");
            usedBpms.Clear();
            usedBpms.Add(currentBpm);
            
            // 重新獲取選項
            if (improved)
            {
                availableOptions = availableBpms
                    .Where(bpm => bpm > currentBpm && !usedBpms.Contains(bpm))
                    .ToList();
            }
            else
            {
                availableOptions = availableBpms
                    .Where(bpm => bpm < currentBpm && !usedBpms.Contains(bpm))
                    .ToList();
            }
            
            // 如果還是沒有，取所有未使用的
            if (availableOptions.Count == 0)
            {
                availableOptions = availableBpms
                    .Where(bpm => !usedBpms.Contains(bpm))
                    .ToList();
            }
        }
        
        // 從可用選項中隨機選擇一個
        float selectedBpm;
        if (availableOptions.Count > 0)
        {
            selectedBpm = availableOptions[Random.Range(0, availableOptions.Count)];
        }
        else
        {
            // 極端情況：只有一個 BPM 可用，就用當前的
            selectedBpm = currentBpm;
            Debug.LogWarning($"[Trajectory] No available BPM options, keeping current: {currentBpm}");
        }
        
        // 更新當前 BPM 並標記為已使用
        currentBpm = selectedBpm;
        usedBpms.Add(selectedBpm);
        
        Debug.Log($"[Trajectory] Selected BPM: {selectedBpm}, Used BPMs: [{string.Join(", ", usedBpms)}]");
        
        return selectedBpm;
    }
    
    /// <summary>
    /// 取得當前 BPM
    /// </summary>
    public float GetCurrentBpm()
    {
        return currentBpm;
    }
    
    /// <summary>
    /// 取得已使用的 BPM 列表
    /// </summary>
    public HashSet<float> GetUsedBpms()
    {
        return new HashSet<float>(usedBpms);
    }
    
    /// <summary>
    /// 重置已使用的 BPM 列表（用於重新開始遊戲）
    /// </summary>
    public void ResetUsedBpms()
    {
        usedBpms.Clear();
        usedBpms.Add(currentBpm);
        previousScore = 0f;
        Debug.Log($"[Trajectory] Reset used BPMs. Current: {currentBpm}");
    }
}

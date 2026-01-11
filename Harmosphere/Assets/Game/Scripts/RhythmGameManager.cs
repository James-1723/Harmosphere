using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using TMPro;

[Serializable]
public class AudioData
{
    // 這是 AudioSource Component
    public AudioClip source; 
    
    // 這是用來描述或標記這個音源的字串
    public string beatmapName; 
}

public class RhythmGameManager : MonoBehaviour
{
    [Header("音頻設定")]
    public AudioClip testClip;
    public AudioSource musicPlayer;
    public AudioSource[] audioSources = new AudioSource[3];
    public AudioSource bgm;
    public string beatmapFileName = "Decayed World.osu";
    public AudioData[] audioData;

    [Header("遊戲設定")]
    public float noteSpeed = 10f;
    public float approachTime = 2f;
    public float judgeLineZ = 2f;
    public MusicGPTClient musicGPTClient;
    private int currentScore;
    private int currentCombo;

    [Header("音符預製體")]
    public GameObject shortNotePrefab;
    public GameObject longNotePrefab;

    [Header("軌道設定")]
    public Transform[] tracks = new Transform[6];

    [Header("UI")]
    public UnityEngine.UI.Text scoreText;
    public UnityEngine.UI.Text comboText;
    public TextMeshPro scoreTextMeshPro;
    public TextMeshPro comboTextMeshPro;
    public TextMeshPro transcriptText;

    [Header("Scene Change")]
    public float nextLevelBpm;
    public int sceneCount = 0;
    public string[] beatmapFiles = new string[3];
    public Material[] skyboxMaterials = new Material[2];
    public AnimationController animationController;
    public GameObject playerModel;
    public TranscriptGenerator transcriptGenerator;
    public ChangeSceneAPI changeSceneAPI;
    public Trajectory trajectory;

    [Header("LLM Option System")]
    public LLMOptionGenerator llmOptionGenerator;
    public OptionSelectionUI3D optionSelectionUI3D;
    // public GameObject optionSelectionContainer; // Removed redundant field
    private int userSelectedConcernIndex = -1;      // 第一題：用戶的心境選擇 (0/1/2)
    private int userSelectedSkyboxIndex = -1;       // 第二題：用戶的天空球選擇 (0/1/2)
    private string userSelectedConcernText = "";     // 儲存具體的心境文字
    private string userSelectedSkyboxText = "";      // 儲存具體的場景文字
    private int followUpQuestionCount = 0;           // 追蹤目前已問的後續問題數 (0-3)
    private string conversationHistory = "";         // 追蹤對話歷史，格式：Q1: [問題]\nA1: [答案]\nQ2: ...
    private string pendingTherapistFeedback = "";    // 儲存待顯示的治療師反饋
    private string userSpeedFeedback = "";           // 儲存用戶對遊戲速度的反饋（太快/正好/太慢）
    private float nextLevelNoteSpeed = 10f;          // 下一關的 note 移動速度


    private OsuBeatmap beatmap;
    private List<HitObject> hitObjects = new List<HitObject>();
    private Queue<HitObject> upcomingNotes = new Queue<HitObject>();

    private float spawnDistance;
    private float currentTime;
    private int score = 0;
    private int combo = 0;
    private int maxCombo = 0;
    private int previousScore = 0;
    private bool gameStarted = false;
    private bool isAdvancingScene = false;   // 防止重複切換
    private bool suppressSceneManager = false; // 載入後是否自動呼叫 SceneManager
    
    // 彈跳特效相關
    private Vector3 originalScoreScale;      // scoreTextMeshPro 的原始縮放值
    private Vector3 originalScorePosition;   // scoreTextMeshPro 的原始位置
    private Vector3 originalComboScale;      // comboTextMeshPro 的原始縮放值
    private Vector3 originalComboTextScale;  // comboText 的原始縮放值
    private bool isBouncing = false;
    private bool isScoreBouncing = false;

    void Start()
    {
        spawnDistance = noteSpeed * approachTime;
        
        // 儲存 scoreTextMeshPro 的原始縮放值和位置
        if (scoreTextMeshPro != null)
        {
            originalScoreScale = scoreTextMeshPro.transform.localScale;
            originalScorePosition = scoreTextMeshPro.transform.localPosition;
        }
        
        // 儲存 comboTextMeshPro 的原始縮放值
        if (comboTextMeshPro != null)
        {
            originalComboScale = comboTextMeshPro.transform.localScale;
        }
        
        // 儲存 comboText 的原始縮放值
        if (comboText != null)
        {
            originalComboTextScale = comboText.transform.localScale;
        }

        // 啟動 Intro 流程
        if (optionSelectionUI3D == null)
        {
            Debug.LogError("[RhythmGameManager] optionSelectionUI3D is NOT assigned! UI will not show up.");
        }
        StartCoroutine(IntroSequence());
    }

    IEnumerator IntroSequence()
    {
        Debug.Log("IntroSequence Started");

        // 第一步：同時開始 LLM 呼叫和動畫播放（並行處理）
        bool llmCompleted = false;
        string introPrompt = IntroPromptManager.GetIntroPrompt();
        
        // 立即開始 LLM 呼叫（不等待）
        Debug.Log("[IntroSequence] Starting LLM call in parallel with animation");
        StartCoroutine(llmOptionGenerator.GenerateOptionsAsync(introPrompt, (success) =>
        {
            llmCompleted = true;
            if (success)
            {
                Debug.Log("[IntroSequence] LLM options generated successfully (parallel)");
            }
            else
            {
                Debug.LogWarning("[IntroSequence] Failed to generate LLM options, using defaults");
            }
        }));

        // 同時播放動畫（會等待 13 秒）
        yield return StartCoroutine(PlayIntroAnimation());

        // 動畫結束後，如果 LLM 還沒完成，等待它完成
        if (!llmCompleted)
        {
            Debug.Log("[IntroSequence] Animation done, waiting for LLM to complete...");
            while (!llmCompleted)
            {
                yield return null;
            }
        }
        else
        {
            Debug.Log("[IntroSequence] LLM already completed during animation");
        }

        // 第二步：顯示選項並等待用戶選擇
        if (optionSelectionUI3D != null) optionSelectionUI3D.gameObject.SetActive(true);
        yield return StartCoroutine(DisplayIntroOptionsAndWaitForSelection());
        
        // 用戶選擇完成後，隱藏選項按鈕（但保留容器以便顯示字幕）
        if (optionSelectionUI3D != null) optionSelectionUI3D.HideOptions();

        // 第三步：自動選擇天空球 (隨機)
        userSelectedSkyboxText = changeSceneAPI.ChangeSkyboxByScore(false, true);

        // 第四步：停止背景音樂
        if (bgm != null && bgm.isPlaying)
        {
            yield return StartCoroutine(FadeOutBGM(bgm, 2f));
        }

        transcriptText.text = "";

        // 第五步：選擇第一首歌 (80 BPM)
        SelectNextBeatmap(80);

        // 第六步：載入 Beatmap
        yield return StartCoroutine(LoadBeatmapAsync());
    }

    /// <summary>
    /// 播放 13 秒的 intro：只顯示介紹文字（不播放動畫，playerModel 只在 Encouragement 階段出現）
    /// </summary>
    IEnumerator PlayIntroAnimation()
    {
        Debug.Log("[Intro] Starting 13-second intro (text only, no player model)");

        // 確保 playerModel 隱藏（只在 Encouragement 階段才顯示）
        if (playerModel != null) playerModel.SetActive(false);

        // 顯示歡迎訊息（介紹文字）
        // string introText = "Welcome to HarmoSphere!\n A peaceful sanctuary where you can relax.\n Move to the calming music and be yourself.\n You are at peace. Enjoy this moment.";
        string introText = "歡迎來到 HarmoSphere！\n這是一個可以讓你放鬆的寧靜空間。\n請隨著平靜的音樂自在舞動。\n好好享受這一刻吧。";
        transcriptText.text = introText;
        transcriptText.gameObject.SetActive(true);

        // 等待 13 秒
        yield return new WaitForSeconds(13f);

        // 13 秒後隱藏介紹文字
        transcriptText.gameObject.SetActive(false);

        Debug.Log("[Intro] 13-second intro completed");
    }

    /// <summary>
    /// 顯示 Intro 選項並等待用戶選擇（LLM 已在 IntroSequence 中提前呼叫）
    /// </summary>
    IEnumerator DisplayIntroOptionsAndWaitForSelection()
    {
        Debug.Log("[IntroSequence] Displaying intro options...");

        // 顯示選項並等待用戶選擇
        bool userSelected = false;
        optionSelectionUI3D.DisplayOptions(llmOptionGenerator.generatedOptions, (selectedIndex) =>
        {
            userSelectedConcernIndex = selectedIndex;
            userSelectedConcernText = llmOptionGenerator.generatedOptions.options[selectedIndex];
            userSelected = true;
            Debug.Log($"[IntroSequence] User selected concern {selectedIndex}: {userSelectedConcernText}");
        });

        // 等待用戶選擇
        while (!userSelected)
        {
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("[IntroSequence] User has selected an option, continuing...");
    }



    void Update()
    {
        if (!gameStarted) return;

        // currentTime = audioSources[sceneCount].time;
        currentTime = musicPlayer.time;
        ProcessUpcomingNotes();
        UpdateUI();
        CheckInput();
        // 自動切到下一首：當場上沒有任何音符或歌曲已停止播放
        if (!isAdvancingScene)
        {
            bool noNotes = upcomingNotes.Count == 0 && FindObjectsOfType<Note>().Length == 0;
            bool audioEnded = musicPlayer != null && !musicPlayer.isPlaying && currentTime > 0.1f;
            if (noNotes || audioEnded)
            {
                StartCoroutine(AdvanceToNextScene());
            }
        }
    }

    private IEnumerator SceneManager()
    {
        Debug.Log($"SceneManager 執行: sceneCount = {sceneCount}");
        
        if (sceneCount == 0)
        {
            Debug.Log("SceneManager: 0 (Game Start)");
            StartCoroutine(StartGameAfterDelay(sceneCount));
        }
        else
        {
            // 通用邏輯：適用於 sceneCount >= 1
            Debug.Log($"SceneManager: Generic Level (sceneCount = {sceneCount})");
            StartCoroutine(StartGameAfterDelay(sceneCount));
            transcriptText.text = "";
        }
        yield break;
    }

    IEnumerator LoadBeatmapAsync()
    {
        string filePath = GetStreamingAssetPath(beatmapFileName);
        yield return StartCoroutine(LoadBeatmapFromStreamingAssets(filePath));
    }

    string GetStreamingAssetPath(string fileName)
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);

#if UNITY_ANDROID && !UNITY_EDITOR
        return path;
#elif UNITY_WEBGL && !UNITY_EDITOR
        return path;
#else
        return path;
#endif
    }

    IEnumerator LoadBeatmapFromStreamingAssets(string path)
    {
        string beatmapData = "";

#if UNITY_ANDROID && !UNITY_EDITOR
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(path))
        {
            yield return www.SendWebRequest();
            
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                beatmapData = www.downloadHandler.text;
            }
            else
            {
                Debug.LogError($"Android載入beatmap失敗: {www.error}");
                yield break;
            }
        }
#else
        try
        {
            if (File.Exists(path))
            {
                beatmapData = File.ReadAllText(path);
            }
            else
            {
                Debug.LogError($"找不到beatmap文件: {path}");
                ListStreamingAssetsContents();
                yield break;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"載入beatmap時發生錯誤: {e.Message}");
            yield break;
        }
#endif

        bool parseSuccess = false;
        try
        {
            beatmap = OsuBeatmapParser.ParseBeatmapFromString(beatmapData);
            hitObjects = beatmap.hitObjects;

            foreach (var hitObj in hitObjects)
            {
                upcomingNotes.Enqueue(hitObj);
            }

            parseSuccess = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"解析beatmap時發生錯誤: {e.Message}");
        }

        if (parseSuccess)
        {
            if (!suppressSceneManager)
            {
                yield return StartCoroutine(SceneManager());
            }
            Debug.Log("Beatmap載入完成");
        }
    }

    void ListStreamingAssetsContents()
    {
        try
        {
            string streamingAssetsPath = Application.streamingAssetsPath;
            if (Directory.Exists(streamingAssetsPath))
            {
                string[] files = Directory.GetFiles(streamingAssetsPath);
                foreach (string file in files)
                {
                    Debug.Log($"  - {Path.GetFileName(file)}");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"無法列出StreamingAssets內容: {e.Message}");
        }
    }

    IEnumerator StartGameAfterDelay(int sceneIndex)
    {
        Debug.Log($"[StartGameAfterDelay] 準備隱藏 playerModel 和 UI 容器，sceneIndex = {sceneIndex}");
        playerModel.SetActive(false);
        
        // 遊戲開始前隱藏整個選項容器（只有轉場時才需要顯示）
        if (optionSelectionUI3D != null) optionSelectionUI3D.HideContainer();
        
        yield return new WaitForSeconds(1f); // 等待 playerModel 消失
        
        yield return new WaitForSeconds(1f); // 額外等待1秒再開始音樂

        if (musicPlayer != null)
        {
            musicPlayer.Play();
            gameStarted = true;
        }
        else
        {
            Debug.LogError("musicPlayer未設置！");
        }
    }

    void ProcessUpcomingNotes()
    {
        while (upcomingNotes.Count > 0)
        {
            HitObject nextNote = upcomingNotes.Peek();
            float noteTime = nextNote.time / 1000f;
            float spawnTime = noteTime - approachTime;

            if (currentTime >= spawnTime)
            {
                SpawnNote(upcomingNotes.Dequeue());
            }
            else
            {
                break;
            }
        }
    }

    void SpawnNote(HitObject hitObj)
    {
        int trackIndex = DetermineTrack(hitObj.x);
        Vector3 spawnPosition = GetTrackPosition(trackIndex);
        spawnPosition.z = judgeLineZ + spawnDistance;

        GameObject notePrefab = (hitObj.type & 128) != 0 ? longNotePrefab : shortNotePrefab;

        if (notePrefab == null)
        {
            Debug.LogError($"音符預製體未設置! 類型: {((hitObj.type & 128) != 0 ? "長音符" : "短音符")}");
            return;
        }

        GameObject noteObj = Instantiate(notePrefab, spawnPosition, Quaternion.identity);

        Note noteScript = noteObj.GetComponent<Note>();
        if (noteScript != null)
        {
            Transform trackTransform = GetTrackTransform(trackIndex);
            Note trackNote = trackTransform?.GetComponent<Note>();

            if (trackNote != null)
            {
                noteScript.noteDirection = trackNote.noteDirection;
                // 若軌道設定了自訂旋轉，沿用；否則讓 Note 依 noteDirection 在 Y 軸轉向
                if (trackNote.useCustomRotation)
                {
                    noteScript.useCustomRotation = true;
                    noteScript.customRotationAngles = trackNote.customRotationAngles;
                }
                else
                {
                    noteScript.useCustomRotation = false;
                }
                noteScript.SetNoteRotationFromSettings();
            }
            else if (trackTransform != null)
            {
                // 軌道沒有 Note 設定元件時，直接採用軌道自身旋轉
                noteScript.useCustomRotation = true;
                noteScript.customRotationAngles = trackTransform.eulerAngles;
                noteScript.SetNoteRotationFromSettings();
            }

            noteScript.Initialize(hitObj.time / 1000f, trackIndex, (hitObj.type & 128) != 0, noteSpeed, judgeLineZ);

            if ((hitObj.type & 128) != 0)
            {
                noteScript.endTime = hitObj.endTime / 1000f;
            }
        }
        else
        {
            Debug.LogError("音符預製體缺少Note腳本!");
        }
    }

    int DetermineTrack(int osuX)
    {
        float normalizedX = (osuX - 36f) / (475f - 36f);
        return Mathf.Clamp(Mathf.FloorToInt(normalizedX * 6), 0, 5);
    }

    Transform GetTrackTransform(int trackIndex)
    {
        if (trackIndex >= 0 && trackIndex < 6 && tracks[trackIndex] != null)
        {
            return tracks[trackIndex];
        }
        else
        {
            Debug.LogError($"軌道 {trackIndex} 未設置!");
            return null;
        }
    }

    Vector3 GetTrackPosition(int trackIndex)
    {
        if (trackIndex >= 0 && trackIndex < 6 && tracks[trackIndex] != null)
        {
            return tracks[trackIndex].position;
        }
        else
        {
            Debug.LogError($"軌道 {trackIndex} 未設置!");
            return Vector3.zero;
        }
    }

    void CheckInput()
    {
        if (Input.GetKeyDown(KeyCode.Q)) CheckHit(0);
        if (Input.GetKeyDown(KeyCode.W)) CheckHit(1);
        if (Input.GetKeyDown(KeyCode.E)) CheckHit(2);
        if (Input.GetKeyDown(KeyCode.A)) CheckHit(3);
        if (Input.GetKeyDown(KeyCode.S)) CheckHit(4);
        if (Input.GetKeyDown(KeyCode.D)) CheckHit(5);
    }

    void CheckHit(int trackIndex)
    {
        Note[] allNotes = FindObjectsOfType<Note>();
        Note closestNote = null;
        float minDistance = float.MaxValue;

        foreach (Note note in allNotes)
        {
            if (note.trackIndex == trackIndex && !note.isHit)
            {
                float distance = Mathf.Abs(note.transform.position.z - judgeLineZ);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestNote = note;
                }
            }
        }

        if (closestNote != null && minDistance < 2f)
        {
            HitNote(closestNote, minDistance);
        }
    }

    public void HitNote(Note note, float accuracy)
    {
        int points = CalculateScore(accuracy);
        score += points;
        combo++;
        if (combo > maxCombo) maxCombo = combo;

        // 觸發 combo 縮放動畫 (comboTextMeshPro)
        if (comboTextMeshPro != null && !isBouncing)
        {
            StartCoroutine(BounceComboText());
        }
        
        // 觸發 score 位置彈跳動畫 (scoreTextMeshPro)
        if (scoreTextMeshPro != null && !isScoreBouncing)
        {
            StartCoroutine(BounceScorePosition());
        }

        if (note != null)
        {
            note.Hit();
        }
    }

    public void MissNote()
    {
        combo = 0;
    }

    int CalculateScore(float accuracy)
    {
        if (accuracy < 0.5f) return 100 + combo;
        if (accuracy < 1f) return 50 + combo / 2;
        return 10;
    }

    void UpdateUI()
    {
        if (scoreText) scoreText.text = $"{score}";
        if (comboText) comboText.text = $"{combo} combo";
        if (scoreTextMeshPro)
        {
            scoreTextMeshPro.text = $"{score}";
        }
        if (comboTextMeshPro) 
        {
            comboTextMeshPro.text = $"{combo} combo!";
        }
    }

    // Combo 文字縮放彈跳動畫
    IEnumerator BounceComboText()
    {
        if (comboTextMeshPro == null) yield break;
        
        isBouncing = true;
        Transform comboTransform = comboTextMeshPro.transform;
        
        // 動畫參數
        float bounceHeight = 1.1f; // 最大縮放比例
        float bounceSpeed = 6f;    // 動畫速度
        float animationTime = 0.3f; // 總動畫時間
        
        float elapsedTime = 0f;
        
        while (elapsedTime < animationTime)
        {
            elapsedTime += Time.deltaTime;
            
            // 使用 sin 波形創造彈跳效果
            float progress = elapsedTime / animationTime;
            float scaleMultiplier = 1f + (bounceHeight - 1f) * Mathf.Sin(progress * Mathf.PI * bounceSpeed) * (1f - progress);
            
            comboTransform.localScale = originalComboScale * scaleMultiplier;
            
            yield return null;
        }
        
        // 確保回到原始大小
        comboTransform.localScale = originalComboScale;
        isBouncing = false;
    }

    // Score 文字位置彈跳動畫
    IEnumerator BounceScorePosition()
    {
        if (scoreTextMeshPro == null) yield break;
        
        isScoreBouncing = true;
        Transform scoreTransform = scoreTextMeshPro.transform;
        
        // 動畫參數
        float bounceHeight = 4f;   // 往上移動的像素距離
        float animationTime = 0.2f; // 總動畫時間
        
        float elapsedTime = 0f;
        
        while (elapsedTime < animationTime)
        {
            elapsedTime += Time.deltaTime;
            
            // 使用拋物線軌跡創造彈跳效果 (類似物理拋擲)
            float progress = elapsedTime / animationTime;
            float heightOffset = bounceHeight * Mathf.Sin(progress * Mathf.PI);
            
            Vector3 newPosition = originalScorePosition;
            newPosition.z += heightOffset;
            scoreTransform.localPosition = newPosition;
            
            yield return null;
        }
        
        // 確保回到原始位置
        scoreTransform.localPosition = originalScorePosition;
        isScoreBouncing = false;
    }

    void SelectNextBeatmap(float bpm)
    {
        string bpmString = bpm.ToString();
        List<(AudioClip clip, string beatmapFile)> candidates = new List<(AudioClip, string)>();
        List<string> osuFiles = new List<string>();

        // 1. 找出所有符合 BPM 的 .osu 檔案 (僅限 Editor/PC/Mac)
        string streamingAssetsPath = Application.streamingAssetsPath;
#if !UNITY_ANDROID || UNITY_EDITOR
        if (Directory.Exists(streamingAssetsPath))
        {
            try 
            {
                var files = Directory.GetFiles(streamingAssetsPath, "*.osu");
                foreach (var f in files)
                {
                    string fname = Path.GetFileName(f);
                    if (fname.StartsWith(bpmString))
                    {
                        osuFiles.Add(fname);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SelectNextBeatmap] Error listing files: {e.Message}");
            }
        }
#endif

        // 2. 匹配 AudioData
        if (audioData != null)
        {
            foreach (var data in audioData)
            {
                if (data.source == null) continue;

                // Case A: beatmapName 明確指定且符合 BPM
                if (!string.IsNullOrEmpty(data.beatmapName) && data.beatmapName.StartsWith(bpmString))
                {
                    string fname = data.beatmapName.EndsWith(".osu") ? data.beatmapName : data.beatmapName + ".osu";
                    // 檢查檔案是否存在 (或是已經在 osuFiles 列表中)
                    if (osuFiles.Contains(fname) || File.Exists(Path.Combine(streamingAssetsPath, fname)))
                    {
                        candidates.Add((data.source, fname));
                    }
                }
                // Case B: AudioClip 名稱符合 BPM (且 beatmapName 未指定或無效)
                else if (data.source.name.StartsWith(bpmString))
                {
                    // 如果有找到對應 BPM 的 .osu 檔，就配對
                    if (osuFiles.Count > 0)
                    {
                        // 這裡簡單隨機選一個該 BPM 的 beatmap 來配對
                        // 如果希望能更精確匹配 (例如 80bpm 1 -> 80-1.osu)，需要額外邏輯
                        // 目前假設只要 BPM 對了，隨便一個譜面都可以玩
                        string bestMatch = osuFiles[UnityEngine.Random.Range(0, osuFiles.Count)];
                        candidates.Add((data.source, bestMatch));
                    }
                }
            }
        }

        if (candidates.Count > 0)
        {
            var selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            musicPlayer.clip = selected.clip;
            beatmapFileName = selected.beatmapFile;
            
            string audioName = selected.clip != null ? selected.clip.name : "null";
            Debug.Log($"Selected beatmap: {beatmapFileName} for BPM: {bpm} (Audio: {audioName})");
        }
        else
        {
            Debug.LogWarning($"No beatmap found for BPM: {bpm}, selecting random fallback.");
            
            // Fallback: 隨便選一個有效的
            if (audioData != null && audioData.Length > 0)
            {
                List<AudioData> validData = new List<AudioData>();
                foreach(var d in audioData) {
                    if (d.source != null) validData.Add(d);
                }

                if (validData.Count > 0)
                {
                    var fallback = validData[UnityEngine.Random.Range(0, validData.Count)];
                    musicPlayer.clip = fallback.source;
                    // 嘗試找一個譜面，如果沒有就用預設
                    beatmapFileName = !string.IsNullOrEmpty(fallback.beatmapName) ? fallback.beatmapName : "Decayed World.osu";
                    if (!beatmapFileName.EndsWith(".osu")) beatmapFileName += ".osu";
                    
                    Debug.Log($"Fallback selected: {beatmapFileName} (Audio: {fallback.source.name})");
                }
            }
        }
    }

    string DetermineSkyboxPrompt()
    {
        string environment = "";
        string timeOfDay = "";
        
        // 1. 比較分數決定環境
        if (currentScore > previousScore)
        {
            // 分數進步：美麗的城市 / 放鬆的屋內
            environment = UnityEngine.Random.value > 0.5f ? "A beautiful futuristic city" : "A relaxing cozy indoor room with large windows";
        }
        else
        {
            // 分數退步：美麗令人放鬆的大自然 / 海岸
            environment = UnityEngine.Random.value > 0.5f ? "A beautiful relaxing nature forest" : "A peaceful coast with gentle waves";
        }
        
        // 更新 previousScore
        previousScore = currentScore;
        
        // 2. 根據 Combo Rate 決定時間
        var (maxScore, songMaxCombo) = GetSongMaxScoreAndCombo();
        // 避免除以零
        float comboRate = songMaxCombo > 0 ? (float)maxCombo / songMaxCombo : 0f;
        
        if (comboRate > 0.5f)
        {
            timeOfDay = "during daytime with bright sunlight";
        }
        else
        {
            timeOfDay = "at night with starry sky";
        }
        
        string prompt = $"{environment} {timeOfDay}, high quality, 8k resolution, panoramic view";
        Debug.Log($"[DetermineSkyboxPrompt] Score: {currentScore} (Prev: {previousScore}), MaxCombo: {maxCombo}/{songMaxCombo} ({comboRate:P0}) -> Prompt: {prompt}");
        return prompt;
    }

    IEnumerator AdvanceToNextScene()
    {
        playerModel.SetActive(true);
        Debug.Log($"[AdvanceToNextScene] playerModel 設為可見");
        isAdvancingScene = true;
        // 備份當前分數和連擊數
        currentScore = score;
        currentCombo = maxCombo;

        // 停止目前音樂
        if (musicPlayer != null && musicPlayer.isPlaying)
        {
            musicPlayer.Stop();
        }

        // 清理場上音符
        var notes = FindObjectsOfType<Note>();
        foreach (var n in notes)
        {
            // 若這個 Note 是掛在 tracks 上作為設定用，則不要刪除
            if (n != null && n.gameObject != null && !IsTrackNote(n))
            {
                Destroy(n.gameObject);
            }
        }
        // 重置分數和連擊數
        score = 0;
        combo = 0;
        maxCombo = 0;
        float scoreRate = currentScore / (float)GetSongMaxScoreAndCombo().maxScore; // 確保浮點數除法
        nextLevelBpm = trajectory.RecordPerformance(scoreRate);
        // musicGPTClient.GenerateMusic(currentScore, currentCombo);

        upcomingNotes.Clear();
        hitObjects.Clear();
        gameStarted = false;

        // 無限迴圈邏輯：不檢查 beatmapFiles.Length，直接進入下一關
        sceneCount++;
        
        // 根據 BPM 選擇下一首 (移至 AskAboutBPM 之後)
        // SelectNextBeatmap(nextLevelBpm);
        
        Debug.Log($"AdvanceToNextScene: sceneCount 更新為 {sceneCount}, Next BPM: {nextLevelBpm}");
        
        // 播放 10 秒的鼓勵動畫和話語
        yield return StartCoroutine(PlayEncouragementAnimation());

        // 每一關都進行完整流程：3 個後續心理治療問題
        followUpQuestionCount = 0;
        conversationHistory = "";

        if (optionSelectionUI3D != null) optionSelectionUI3D.gameObject.SetActive(true);

        // 問題 1, 2, 3
        for (int q = 1; q <= 3; q++)
        {
            yield return StartCoroutine(AskFollowUpQuestion(q));
            followUpQuestionCount++;
        }

        // 3 個問題後：生成治療師反饋
        yield return StartCoroutine(DisplayTherapistFeedback());

        // 隱藏選項按鈕，因為接下來要顯示純文字的反饋（保留容器）
        if (optionSelectionUI3D != null) optionSelectionUI3D.HideOptions();

        // 顯示治療師反饋 (因為不再有 Skybox 選擇頁面來顯示它，所以這裡需要處理顯示邏輯)
        // 目前 DisplayTherapistFeedback 只是生成並存到 pendingTherapistFeedback
        // 我們可以暫時用 transcriptText 顯示，或者直接忽略顯示，因為使用者要求刪除 Skybox 選擇
        // 但為了保留治療師的回饋，我們可以用 transcriptText 顯示幾秒
        if (!string.IsNullOrEmpty(pendingTherapistFeedback))
        {
            transcriptText.text = pendingTherapistFeedback;
            transcriptText.gameObject.SetActive(true);
            yield return new WaitForSeconds(8f); // 顯示 8 秒
            transcriptText.gameObject.SetActive(false);
            pendingTherapistFeedback = "";
        }

        // 自動選擇 Skybox
        // 第一和第二關 (sceneCount < 2) 隨機挑選 (randomAll = true)
        // 之後根據分數進步與否挑選 (randomAll = false)
        bool improved = currentScore > previousScore;
        userSelectedSkyboxText = changeSceneAPI.ChangeSkyboxByScore(improved, sceneCount < 2);

        // 直接根據表現預測的 BPM 選擇下一首（不再詢問用戶）
        SelectNextBeatmap(nextLevelBpm);

        // 載入下一首
        string nextPath = GetStreamingAssetPath(beatmapFileName);
        yield return StartCoroutine(LoadBeatmapFromStreamingAssets(nextPath));

        isAdvancingScene = false;
    }

    // 判斷傳入的 Note 是否就是 tracks 陣列中的設定物件
    bool IsTrackNote(Note note)
    {
        if (note == null) return false;
        if (tracks == null || tracks.Length == 0) return false;
        var t = note.transform;
        for (int i = 0; i < tracks.Length; i++)
        {
            if (tracks[i] == null) continue;
            if (tracks[i] == t) return true;
        }
        return false;
    }

    // 依序使用 skyboxMaterials 切換天空盒
    void ApplySkyboxByIndex(int index)
    {
        if (skyboxMaterials == null || skyboxMaterials.Length == 0)
        {
            Debug.LogWarning("skyboxMaterials 為空，無法切換天空盒");
            return;
        }
        int idx = index % skyboxMaterials.Length;
        var mat = skyboxMaterials[idx];
        if (mat == null)
        {
            Debug.LogWarning($"skyboxMaterials[{idx}] 為空");
            return;
        }
        RenderSettings.skybox = mat;
        DynamicGI.UpdateEnvironment();
        Debug.Log($"天空盒已切換為 skyboxMaterials[{idx}]");
    }

    // 計算當前歌曲的最大分數和最大連擊數
    public (int maxScore, int maxCombo) GetSongMaxScoreAndCombo()
    {
        if (beatmap == null)
        {
            Debug.LogError("beatmap 為空");
            return (0, 0);
        }

        int totalNotes = hitObjects.Count;
        int maxCombo = totalNotes;
        int maxScore = 0;

        // 計算理論最大分數（假設所有音符都是完美命中 accuracy < 0.5f）
        for (int i = 0; i < totalNotes; i++)
        {
            int currentCombo = i + 1; // 當前連擊數（從1開始）
            int noteScore = 100 + currentCombo; // 根據 CalculateScore 邏輯
            maxScore += noteScore;
        }

        return (maxScore, maxCombo);
    }

    // 逐步顯示轉錄文字（支援完成回調）
    private IEnumerator DisplayTranscriptGradually(System.Action onComplete = null)
    {
        string fullText = "";
        if (sceneCount != 0)
        {
            // 等待 API 完成生成
            while (transcriptGenerator.transcriptText == "Generating..." || transcriptGenerator.transcriptText == null)
            {
                transcriptText.text = "Thinking...";
                yield return new WaitForSeconds(0.5f);
            }

            // 檢查是否有錯誤
            if (transcriptGenerator.transcriptText.StartsWith("Error:"))
            {
                transcriptText.text = "生成失敗，請稍後再試";
                onComplete?.Invoke(); // 即使失敗也要呼叫完成回調
                yield break;
            }

            // 將完整文字分割成單字
            fullText = transcriptGenerator.transcriptText;
        }
        else
        {
            fullText = "Welcome to HarmoSphere!\n A peaceful sanctuary where you can relax.\n Move to the calming music and be yourself.\n You are at peace. Enjoy this moment.";
        }
        string[] words = fullText.Split(new char[] { ' ', '\n', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
        
        transcriptText.text = ""; // 清空顯示
        
        // 一次顯示所有文字
        transcriptText.text = string.Join(" ", words);
        // 等待至多 10 秒（通常由外層控制時間）
        float elapsedTime = 0f;
        while (elapsedTime < 10f && onComplete == null)
        {
            yield return new WaitForSeconds(0.1f);
            elapsedTime += 0.1f;
        }
        
        // 所有文字顯示完成
        onComplete?.Invoke();
    }

    private IEnumerator InspiringMessageDisplay(string skyboxPrompt = "A miserable city at night")
    {
        // sceneCount == 0 時由 PlayIntroAnimation 處理，這裡只處理其他場景
        if (sceneCount == 0)
        {
            yield break;
        }

        // 通用邏輯：適用於 sceneCount >= 1
        bool transcriptCompleted = false;
        bool animationCompleted = false;

        // 1. 開始生成 Skybox，但不自動套用
        changeSceneAPI.ChangeSkybox(skyboxPrompt, false);

        changeSceneAPI.ShowScoreComboPanel(currentScore, currentCombo);

        // 2. 開始生成 Transcript
        changeSceneAPI.GenerateTranscript();

        // 3. 保持 playerModel 顯示，顯示 "Thinking..." 文字
        if (playerModel != null) playerModel.SetActive(true);
        changeSceneAPI.PlayIdleAnimation();
        
        if (transcriptText != null)
        {
            transcriptText.text = "Thinking...";
            transcriptText.gameObject.SetActive(true);
        }

        // 4. 等待 Transcript 生成完成
        while (transcriptGenerator.transcriptText == "Generating..." || string.IsNullOrEmpty(transcriptGenerator.transcriptText))
        {
            yield return new WaitForSeconds(0.5f);
        }

        // 5. Transcript 生成完成，顯示字幕 & 播放動畫
        StartCoroutine(DisplayTranscriptGradually(() => { transcriptCompleted = true; }));
        // 隨機播放動畫 2 或 3
        int animIndex = UnityEngine.Random.Range(2, 4);
        changeSceneAPI.PlayAnimation(animIndex, () => { animationCompleted = true; });

        yield return new WaitUntil(() => animationCompleted && transcriptCompleted);

        // 6. 檢查 Skybox（等待時顯示 "Thinking..."）
        if (!changeSceneAPI.HasCachedSkybox())
        {
            if (transcriptText != null)
            {
                transcriptText.text = "Thinking...";
                transcriptText.gameObject.SetActive(true);
            }
            while (!changeSceneAPI.HasCachedSkybox())
            {
                yield return new WaitForSeconds(0.5f);
            }
            if (transcriptText != null)
            {
                transcriptText.gameObject.SetActive(false);
            }
        }

        // 7. 套用 Skybox
        changeSceneAPI.ApplyCachedSkybox();

        changeSceneAPI.CleanScoreComboPanel();

        Debug.Log($"[InspiringMessageDisplay] sceneCount = {sceneCount} 流程完成");
    }

    // 背景音樂淡出效果
    private IEnumerator FadeOutBGM(AudioSource audioSource, float fadeTime)
    {
        if (audioSource == null)
        {
            Debug.LogWarning("AudioSource 為空，無法執行淡出效果");
            yield break;
        }

        float startVolume = audioSource.volume;
        float elapsedTime = 0f;

        // Debug.Log($"開始淡出 BGM，初始音量: {startVolume}，淡出時間: {fadeTime} 秒");

        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeTime;
            
            // 使用平滑的淡出曲線
            audioSource.volume = Mathf.Lerp(startVolume, 0f, progress);
            
            yield return null;
        }

        // 確保音量完全歸零並停止播放
        audioSource.volume = 0f;
        audioSource.Stop();
        
        // 恢復原始音量以備下次使用
        audioSource.volume = startVolume;

        Debug.Log("BGM 淡出完成並停止播放");
    }

    /// <summary>
    /// 詢問後續心理治療問題 (第 1, 2, 3 題)
    /// </summary>
    IEnumerator AskFollowUpQuestion(int questionNumber)
    {
        Debug.Log($"[FollowUp] Starting question {questionNumber}");

        // 生成 prompt
        string prompt = IntroPromptManager.GetFollowUpPrompt(
            questionNumber,
            userSelectedConcernText,
            userSelectedSkyboxText,
            conversationHistory
        );

        // 隱藏選項按鈕，保持 playerModel 顯示
        if (optionSelectionUI3D != null) optionSelectionUI3D.HideOptions();
        if (playerModel != null) playerModel.SetActive(true);
        changeSceneAPI.PlayIdleAnimation();

        // 顯示 "Thinking..." 文字
        if (transcriptText != null)
        {
            transcriptText.text = "Thinking...";
            transcriptText.gameObject.SetActive(true);
        }

        // 調用 LLM 生成選項
        bool llmCompleted = false;
        yield return StartCoroutine(llmOptionGenerator.GenerateOptionsAsync(prompt, (success) =>
        {
            llmCompleted = true;
            if (success)
            {
                Debug.Log($"[FollowUp] Question {questionNumber} options generated");
            }
            else
            {
                Debug.LogWarning($"[FollowUp] Failed to generate question {questionNumber}, using defaults");
            }
        }));

        // 等待 LLM 完成
        while (!llmCompleted)
        {
            yield return null;
        }

        // LLM 完成，隱藏 "Thinking..." 文字
        if (transcriptText != null)
        {
            transcriptText.text = "";
            transcriptText.gameObject.SetActive(false);
        }

        // 顯示選項並等待用戶選擇
        bool userSelected = false;
        string selectedAnswer = "";

        // 確保 UI 容器開啟
        if (optionSelectionUI3D != null) 
        {
            optionSelectionUI3D.gameObject.SetActive(true);
            Debug.Log("[FollowUp] optionSelectionUI3D activated");
        }
        
        // 確保之前的字幕（如鼓勵話語）被隱藏，避免重疊
        if (transcriptText != null)
        {
            transcriptText.text = "";
            transcriptText.gameObject.SetActive(false);
        }

        optionSelectionUI3D.DisplayOptions(llmOptionGenerator.generatedOptions, (selectedIndex) =>
        {
            selectedAnswer = llmOptionGenerator.generatedOptions.options[selectedIndex];
            userSelected = true;
            Debug.Log($"[FollowUp] User answered Q{questionNumber}: {selectedAnswer}");
        });

        // 等待用戶選擇
        while (!userSelected)
        {
            yield return new WaitForSeconds(0.5f);
        }

        // 記錄到對話歷史
        conversationHistory += $"Q{questionNumber}: {llmOptionGenerator.generatedOptions.welcome}\n";
        conversationHistory += $"A{questionNumber}: {selectedAnswer}\n";
    }

    /// <summary>
    /// 生成並顯示治療師的反饋
    /// </summary>
    IEnumerator DisplayTherapistFeedback()
    {
        Debug.Log("[Feedback] Generating therapist feedback");

        // 生成反饋 prompt
        string feedbackPrompt = IntroPromptManager.GetTherapistFeedbackPrompt(
            userSelectedConcernText,
            userSelectedSkyboxText,
            conversationHistory
        );

        // 調用 LLM 生成反饋
        yield return StartCoroutine(llmOptionGenerator.GenerateOptionsAsync(feedbackPrompt, (success) =>
        {
            if (success)
            {
                Debug.Log("[Feedback] Therapist feedback generated");
            }
        }));

        // 儲存反饋以供稍後在 Skybox 選擇時顯示
        pendingTherapistFeedback = llmOptionGenerator.generatedOptions.welcome;
        Debug.Log($"[Feedback] Feedback stored for later display: {pendingTherapistFeedback}");
    }

    /// <summary>
    /// 播放 10 秒的鼓勵動畫和話語（每關結束後）
    /// </summary>
    IEnumerator PlayEncouragementAnimation()
    {
        Debug.Log("[Encouragement] Starting encouragement process with Loading screen");

        // 轉場開始，顯示容器但隱藏選項按鈕（字幕在容器內）
        if (optionSelectionUI3D != null) 
        {
            optionSelectionUI3D.gameObject.SetActive(true);
            optionSelectionUI3D.HideOptions();
        }

        // 保持 playerModel 顯示，播放 Idle 動畫
        if (playerModel != null) playerModel.SetActive(true);
        changeSceneAPI.PlayIdleAnimation();

        // 顯示 "Thinking..." 文字
        if (transcriptText != null)
        {
            transcriptText.text = "Thinking...";
            transcriptText.gameObject.SetActive(true);
        }

        // 生成鼓勵話語（基於使用者的初始選擇和已回答的問題）
        string encouragementPrompt = LLMPromptManager.GetEncouragementMessage(userSelectedConcernText);
        bool llmCompleted = false;

        // 調用 LLM 生成鼓勵話語
        yield return StartCoroutine(llmOptionGenerator.GenerateOptionsAsync(encouragementPrompt, (success) =>
        {
            llmCompleted = true;
            if (success)
            {
                Debug.Log("[Encouragement] Message generated successfully");
            }
            else
            {
                Debug.LogWarning("[Encouragement] Failed to generate message");
            }
        }));

        // 等待 LLM
        while (!llmCompleted)
        {
            yield return null;
        }

        // 取得生成的鼓勵話語
        string encouragementMessage = llmOptionGenerator.generatedOptions.welcome;


        if (string.IsNullOrEmpty(encouragementMessage))
        {
            encouragementMessage = "Great effort! You're making progress.";
        }

        Debug.Log($"[Encouragement] Message to display: {encouragementMessage}");

        // 先顯示鼓勵話語
        if (transcriptText != null)
        {
            transcriptText.text = encouragementMessage;
            transcriptText.gameObject.SetActive(true);
            Debug.Log("[Encouragement] transcriptText activated and text set");
        }
        else
        {
            Debug.LogError("[Encouragement] transcriptText is null!");
        }

        // 延遲 5 秒後再播放動畫（給字幕生成時間）
        yield return new WaitForSeconds(5f);

        // 播放鼓勵動畫（隨機選擇動畫 2 或 3）
        int animIndex = UnityEngine.Random.Range(2, 4);
        Debug.Log($"[Encouragement] Playing animation index: {animIndex}");
        changeSceneAPI.PlayAnimation(animIndex, null);

        // 等待 5 秒（5 + 5 = 10 秒總時長）
        yield return new WaitForSeconds(5f);

        // 10 秒後隱藏
        if (transcriptText != null)
        {
            transcriptText.gameObject.SetActive(false);
        }
        if (playerModel != null)
        {
            playerModel.SetActive(false);
        }

        Debug.Log("[Encouragement] 15-second encouragement completed");
    }

    /// <summary>
    /// 詢問用戶歌曲速度是否太快
    /// </summary>
    IEnumerator AskAboutBPM()
    {
        Debug.Log("[BPM Question] Starting BPM question");

        string bpmPrompt = LLMPromptManager.GetBPMAdjustmentPrompt();

        // 調用 LLM 生成選項
        yield return StartCoroutine(llmOptionGenerator.GenerateOptionsAsync(bpmPrompt, (success) =>
        {
            if (success)
            {
                Debug.Log("[BPM Question] BPM question options generated");
            }
        }));

        // 顯示選項並等待用戶選擇
        bool userSelected = false;
        int selectedSpeedIndex = -1;
        optionSelectionUI3D.DisplayOptions(llmOptionGenerator.generatedOptions, (selectedIndex) =>
        {
            // 0=太快(降低noteSpeed), 1=正好(保持), 2=太慢(提高noteSpeed)
            selectedSpeedIndex = selectedIndex;
            userSpeedFeedback = llmOptionGenerator.generatedOptions.options[selectedIndex];
            Debug.Log($"[BPM Question] User answered: {userSpeedFeedback}");
            userSelected = true;
        });

        // 等待用戶選擇
        while (!userSelected)
        {
            yield return new WaitForSeconds(0.5f);
        }

        // 根據使用者選擇調整下一關的 noteSpeed
        AdjustNoteSpeedForNextLevel(selectedSpeedIndex);
    }

    /// <summary>
    /// 根據使用者對遊戲速度的反饋調整下一關的 note 移動速度
    /// </summary>
    private void AdjustNoteSpeedForNextLevel(int speedFeedbackIndex)
    {
        if (speedFeedbackIndex == 0)
        {
            // 太快 - 降低速度為 0.85 倍，並降低 BPM
            nextLevelNoteSpeed = noteSpeed * 0.85f;
            nextLevelBpm = Mathf.Max(60, nextLevelBpm - 10); // 降低 10 BPM，最低 60
            // 確保是 10 的倍數
            nextLevelBpm = Mathf.Round(nextLevelBpm / 10f) * 10f;
            Debug.Log($"[NoteSpeed] User thinks too fast. Reducing noteSpeed to {nextLevelNoteSpeed}, BPM to {nextLevelBpm}");
        }
        else if (speedFeedbackIndex == 1)
        {
            // 正好 - 保持速度，BPM 維持 Trajectory 的計算結果
            nextLevelNoteSpeed = noteSpeed;
            // 確保是 10 的倍數
            nextLevelBpm = Mathf.Round(nextLevelBpm / 10f) * 10f;
            Debug.Log($"[NoteSpeed] User happy. Keeping noteSpeed at {noteSpeed}, BPM at {nextLevelBpm}");
        }
        else if (speedFeedbackIndex == 2)
        {
            // 太慢 - 提高速度為 1.15 倍，並提高 BPM
            nextLevelNoteSpeed = noteSpeed / 0.85f;
            nextLevelBpm = Mathf.Min(100, nextLevelBpm + 10); // 提高 10 BPM，最高 100
            // 確保是 10 的倍數
            nextLevelBpm = Mathf.Round(nextLevelBpm / 10f) * 10f;
            Debug.Log($"[NoteSpeed] User thinks too slow. Increasing noteSpeed to {nextLevelNoteSpeed}, BPM to {nextLevelBpm}");
        }
    }

    /// <summary>
    /// 取得用戶選擇的天空球名稱
    /// </summary>
    public string GetSelectedSkybox()
    {
        return userSelectedSkyboxText;
    }

    /// <summary>
    /// 取得下一關的 note 移動速度
    /// </summary>
    public float GetNextLevelNoteSpeed()
    {
        return nextLevelNoteSpeed;
    }
}
using UnityEngine;
using System.Collections;

public class Note : MonoBehaviour
{
    [Header("音符設定")]
    public float hitTime;
    public float endTime;
    public int trackIndex;
    public bool isLongNote;
    public bool isHit = false;

    [Header("長音符軌跡顏色")]
    public Color lineColor = Color.white;

    [Header("粒子顏色設定")]
    public Color particleColor = Color.red;

    [Header("音符方向設定")]
    public NoteDirection noteDirection = NoteDirection.Down;
    public bool useCustomRotation = false;
    public Vector3 customRotationAngles = Vector3.zero;

    [Header("特效系統")]
    public GameObject longNoteParticle;
    public GameObject shortNoteParticle;

    [Header("觸碰設定")]
    public float hitAccuracyRange = 2f;

    // 私有變數
    private float noteSpeed;
    private float judgeLineZ;
    private bool hasPassed = false;
    private RhythmGameManager gameManager;
    
    // 長音符相關
    private bool longNoteActive = false;
    private LineRenderer trailRenderer;
    private bool isLongNoteCompleted = false;
    
    // 觸碰相關
    private bool hasInteracted = false;
    private bool isHandTouching = false;
    private Vector3 lastHandPosition;
    private GameObject currentParticleEffect;

    public enum NoteDirection
    {
        Up, UpRight, Right, DownRight, Down, DownLeft, Left, UpLeft
    }

    void Start()
    {
        gameManager = FindObjectOfType<RhythmGameManager>();
        SetupNote();
    }

    void SetupNote()
    {
        // 設定旋轉
        SetNoteRotation();
        
        // 長音符特殊設定
        if (isLongNote)
        {
            SetupLongNoteTrail();
        }
        
        // 確保有碰撞器
        EnsureCollider();
    }

    void SetNoteRotation()
    {
        Vector3 targetRotation = useCustomRotation ? 
            customRotationAngles : 
            new Vector3(GetXRotationFromDirection(noteDirection), 90f, 180f);
        
        transform.rotation = Quaternion.Euler(targetRotation);
    }

    float GetXRotationFromDirection(NoteDirection direction)
    {
        switch (direction)
        {
            case NoteDirection.Up: return 90f;
            case NoteDirection.UpRight: return 135f;
            case NoteDirection.Right: return 180f;
            case NoteDirection.DownRight: return -135f;
            case NoteDirection.Down: return -90f;
            case NoteDirection.DownLeft: return -45f;
            case NoteDirection.Left: return 0f;
            case NoteDirection.UpLeft: return 45f;
            default: return -90f;
        }
    }

    void EnsureCollider()
    {
        Collider noteCollider = GetComponent<Collider>();
        if (noteCollider == null)
        {
            noteCollider = gameObject.AddComponent<BoxCollider>();
        }
        noteCollider.isTrigger = true;
    }

    void SetupLongNoteTrail()
    {
        trailRenderer = gameObject.AddComponent<LineRenderer>();
        
        // 使用Unity內建材質
        trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
        
        // 設定固定顏色
        trailRenderer.startColor = lineColor;
        trailRenderer.endColor = lineColor;
        
        // 設定線條屬性
        trailRenderer.startWidth = 0.2f;
        trailRenderer.endWidth = 0.15f;
        trailRenderer.positionCount = 2;
        trailRenderer.useWorldSpace = true;
        trailRenderer.sortingOrder = 1;
    }

    // 新增：設定粒子顏色的方法
    void SetParticleColor(GameObject particleObject)
    {
        if (particleObject == null) return;

        // 為 Additive 模式調整顏色 - 降低亮度避免過白
        Color adjustedColor = particleColor * 0.7f; // 調整這個數值來控制亮度 (0.5-0.8)
        adjustedColor.a = particleColor.a; // 保持原始透明度

        // 設定主要粒子系統
        ParticleSystem ps = particleObject.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(adjustedColor);
        }

        // 設定所有子物件的粒子系統
        ParticleSystem[] childPS = particleObject.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem childP in childPS)
        {
            if (childP != ps) // 避免重複設定主要粒子系統
            {
                var main = childP.main;
                main.startColor = new ParticleSystem.MinMaxGradient(adjustedColor);
            }
        }
        
        // Debug.Log($"設定粒子顏色為: {adjustedColor} (原始: {particleColor})");
    }

    public void Initialize(float hitTime, int trackIndex, bool isLongNote, float noteSpeed, float judgeLineZ)
    {
        this.hitTime = hitTime;
        this.trackIndex = trackIndex;
        this.isLongNote = isLongNote;
        this.noteSpeed = noteSpeed;
        this.judgeLineZ = judgeLineZ;
    }

    public void SetNoteRotationFromSettings()
    {
        SetNoteRotation();
    }

    void Update()
    {
        // 已完成的音符不再更新
        if ((isHit && !isLongNote) || (isHit && isLongNote && isLongNoteCompleted)) 
            return;

        // 音符移動
        if (!isHit || (isLongNote && !isLongNoteCompleted))
        {
            transform.position += Vector3.back * noteSpeed * Time.deltaTime;
        }

        // 檢查錯過
        CheckMissed();
        
        // 長音符特殊更新
        if (isLongNote)
        {
            UpdateLongNote();
        }

        // 清理超出範圍的音符
        if (transform.position.z < judgeLineZ - 10f)
        {
            DestroyNote();
        }
    }

    void CheckMissed()
    {
        if (!hasPassed && transform.position.z < judgeLineZ - hitAccuracyRange)
        {
            hasPassed = true;
            gameManager.MissNote();

            if (!isLongNote)
            {
                DestroyNote();
            }
        }
    }

    void UpdateLongNote()
    {
        if (trailRenderer == null) return;

        Vector3 startPos = transform.position;
        float noteLengthInUnits = (endTime - hitTime) * noteSpeed;
        Vector3 endPos = startPos + Vector3.forward * noteLengthInUnits;

        trailRenderer.SetPosition(0, startPos);
        trailRenderer.SetPosition(1, endPos);

        // 檢查長音符是否完成
        if (longNoteActive && Time.time >= endTime)
        {
            CompleteLongNote();
        }
    }

    void CompleteLongNote()
    {
        isLongNoteCompleted = true;

        // 播放短音符特效
        CreateCompletionEffect();

        // 延遲銷毀音符
        Invoke("DestroyNote", 0.5f);
    }

    public void Hit()
    {
        if (isHit) return;

        isHit = true;

        if (isLongNote)
        {
            HitLongNote();
        }
        else
        {
            HitShortNote();
        }
    }

    void HitLongNote()
    {
        longNoteActive = true;
        
        // 視覺回饋
        if (GetComponent<Renderer>() != null)
        {
            GetComponent<Renderer>().material.color = Color.green;
        }

        if (trailRenderer != null)
        {
            trailRenderer.enabled = true;
        }

        // 創建特效
        CreateParticleEffect(longNoteParticle);
    }

    void HitShortNote()
    {
        // 創建特效
        CreateParticleEffect(shortNoteParticle);
        
        // 短音符直接銷毀
        DestroyNote();
    }

    void CreateParticleEffect(GameObject particlePrefab)
    {
        if (particlePrefab == null) return;

        Vector3 effectPosition = GetEffectPosition();

        if (isLongNote)
        {
            // 長音符特效 - 持續存在
            if (currentParticleEffect != null)
            {
                Destroy(currentParticleEffect);
            }

            currentParticleEffect = Instantiate(particlePrefab, effectPosition, Quaternion.identity);
            SetParticleColor(currentParticleEffect);  // 修改：設定粒子顏色
            PlayParticleSystem(currentParticleEffect);
        }
        else
        {
            // 短音符特效 - 獨立播放
            GameObject effect = Instantiate(particlePrefab, effectPosition, Quaternion.identity);
            SetParticleColor(effect);  // 修改：設定粒子顏色
            PlayParticleSystem(effect);
            
            // 自動銷毀
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(effect, 3f);
            }
        }
    }

    void PlayParticleSystem(GameObject effectObject)
    {
        if (effectObject == null) return;

        // 播放主要粒子系統
        ParticleSystem ps = effectObject.GetComponent<ParticleSystem>();
        if (ps != null && !ps.isPlaying)
        {
            ps.Play();
        }

        // 播放子物件粒子系統
        ParticleSystem[] childPS = effectObject.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem childP in childPS)
        {
            if (!childP.isPlaying)
            {
                childP.Play();
            }
        }
    }

    Vector3 GetEffectPosition()
    {
        Vector3 basePosition = (isHandTouching && lastHandPosition != Vector3.zero) ?
            lastHandPosition : transform.position;

        // 在z方向+1
        return basePosition;
    }

    bool IsInHitRange()
    {
        float distanceToJudgeLine = Mathf.Abs(transform.position.z - judgeLineZ);
        return distanceToJudgeLine <= hitAccuracyRange;
    }

    void DestroyNote()
    {
        // 清理長音符特效
        if (currentParticleEffect != null)
        {
            ParticleSystem ps = currentParticleEffect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop();  // 停止發射新粒子，但現有粒子會自然消失
            }
            Destroy(currentParticleEffect, 3f);
        }

        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    void UpdateLongNoteParticle(Vector3 handPosition)
    {
        if (currentParticleEffect != null)
        {
            currentParticleEffect.transform.position = handPosition;
        }
    }

    void CreateCompletionEffect()
    {
        if (shortNoteParticle == null) return;

        Vector3 effectPosition = GetEffectPosition();

        // 長音符完成時播放短音符特效 - 獨立播放
        GameObject effect = Instantiate(shortNoteParticle, effectPosition, Quaternion.identity);
        SetParticleColor(effect);  // 修改：設定粒子顏色
        PlayParticleSystem(effect);
        
        // 自動銷毀
        ParticleSystem ps = effect.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
        }
        else
        {
            Destroy(effect, 3f);
        }
    }

    // MR 觸碰事件
    void OnTriggerEnter(Collider other)
    {
        if (!IsHandCollider(other)) return;

        isHandTouching = true;
        lastHandPosition = other.transform.position;

        if (!IsInHitRange() || hasInteracted) return;

        hasInteracted = true;
        float accuracy = Mathf.Abs(transform.position.z - judgeLineZ);
        gameManager.HitNote(this, accuracy);
        Hit();
    }

    void OnTriggerStay(Collider other)
    {
        if (!IsHandCollider(other)) return;

        lastHandPosition = other.transform.position;

        if (isLongNote && longNoteActive && !isLongNoteCompleted)
        {
            UpdateLongNoteParticle(lastHandPosition);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsHandCollider(other)) return;

        isHandTouching = false;

        // 長音符提前結束邏輯
        if (isLongNote && longNoteActive && !isLongNoteCompleted)
        {
            if (Time.time < endTime)
            {
                gameManager.MissNote();
                CompleteLongNote();
            }
        }
    }

    bool IsHandCollider(Collider collider)
    {
        return collider.gameObject.name.Contains("handMesh") ||
               collider.transform.root.name.Contains("OVRHand") ||
               collider.CompareTag("Hand") ||
               collider.gameObject.layer == LayerMask.NameToLayer("Hand") ||
               collider.gameObject.name.Contains("Hand");
    }

    void OnDestroy()
    {
        CancelInvoke();
    }
}
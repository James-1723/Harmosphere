using UnityEngine;
using System.Collections;
using System;

public class AnimationController : MonoBehaviour
{
    [Header("動畫控制器")]
    public Animator animator;
    
    [Header("動畫狀態名稱設定")]
    [SerializeField] private string idleState = "Idle";
    [SerializeField] private string helloWaveState = "";
    [SerializeField] private string cheerUpState_1 = "";
    [SerializeField] private string cheerUpState_2 = "";
    
    [Header("動畫播放設定")]
    [SerializeField] private float fadeTime = 0.2f;
    [SerializeField] private float helloWaveDuration = 10.0f;  // intro 時長（秒）
    [SerializeField] private float cheerUpDuration = 5.0f;     // 遊戲中的鼓勵動畫
    
    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V)) PlayHelloWave();
        //if (Input.GetKeyDown(KeyCode.B)) PlayExplain();
        //if (Input.GetKeyDown(KeyCode.N)) PlayCheerUp();
        //if (Input.GetKeyDown(KeyCode.M)) PlayPoint();
    }
    
    /// <summary>
    /// 播放 Idle 動畫（站立待機）
    /// </summary>
    public void PlayIdle()
    {
        if (animator != null)
        {
            animator.CrossFade(idleState, fadeTime);
            Debug.Log("播放動畫: Idle 待機");
        }
    }

    public void PlayHelloWave()
    {
        PlayHelloWave(null);
    }

    
    public void PlayHelloWave(Action onComplete)
    {
        if (animator != null)
        {
            animator.CrossFade(helloWaveState, fadeTime);
            StartCoroutine(WaitForHelloWaveComplete(onComplete));
            Debug.Log("播放動畫: 打招呼揮手");
        }
    }

    public void PlayFirstCheerUp()
    {
        PlayFirstCheerUp(null);
    }

    public void PlayFirstCheerUp(Action onComplete)
    {
        if (animator != null)
        {
            Debug.Log($"[AnimationController] 開始播放第一次加油動畫: {cheerUpState_1}");
            Debug.Log($"[AnimationController] Animator 狀態 - enabled: {animator.enabled}, gameObject.activeInHierarchy: {animator.gameObject.activeInHierarchy}");
            
            // 確保動畫控制器啟用
            animator.enabled = true;
            
            // 使用 CrossFadeInFixedTime 或 Play 來確保動畫播放
            animator.CrossFade(cheerUpState_1, fadeTime, 0, 0f);
            
            // 檢查當前動畫狀態
            StartCoroutine(CheckAnimationState());
            
            StartCoroutine(WaitForCheerUpComplete(onComplete));
            Debug.Log("播放動畫: 第一次加油鼓勵");
        }
        else
        {
            Debug.LogError("[AnimationController] Animator 為空！");
        }
    }

    public void PlaySecondCheerUp()
    {
        PlaySecondCheerUp(null);
    }

    public void PlaySecondCheerUp(Action onComplete)
    {
        if (animator != null)
        {
            animator.CrossFade(cheerUpState_2, fadeTime);
            StartCoroutine(WaitForCheerUpComplete(onComplete));
            Debug.Log("播放動畫: 第二次加油鼓勵");
        }
    }
    
    public void PlayAnimation(string animationName)
    {
        if (animator != null)
        {
            animator.CrossFade(animationName, fadeTime);
            Debug.Log($"播放動畫: {animationName}");
        }
    }
    
    public void SetAnimationSpeed(float speed)
    {
        if (animator != null)
        {
            animator.speed = speed;
            Debug.Log($"設定動畫速度: {speed}");
        }
    }
    
    private IEnumerator WaitForThreeSeconds(Action onComplete = null)
    {
        yield return new WaitForSeconds(3.0f);
        Debug.Log("三秒等待完成");
        onComplete?.Invoke();
    }

    private IEnumerator WaitForHelloWaveComplete(Action onComplete = null)
    {
        yield return new WaitForSeconds(helloWaveDuration); // 使用可配置的動畫持續時間
        Debug.Log($"HelloWave 動畫播放完成 ({helloWaveDuration} 秒)");
        onComplete?.Invoke();
    }

    private IEnumerator WaitForCheerUpComplete(Action onComplete = null)
    {
        yield return new WaitForSeconds(cheerUpDuration); // 使用可配置的動畫持續時間
        Debug.Log($"CheerUp 動畫播放完成 ({cheerUpDuration} 秒)");
        onComplete?.Invoke();
    }
    
    private IEnumerator CheckAnimationState()
    {
        yield return new WaitForSeconds(0.5f); // 等待動畫開始
        
        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
            
            Debug.Log($"[AnimationController] 當前動畫狀態:");
            Debug.Log($"  - State Name Hash: {stateInfo.shortNameHash}");
            Debug.Log($"  - Is Playing: {!stateInfo.IsTag("Idle")}");
            Debug.Log($"  - Normalized Time: {stateInfo.normalizedTime}");
            Debug.Log($"  - Speed: {animator.speed}");
            
            if (clipInfo.Length > 0)
            {
                Debug.Log($"  - Current Clip: {clipInfo[0].clip.name}");
            }
            else
            {
                Debug.LogWarning("  - 沒有正在播放的動畫片段！");
            }
        }
    }
}
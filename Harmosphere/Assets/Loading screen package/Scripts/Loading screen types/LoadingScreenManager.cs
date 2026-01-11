using UnityEngine;
using System.Collections;

public class LoadingScreenManager : MonoBehaviour
{
    private Animator _animatorComponent;
    private bool _isRevealing = false;
    private bool _pendingHide = false;

    private void Start()
    {
        _animatorComponent = transform.GetComponent<Animator>();  

        // Remove it if you don't want to hide it in the Start function and call it elsewhere
        HideLoadingScreen();
        // RevealLoadingScreen();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RevealLoadingScreen();
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            HideLoadingScreen();
        }
    }

    public void RevealLoadingScreen()
    {
        _isRevealing = true;
        _pendingHide = false;
        _animatorComponent.SetTrigger("Reveal");
    }

    public void HideLoadingScreen()
    {
        Debug.Log("[LoadingScreenManager] HideLoadingScreen called");
        
        // 如果正在 reveal 動畫中，標記 pending hide，等 reveal 完成後再 hide
        if (_isRevealing)
        {
            Debug.Log("[LoadingScreenManager] Still revealing, marking pending hide");
            _pendingHide = true;
            return;
        }
        
        // Call this function, if you want start hiding the loading screen
        _animatorComponent.SetTrigger("Hide");
    }

    public System.Action onHideComplete;

    public void OnFinishedReveal()
    {
        Debug.Log("[LoadingScreenManager] OnFinishedReveal called");
        _isRevealing = false;
        
        // 如果有 pending hide 請求，直接完成（跳過 hide 動畫以加快流程）
        if (_pendingHide)
        {
            Debug.Log("[LoadingScreenManager] Processing pending hide - immediate completion");
            _pendingHide = false;
            // 直接觸發 hide 並立即呼叫完成回調
            _animatorComponent.SetTrigger("Hide");
            // 延遲一幀後直接呼叫 OnFinishedHide，確保快速完成
            StartCoroutine(ImmediateHideComplete());
        }
    }
    
    private System.Collections.IEnumerator ImmediateHideComplete()
    {
        // 等待一幀讓動畫開始
        yield return null;
        // 強制跳到 hide 動畫的結束狀態
        if (_animatorComponent != null)
        {
            // 播放 hide 動畫但立即跳到結束
            _animatorComponent.Play("Hide", 0, 1f); // 直接跳到動畫結尾
        }
        yield return null;
        // 直接呼叫完成回調，不等動畫播完
        OnFinishedHide();
    }

    public void OnFinishedHide()
    {
        Debug.Log("[LoadingScreenManager] OnFinishedHide called");
        _isRevealing = false;
        _pendingHide = false;
        
        // 防止重複呼叫
        var callback = onHideComplete;
        onHideComplete = null; // Clear first to prevent double calls
        callback?.Invoke();
    }

}

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;

/// <summary>
/// 3D 版本的選項選擇 UI 控制器
/// 使用 3D TextMeshPro，鍵盤輸入（1、2、3）選擇
/// </summary>
public class OptionSelectionUI3D : MonoBehaviour
{
    [Header("3D Text Elements")]
    public TextMeshPro welcomeText;           // 歡迎文字 (3D TextMeshPro)
    public TextMeshPro[] optionTexts = new TextMeshPro[3];  // 三個選項文字
    
    [Header("Option Buttons (for state reset)")]
    public GameObject[] optionButtons = new GameObject[3];  // 選項按鈕的父物件

    private int selectedOptionIndex = -1;
    private bool hasSelectedOption = false;
    private Action<int> onOptionSelected;

    void Start()
    {
        // 初始化隱藏 UI
        // gameObject.SetActive(false);
        welcomeText.gameObject.SetActive(false);
        for (int i = 0; i < optionTexts.Length; i++)
        {
            optionTexts[i].gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!gameObject.activeSelf) return;
        if (hasSelectedOption) return;

        // 檢測鍵盤輸入 (1、2、3)
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            OnOptionSelected(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            OnOptionSelected(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            OnOptionSelected(2);
        }
    }

    public void OptionSelection(int id)
    {
        OnOptionSelected(id);
    }

    private void OnOptionSelected(int optionIndex)
    {
        if (hasSelectedOption) return;

        hasSelectedOption = true;
        selectedOptionIndex = optionIndex;

        Debug.Log($"[OptionSelectionUI3D] User selected option {optionIndex}: {optionTexts[optionIndex].text}");

        // 調用回調
        if (onOptionSelected != null)
        {
            onOptionSelected.Invoke(optionIndex);
        }

        // 隱藏選項（但保留容器）
        HideOptions();
    }

    /// <summary>
    /// 顯示選項並等待用戶選擇
    /// </summary>
    public void DisplayOptions(LLMOptions options, Action<int> callback)
    {
        // 清除當前選取的 UI 物件，避免殘留選取狀態
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        onOptionSelected = callback;
        selectedOptionIndex = -1;
        hasSelectedOption = false;

        // 重置所有選項按鈕的狀態
        ResetAllOptionStates();

        // 顯示歡迎文字
        if (welcomeText != null)
        {
            welcomeText.text = options.welcome;
            welcomeText.gameObject.SetActive(true);
        }

        // 顯示選項文字
        for (int i = 0; i < 3; i++)
        {
            if (optionTexts[i] != null)
            {
                optionTexts[i].text = $"{i + 1}. {options.options[i]}";
                optionTexts[i].gameObject.SetActive(true);
            }
        }

        // 顯示整個 UI 物件
        gameObject.SetActive(true);

        Debug.Log("[OptionSelectionUI3D] Options displayed. Press 1, 2, or 3 to select.");
    }
    
    /// <summary>
    /// 重置所有選項按鈕的狀態（Animator、Button 等）
    /// </summary>
    private void ResetAllOptionStates()
    {
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] != null)
            {
                ResetOptionState(optionButtons[i]);
            }
            // 如果 optionButtons 沒有設定，嘗試使用 optionTexts 的父物件
            else if (i < optionTexts.Length && optionTexts[i] != null)
            {
                // 嘗試找到選項按鈕的父物件
                Transform parent = optionTexts[i].transform.parent;
                if (parent != null)
                {
                    ResetOptionState(parent.gameObject);
                }
            }
        }
    }
    
    /// <summary>
    /// 重置單個選項按鈕的狀態
    /// </summary>
    private void ResetOptionState(GameObject optionObj)
    {
        if (optionObj == null) return;
        
        // 重置 Animator 狀態
        Animator animator = optionObj.GetComponent<Animator>();
        if (animator != null)
        {
            // 重置所有觸發器和布林參數
            animator.SetBool("Open", false);
            animator.SetBool("Selected", false);
            animator.SetBool("Highlighted", false);
            animator.SetBool("Pressed", false);
            animator.SetBool("Disabled", false);
            
            // 嘗試播放 Normal 或 Closed 狀態
            animator.Play("Normal", 0, 0f);
            
            Debug.Log($"[OptionSelectionUI3D] Reset animator state for {optionObj.name}");
        }
        
        // 重置 Button 狀態
        Button button = optionObj.GetComponent<Button>();
        if (button != null)
        {
            // 強制重置按鈕的選中狀態
            button.OnDeselect(null);
            
            // 重新啟用按鈕的互動
            button.interactable = false;
            button.interactable = true;
        }
        
        // 重置 Selectable 狀態
        Selectable selectable = optionObj.GetComponent<Selectable>();
        if (selectable != null)
        {
            selectable.OnDeselect(null);
        }
    }

    /// <summary>
    /// 隱藏選項按鈕和歡迎文字（但保留容器以便顯示其他內容如字幕）
    /// </summary>
    public void HideOptions()
    {
        // 隱藏歡迎文字
        if (welcomeText != null)
        {
            welcomeText.gameObject.SetActive(false);
        }
        
        // 隱藏所有選項按鈕
        for (int i = 0; i < optionTexts.Length; i++)
        {
            if (optionTexts[i] != null)
            {
                optionTexts[i].gameObject.SetActive(false);
            }
        }
        
        Debug.Log("[OptionSelectionUI3D] Options hidden (container remains active)");
    }
    
    /// <summary>
    /// 完全隱藏整個選項 UI 容器
    /// </summary>
    public void HideContainer()
    {
        gameObject.SetActive(false);
    }

    public int GetSelectedOptionIndex()
    {
        return selectedOptionIndex;
    }

    public bool HasSelectedOption()
    {
        return hasSelectedOption;
    }
}

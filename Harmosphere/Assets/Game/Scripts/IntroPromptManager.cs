using UnityEngine;

/// <summary>
/// 專門管理遊戲介紹序列的 LLM Prompt
/// 負責定義和返回適當的 prompt 內容
/// </summary>
public static class IntroPromptManager
{
    public static string GetIntroPrompt()
    {
        return LLMPromptManager.GetInitialConcernPrompt();
    }

    public static string GetSkyboxPrompt()
    {
        return LLMPromptManager.GetSkyboxPrompt();
    }

    /// <summary>
    /// 生成帶有正面回饋的 Skybox prompt（每關結束後）
    /// </summary>
    public static string GetSkyboxPromptWithFeedback(string userConcern, string conversationHistory)
    {
        return LLMPromptManager.GetSkyboxPromptWithFeedback(userConcern, conversationHistory);
    }

    /// <summary>
    /// 生成心理治療式的後續問題（每關結束後）
    /// </summary>
    public static string GetFollowUpPrompt(int questionNumber, string userConcern, string selectedSkybox, string previousAnswers)
    {
        if (questionNumber == 1)
        {
            return LLMPromptManager.GetFollowUpQuestion1(userConcern, selectedSkybox, previousAnswers);
        }
        else if (questionNumber == 2)
        {
            return LLMPromptManager.GetFollowUpQuestion2(userConcern, selectedSkybox, previousAnswers);
        }
        else if (questionNumber == 3)
        {
            return LLMPromptManager.GetFollowUpQuestion3(userConcern, selectedSkybox, previousAnswers);
        }

        return "";
    }

    /// <summary>
    /// 生成治療師的反饋訊息
    /// </summary>
    public static string GetTherapistFeedbackPrompt(string userConcern, string selectedSkybox, string conversationHistory)
    {
        return LLMPromptManager.GetTherapistFeedback(userConcern, selectedSkybox, conversationHistory);
    }
}

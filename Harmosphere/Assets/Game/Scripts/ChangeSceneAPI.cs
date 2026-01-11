using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;

public class ChangeSceneAPI : MonoBehaviour
{
    public SkyboxAPI skyboxAPI;
    public AnimationController animationController;
    public TranscriptGenerator transcriptGenerator;
    public GameObject scoreComboPanel;
    public TextMeshPro scoreText;
    public TextMeshPro comboText;
    public Material[] universeSkyboxes;
    public Material[] natureSkyboxes;
    public Material[] citySkyboxes;
    public Material[] coldSkyboxes;
    public Material[] warmSkyboxes;

    public void GenerateTranscript()
    {
        transcriptGenerator.GenerateTranscript();
    }

    public void ChangeSkybox(string prompt, bool autoApply = true)
    {
        skyboxAPI.SetAutoApplyToScene(autoApply);
        // string prompt = "A miserable city at night"; // Removed hardcoded prompt
        skyboxAPI.GenerateSkybox(prompt);
    }

    public void ChangeSkyboxByCategory(int categoryIndex)
    {
        Material[] targetSkyboxes = null;

        switch (categoryIndex)
        {
            case 0: // Universe
                targetSkyboxes = universeSkyboxes;
                break;
            case 1: // Nature (Ocean)
                targetSkyboxes = natureSkyboxes;
                break;
            case 2: // City
                targetSkyboxes = citySkyboxes;
                break;
        }

        ApplySkyboxFromList(targetSkyboxes, $"Category {categoryIndex}");
    }

    public string ChangeSkyboxByScore(bool improved, bool randomAll)
    {
        Material[] targetSkyboxes = null;

        if (randomAll)
        {
            // Randomly pick between Cold and Warm
            if (UnityEngine.Random.value > 0.5f)
            {
                targetSkyboxes = coldSkyboxes;
            }
            else
            {
                targetSkyboxes = warmSkyboxes;
            }
        }
        else
        {
            if (improved)
            {
                targetSkyboxes = coldSkyboxes;
            }
            else
            {
                targetSkyboxes = warmSkyboxes;
            }
        }

        return ApplySkyboxFromList(targetSkyboxes, $"Score Logic (Improved: {improved}, RandomAll: {randomAll})");
    }

    private string ApplySkyboxFromList(Material[] skyboxes, string context)
    {
        if (skyboxes != null && skyboxes.Length > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, skyboxes.Length);
            Material selectedSkybox = skyboxes[randomIndex];
            
            if (selectedSkybox != null)
            {
                RenderSettings.skybox = selectedSkybox;
                DynamicGI.UpdateEnvironment();
                Debug.Log($"[ChangeSceneAPI] Changed skybox [{context}]. Index: {randomIndex}");
                return selectedSkybox.name;
            }
            else
            {
                Debug.LogWarning($"[ChangeSceneAPI] Selected skybox at index {randomIndex} is null! Context: {context}");
                return "Unknown Environment";
            }
        }
        else
        {
            Debug.LogWarning($"[ChangeSceneAPI] No skyboxes found! Context: {context}");
            return "Unknown Environment";
        }
    }

    public void ShowScoreComboPanel(int score, int combo)
    {
        scoreComboPanel.SetActive(true);
        scoreText.text = score.ToString();
        comboText.text = combo.ToString() + " combo!";
    }

    public void CleanScoreComboPanel()
    {
        scoreComboPanel.SetActive(false);
        scoreText.text = "";
        comboText.text = "";
    }

    public void PlayAnimation(int animationIndex, Action onComplete = null)
    {
        switch (animationIndex)
        {
            case 0:
                animationController.PlayIdle();
                onComplete?.Invoke();
                break;
            case 1:
                animationController.PlayHelloWave(onComplete);
                break;
            case 2:
                animationController.PlayFirstCheerUp(onComplete);
                break;
            case 3:
                animationController.PlaySecondCheerUp(onComplete);
                break;
        }
    }
    
    /// <summary>
    /// 播放 Idle 待機動畫
    /// </summary>
    public void PlayIdleAnimation()
    {
        animationController.PlayIdle();
    }

    public void RevealLoader()
    {
        skyboxAPI.revealLoader(true);
    }

    public void HideLoader(Action onComplete = null)
    {
        if (skyboxAPI.loadingAnimation != null)
        {
            if (onComplete != null)
            {
                skyboxAPI.loadingAnimation.onHideComplete += onComplete;
            }
            skyboxAPI.loadingAnimation.HideLoadingScreen();
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    public bool HasCachedSkybox()
    {
        return skyboxAPI.HasCachedSkybox();
    }

    public void ApplyCachedSkybox()
    {
        skyboxAPI.ApplyCachedSkybox();
    }
}
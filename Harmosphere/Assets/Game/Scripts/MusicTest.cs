using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class MusicTest : MonoBehaviour
{
    public MusicGPTClient musicGPTClient;
    public Button button;
    public Button button2;
    public string mp3Path;
    public MapperatorClient mapperatorClient;

    void Start()
    {
        button.onClick.AddListener(GenerateMusic);
        button2.onClick.AddListener(GenerateMusicMsp);
    }

    void GenerateMusic()
    {
        Debug.Log("Generate Music");
        musicGPTClient.GenerateMusic(100, 100);
    }

    void GenerateMusicMsp()
    {
        mapperatorClient.GenerateBeatmap(mp3Path, 
        onSuccess: (osuPath) => {
            Debug.Log($"Success! Beatmap saved at: {osuPath}");
        }, 
        onError: (error) => {
            Debug.LogError($"Error: {error}");
        }
    );
    }
}
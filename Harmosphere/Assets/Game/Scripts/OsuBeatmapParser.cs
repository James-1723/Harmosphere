using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class HitObject
{
    public int x, y;
    public int time;
    public int type;
    public int endTime; // 長音符結束時間
}

[System.Serializable]
public class TimingPoint
{
    public float time;
    public float beatLength;
    public int meter;
    public int sampleSet;
    public int sampleIndex;
    public int volume;
    public bool uninherited;
    public int effects;
}

[System.Serializable]
public class OsuBeatmap
{
    public string audioFilename;
    public float previewTime;
    public string title;
    public string artist;
    public string creator;
    public string version;
    
    public float hpDrainRate;
    public float circleSize;
    public float overallDifficulty;
    public float approachRate;
    
    public List<TimingPoint> timingPoints = new List<TimingPoint>();
    public List<HitObject> hitObjects = new List<HitObject>();
}

public static class OsuBeatmapParser
{
    public static OsuBeatmap ParseBeatmap(string filePath)
    {
        try
        {
            string[] lines = File.ReadAllLines(filePath);
            return ParseBeatmapFromLines(lines);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"讀取beatmap文件失敗: {e.Message}");
            throw;
        }
    }
    
    public static OsuBeatmap ParseBeatmapFromString(string beatmapData)
    {
        try
        {
            string[] lines = beatmapData.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
            return ParseBeatmapFromLines(lines);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"解析beatmap數據失敗: {e.Message}");
            throw;
        }
    }
    
    static OsuBeatmap ParseBeatmapFromLines(string[] lines)
    {
        OsuBeatmap beatmap = new OsuBeatmap();
        string currentSection = "";
        
        Debug.Log($"開始解析beatmap，共 {lines.Length} 行");
        
        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("//"))
                continue;
                
            if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
            {
                currentSection = trimmedLine;
                Debug.Log($"進入區段: {currentSection}");
                continue;
            }
            
            switch (currentSection)
            {
                case "[General]":
                    ParseGeneral(beatmap, trimmedLine);
                    break;
                case "[Metadata]":
                    ParseMetadata(beatmap, trimmedLine);
                    break;
                case "[Difficulty]":
                    ParseDifficulty(beatmap, trimmedLine);
                    break;
                case "[TimingPoints]":
                    ParseTimingPoint(beatmap, trimmedLine);
                    break;
                case "[HitObjects]":
                    ParseHitObject(beatmap, trimmedLine);
                    break;
            }
        }
        
        Debug.Log($"解析完成: {beatmap.title} by {beatmap.artist}");
        Debug.Log($"音符數量: {beatmap.hitObjects.Count}");
        Debug.Log($"Timing Points: {beatmap.timingPoints.Count}");
        
        return beatmap;
    }
    
    static void ParseGeneral(OsuBeatmap beatmap, string line)
    {
        var parts = line.Split(':');
        if (parts.Length < 2) return;
        
        string key = parts[0].Trim();
        string value = parts[1].Trim();
        
        switch (key)
        {
            case "AudioFilename":
                beatmap.audioFilename = value;
                Debug.Log($"音頻文件: {value}");
                break;
            case "PreviewTime":
                float.TryParse(value, out beatmap.previewTime);
                break;
        }
    }
    
    static void ParseMetadata(OsuBeatmap beatmap, string line)
    {
        var parts = line.Split(':');
        if (parts.Length < 2) return;
        
        string key = parts[0].Trim();
        string value = parts[1].Trim();
        
        switch (key)
        {
            case "Title":
                beatmap.title = value;
                break;
            case "Artist":
                beatmap.artist = value;
                break;
            case "Creator":
                beatmap.creator = value;
                break;
            case "Version":
                beatmap.version = value;
                break;
        }
    }
    
    static void ParseDifficulty(OsuBeatmap beatmap, string line)
    {
        var parts = line.Split(':');
        if (parts.Length < 2) return;
        
        string key = parts[0].Trim();
        string value = parts[1].Trim();
        
        switch (key)
        {
            case "HPDrainRate":
                float.TryParse(value, out beatmap.hpDrainRate);
                break;
            case "CircleSize":
                float.TryParse(value, out beatmap.circleSize);
                break;
            case "OverallDifficulty":
                float.TryParse(value, out beatmap.overallDifficulty);
                break;
            case "ApproachRate":
                float.TryParse(value, out beatmap.approachRate);
                break;
        }
    }
    
    static void ParseTimingPoint(OsuBeatmap beatmap, string line)
    {
        var parts = line.Split(',');
        if (parts.Length < 2) return;
        
        TimingPoint tp = new TimingPoint();
        
        if (float.TryParse(parts[0], out tp.time) &&
            float.TryParse(parts[1], out tp.beatLength))
        {
            if (parts.Length > 2) int.TryParse(parts[2], out tp.meter);
            if (parts.Length > 3) int.TryParse(parts[3], out tp.sampleSet);
            if (parts.Length > 4) int.TryParse(parts[4], out tp.sampleIndex);
            if (parts.Length > 5) int.TryParse(parts[5], out tp.volume);
            if (parts.Length > 6) tp.uninherited = parts[6] == "1";
            if (parts.Length > 7) int.TryParse(parts[7], out tp.effects);
            
            beatmap.timingPoints.Add(tp);
        }
    }
    
    static void ParseHitObject(OsuBeatmap beatmap, string line)
    {
        var parts = line.Split(',');
        if (parts.Length < 4) return;
        
        HitObject hitObj = new HitObject();
        
        if (int.TryParse(parts[0], out hitObj.x) &&
            int.TryParse(parts[1], out hitObj.y) &&
            int.TryParse(parts[2], out hitObj.time) &&
            int.TryParse(parts[3], out hitObj.type))
        {
            // 長音符處理 (type & 128 != 0)
            if ((hitObj.type & 128) != 0 && parts.Length > 5)
            {
                var endTimeStr = parts[5].Split(':')[0];
                int.TryParse(endTimeStr, out hitObj.endTime);
            }
            
            beatmap.hitObjects.Add(hitObj);
        }
    }
}
// ==================== ErrorDisplayManager.cs ====================
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class ErrorDisplayManager : MonoBehaviour
{
    [Header("Display Settings")]
    public TextMeshPro errorText;
    public int maxErrorLines = 5;  // Maximum error lines to display
    public float errorLifeTime = 30f;  // Display time for each error (seconds)

    [Header("Color Settings")]
    public Color errorColor = Color.red;
    public Color warningColor = Color.yellow;
    public Color logColor = Color.white;
    public Color exceptionColor = Color.magenta;

    [Header("Font Settings")]
    public float fontSize = 4f;
    public float lineSpacing = 0.2f;

    [Header("Position Settings")]
    public Vector3 displayPosition = new Vector3(0, 5, 10);
    public bool followCamera = false;
    public float cameraDistance = 5f;

    private Queue<ErrorEntry> errorQueue = new Queue<ErrorEntry>();
    private List<ErrorEntry> currentErrors = new List<ErrorEntry>();

    [System.Serializable]
    public class ErrorEntry
    {
        public string message;
        public string stackTrace;
        public LogType type;
        public float timeStamp;
        public Color color;

        public ErrorEntry(string msg, string stack, LogType logType, Color c)
        {
            message = msg;
            stackTrace = stack;
            type = logType;
            timeStamp = Time.time;
            color = c;
        }
    }

    void Awake()
    {
        // Register Unity's log callback
        Application.logMessageReceived += HandleLog;

        SetupErrorDisplay();
    }

    void OnDestroy()
    {
        // Unregister to avoid memory leak
        Application.logMessageReceived -= HandleLog;
    }

    void SetupErrorDisplay()
    {
        if (errorText == null)
        {
            // Auto create TextMeshPro component
            GameObject textObj = new GameObject("ErrorDisplay");
            textObj.transform.SetParent(transform);
            textObj.transform.localPosition = displayPosition;

            errorText = textObj.AddComponent<TextMeshPro>();
            errorText.color = Color.white;
            errorText.text = "Error Display Ready\n";
            errorText.alignment = TextAlignmentOptions.TopLeft;
        }

        // Set initial position
        transform.position = displayPosition;

        // Apply font size to bound or newly created TextMeshPro
        if (errorText != null)
        {
            errorText.fontSize = fontSize;
        }
    }

    void Update()
    {
        // Follow camera
        if (followCamera && Camera.main != null)
        {
            Vector3 cameraPos = Camera.main.transform.position;
            Vector3 cameraForward = Camera.main.transform.forward;
            transform.position = cameraPos + cameraForward * cameraDistance;
            transform.LookAt(Camera.main.transform);
        }

        // Clean up old errors
        CleanupOldErrors();

        // Update display
        UpdateErrorDisplay();
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Only display Error / Exception
        // if (type != LogType.Error && type != LogType.Warning)
        if (type != LogType.Error)
        {
            return;
        }
        Color messageColor = GetColorForLogType(type);

        // Create error entry
        ErrorEntry newError = new ErrorEntry(logString, stackTrace, type, messageColor);

        // Add to queue
        errorQueue.Enqueue(newError);
        currentErrors.Add(newError);

        // Limit error count
        while (currentErrors.Count > maxErrorLines)
        {
            currentErrors.RemoveAt(0);
        }

        // Update display immediately
        UpdateErrorDisplay();
    }

    Color GetColorForLogType(LogType type)
    {
        switch (type)
        {
            case LogType.Error:
                return errorColor;
            case LogType.Warning:
                return warningColor;
            case LogType.Log:
                return logColor;
            case LogType.Exception:
                return exceptionColor;
            case LogType.Assert:
                return exceptionColor;
            default:
                return logColor;
        }
    }

    void CleanupOldErrors()
    {
        float currentTime = Time.time;

        // Remove expired errors
        for (int i = currentErrors.Count - 1; i >= 0; i--)
        {
            if (currentTime - currentErrors[i].timeStamp > errorLifeTime)
            {
                currentErrors.RemoveAt(i);
            }
        }
    }

    void UpdateErrorDisplay()
    {
        if (errorText == null) return;

        if (currentErrors.Count == 0)
        {
            errorText.text = "<color=#808080>No Errors - System Running</color>";
            return;
        }

        string displayText = "";

        // Add title
        displayText += $"<color=#00FF00>[ERRORS: {currentErrors.Count}]</color>\n";
        displayText += "<color=#808080>--------------------</color>\n";

        // Display errors (newest on top)
        for (int i = currentErrors.Count - 1; i >= 0; i--)
        {
            ErrorEntry error = currentErrors[i];

            // Calculate remaining time
            float remainingTime = errorLifeTime - (Time.time - error.timeStamp);

            // Add prefix based on error type
            string prefix = GetPrefixForLogType(error.type);

            // Format timestamp
            string timeStr = System.DateTime.Now.ToString("HH:mm:ss");

            // Build error message (limit length to avoid UI clutter)
            string shortMessage = error.message.Length > 100 ?
                error.message.Substring(0, 100) + "..." : error.message;

            string colorHex = ColorUtility.ToHtmlStringRGB(error.color);

            displayText += $"<color=#{colorHex}>[{timeStr}] {prefix}: {shortMessage}</color>\n";

            // Show remaining time (optional)
            if (remainingTime < 5f)
            {
                displayText += $"<color=#FF8080>  -> Expires in {remainingTime:F1}s</color>\n";
            }

            displayText += "\n";
        }

        errorText.text = displayText;
    }

    // Clean message to remove characters that might not display properly in TextMeshPro
    string CleanMessageForDisplay(string message)
    {
        if (string.IsNullOrEmpty(message)) return "";

        // Remove emoji and special Unicode characters that might cause display issues
        string cleaned = "";
        foreach (char c in message)
        {
            // Keep only basic ASCII characters, spaces, and common punctuation
            if ((c >= 32 && c <= 126) || c == '\n' || c == '\r')
            {
                cleaned += c;
            }
            else
            {
                // Replace problematic characters with underscore
                cleaned += "_";
            }
        }

        return cleaned;
    }

    string GetPrefixForLogType(LogType type)
    {
        switch (type)
        {
            case LogType.Error:
                return "ERROR";
            case LogType.Warning:
                return "WARN";
            case LogType.Log:
                return "INFO";
            case LogType.Exception:
                return "EXCEPTION";
            case LogType.Assert:
                return "ASSERT";
            default:
                return "LOG";
        }
    }

    // Public method: Manually clear all errors
    [ContextMenu("Clear All Errors")]
    public void ClearAllErrors()
    {
        currentErrors.Clear();
        errorQueue.Clear();
        UpdateErrorDisplay();
        Debug.Log("All errors cleared from display");
    }

    // Public method: Add custom error
    public void AddCustomError(string message, LogType type = LogType.Error)
    {
        HandleLog(message, "", type);
    }

    // Public method: Toggle camera follow
    public void ToggleCameraFollow()
    {
        followCamera = !followCamera;
    }

    // Test method: Generate test errors
    [ContextMenu("Generate Test Errors")]
    public void GenerateTestErrors()
    {
        Debug.Log("This is a test log message");
        Debug.LogWarning("This is a test warning message");
        Debug.LogError("This is a test error message");

        try
        {
            throw new System.Exception("This is a test exception");
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }
    }
}
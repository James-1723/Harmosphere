using UnityEngine;
using System.Reflection;

public class SimplePressHold : MonoBehaviour
{
    public Timer timer;
    private bool isPressed = false;

    void Start()
    {
        // 訂閱計時器結束事件
        if (timer != null)
        {
            timer.onTimerEnd.AddListener(OnTimerComplete);
            // 初始隱藏計時器
            SetTimerVisibility(false);
        }
    }

    void OnMouseDown()
    {
        Debug.Log("開始按住計時");
        isPressed = true;

        // 強制重置Timer狀態
        ResetTimerState();

        // 顯示計時器
        SetTimerVisibility(true);

        // 啟動Timer
        timer.StartTimer();
    }

    void OnMouseUp()
    {
        Debug.Log("放開按鈕，停止計時");
        isPressed = false;

        // 停止計時器
        if (timer != null)
        {
            timer.StopTimer();
        }

        // 隱藏計時器
        SetTimerVisibility(false);
    }

    void Update()
    {
        // 如果不是按住狀態，但計時器還在運行，則停止計時器
        if (!isPressed && timer != null)
        {
            var timerType = timer.GetType();
            var timerRunningField = timerType.GetField("timerRunning", BindingFlags.NonPublic | BindingFlags.Instance);
            bool timerRunning = (bool)timerRunningField.GetValue(timer);

            if (timerRunning)
            {
                timer.StopTimer();
                // 如果強制停止，也隱藏計時器
                SetTimerVisibility(false);
            }
        }
    }

    void OnTimerComplete()
    {
        Debug.Log("計時完成！刪除物件");

        // 隱藏計時器
        SetTimerVisibility(false);

        // 計時完成後刪除此物件（不是Timer）
        Destroy(gameObject);
    }

    void ResetTimerState()
    {
        var timerType = timer.GetType();

        // 重置內部狀態
        var timerRunningField = timerType.GetField("timerRunning", BindingFlags.NonPublic | BindingFlags.Instance);
        var timerPausedField = timerType.GetField("timerPaused", BindingFlags.NonPublic | BindingFlags.Instance);

        if (timerRunningField != null)
            timerRunningField.SetValue(timer, false);
        if (timerPausedField != null)
            timerPausedField.SetValue(timer, false);

        Debug.Log("Timer狀態已重置");
    }

    void SetTimerVisibility(bool visible)
    {
        if (timer != null)
        {
            timer.gameObject.SetActive(visible);
            Debug.Log($"計時器顯示狀態：{(visible ? "顯示" : "隱藏")}");
        }
    }

    void OnDestroy()
    {
        // 清理事件訂閱，避免記憶體洩漏
        if (timer != null)
        {
            timer.onTimerEnd.RemoveListener(OnTimerComplete);
        }
    }
}
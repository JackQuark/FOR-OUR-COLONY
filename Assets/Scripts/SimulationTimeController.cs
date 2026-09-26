using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SimulationTimeController : MonoBehaviour
{
    [Header("UI")]
    public Button pauseButton;
    public Button speedButton;
    public TMP_Text pauseButtonText;
    public TMP_Text speedButtonText;

    [Header("Speed")]
    public float normalSpeed = 1f;
    public float fastSpeed = 2f;

    bool isPaused;
    bool isFast;

    void Start()
    {
        ApplyTimeScale();
        RefreshButtonText();
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        ApplyTimeScale();
        RefreshButtonText();
    }

    public void ToggleSpeed()
    {
        isFast = !isFast;
        ApplyTimeScale();
        RefreshButtonText();
    }

    void ApplyTimeScale()
    {
        if (isPaused)
        {
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = isFast ? fastSpeed : normalSpeed;
        }
    }

    void RefreshButtonText()
    {
        if (pauseButtonText != null)
        {
            pauseButtonText.text = isPaused ? "Start" : "Pause";
        }

        if (speedButtonText != null)
        {
            speedButtonText.text = isFast ? "2x speed" : "1x speed";
        }
    }
}
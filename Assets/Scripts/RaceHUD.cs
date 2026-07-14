using UnityEngine;
using TMPro;

public class RaceHUD : MonoBehaviour
{
    public RunningController runningController;
    public TMP_Text liveTimerText;   // shows running time while racing
    public TMP_Text resultText;      // shows final time + high score after finish

    private const string HighScoreKey = "100mRush_BestTime";

    void OnEnable()
    {
        runningController.OnRaceFinished += HandleRaceFinished;
    }

    void OnDisable()
    {
        runningController.OnRaceFinished -= HandleRaceFinished;
    }

    void Update()
    {
        if (runningController.raceStarted && liveTimerText != null)
        {
            liveTimerText.text = FormatTime(runningController.RaceTimer);
        }
    }

    void HandleRaceFinished(float finalTime)
    {
        float bestTime = PlayerPrefs.GetFloat(HighScoreKey, float.MaxValue);
        bool isNewBest = finalTime < bestTime;

        if (isNewBest)
        {
            PlayerPrefs.SetFloat(HighScoreKey, finalTime);
            PlayerPrefs.Save();
            bestTime = finalTime;
        }

        if (resultText != null)
        {
            resultText.text = isNewBest
                ? $"Time: {FormatTime(finalTime)}\nNEW BEST TIME!"
                : $"Time: {FormatTime(finalTime)}\nBest: {FormatTime(bestTime)}";
        }
    }

    string FormatTime(float t) => t.ToString("0.00") + "s";
}
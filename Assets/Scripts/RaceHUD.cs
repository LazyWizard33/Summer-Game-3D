using UnityEngine;
using TMPro;

public class RaceHUD : MonoBehaviour
{
    public RunningController runningController;
    public TMP_Text liveTimerText;   // shows running time while racing
    public TMP_Text resultText;      // shows final time + high score after finish

    public string highScoreKey = "100mRush_BestTime"; // CHANGED — now editable per instance in the Inspector

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
        float bestTime = PlayerPrefs.GetFloat(highScoreKey, float.MaxValue); // CHANGED — uses highScoreKey instead of HighScoreKey
        bool isNewBest = finalTime < bestTime;

        if (isNewBest)
        {
            PlayerPrefs.SetFloat(highScoreKey, finalTime); // CHANGED
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
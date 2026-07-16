using System;
using System.Collections;
using UnityEngine;

public class HurdleController : MonoBehaviour
{
    [Header("References")]
    public RunningController runningController;

    [Header("Hurdle Layout")]
    public float[] hurdleZPositions = { 15f, 25f, 35f, 45f, 55f };

    [Header("Timing")]
    public float approachWindowSeconds = 1.5f;

    [Header("Penalty / Jump")]
    public float wrongTapPenalty = 2f;
    public float missedHurdlePenalty = 2.5f; // if you never tap in time
    public float jumpHopHeight = 0.6f;
    public float jumpHopDuration = 0.35f;

    private int nextHurdleIndex = 0;
    private bool inChoiceMode = false;
    private bool hurdleResultPending = false;
    private TapSide jumpSide; // which side is the correct button this time

    public event Action<TapSide> OnHurdleChoiceShown; // tells UI which side is the real Jump button
    public event Action<int> OnHurdleCleared;
    public event Action<int> OnHurdleFailed;
    public event Action OnAllHurdlesComplete;

    void Update()
    {
        if (nextHurdleIndex >= hurdleZPositions.Length) return;

        float z = transform.position.z;
        float targetZ = hurdleZPositions[nextHurdleIndex];
        float speed = runningController.currentSpeed;

        if (!inChoiceMode && !hurdleResultPending && speed > 0.1f)
        {
            float timeRemaining = (targetZ - z) / speed;
            if (timeRemaining <= approachWindowSeconds)
            {
                ShowHurdleChoice();
            }
        }

        // Reached the hurdle without picking anything — automatic fail
        if (inChoiceMode && z >= targetZ)
        {
            FailHurdle();
        }

        if (Input.GetKeyDown(KeyCode.Keypad4)) OnTapSide(TapSide.Left);
        if (Input.GetKeyDown(KeyCode.Keypad6)) OnTapSide(TapSide.Right);
    }

    void ShowHurdleChoice()
    {
        inChoiceMode = true;
        jumpSide = UnityEngine.Random.value < 0.5f ? TapSide.Left : TapSide.Right;

        runningController.FreezeRunningInput();
        OnHurdleChoiceShown?.Invoke(jumpSide);
    }

    public void OnTapSide(TapSide tapped)
    {
        if (!inChoiceMode) return;

        inChoiceMode = false;
        hurdleResultPending = true;

        if (tapped == jumpSide)
        {
            StartCoroutine(HopThenContinue(true));
            OnHurdleCleared?.Invoke(nextHurdleIndex);
        }
        else
        {
            runningController.currentSpeed = Mathf.Max(0f, runningController.currentSpeed - wrongTapPenalty);
            StartCoroutine(HopThenContinue(false));
            OnHurdleFailed?.Invoke(nextHurdleIndex);
        }
    }

    public void OnLeftTap() => OnTapSide(TapSide.Left);
    public void OnRightTap() => OnTapSide(TapSide.Right);

    void FailHurdle()
    {
        inChoiceMode = false;
        hurdleResultPending = true;
        runningController.currentSpeed = Mathf.Max(0f, runningController.currentSpeed - missedHurdlePenalty);
        StartCoroutine(HopThenContinue(false));
        OnHurdleFailed?.Invoke(nextHurdleIndex);
    }

    IEnumerator HopThenContinue(bool success)
    {
        Vector3 startPos = transform.position;
        float duration = success ? jumpHopDuration : jumpHopDuration * 0.6f;
        float height = success ? jumpHopHeight : 0.1f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float arc = height * 4f * t * (1f - t);
            transform.position = new Vector3(startPos.x, startPos.y + arc, transform.position.z);
            yield return null;
        }

        transform.position = new Vector3(startPos.x, startPos.y, transform.position.z);

        nextHurdleIndex++;
        hurdleResultPending = false;

        if (nextHurdleIndex >= hurdleZPositions.Length)
            OnAllHurdlesComplete?.Invoke();
        else
            runningController.UnfreezeRunningInput();
    }
}
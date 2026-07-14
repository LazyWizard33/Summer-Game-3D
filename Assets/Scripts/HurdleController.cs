using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HurdleController : MonoBehaviour
{
    [Header("References")]
    public RunningController runningController;

    [Header("Hurdle Layout")]
    public float[] hurdleZPositions = { 15f, 25f, 35f, 45f, 55f };

    [Header("Timing")]
    public float approachWindowSeconds = 1.5f;
    public int sequenceLength = 3;

    [Header("Penalty / Jump")]
    public float wrongSequencePenalty = 2.5f;
    public float jumpHopHeight = 0.6f;
    public float jumpHopDuration = 0.35f;

    private int nextHurdleIndex = 0;
    private bool inSequenceMode = false;
    private List<TapSide> currentSequence = new List<TapSide>(); // CHANGED — reuses TapSide, not a new enum
    private int currentInputIndex = 0;
    private bool hurdleResultPending = false;

    public event Action<List<TapSide>> OnSequenceGenerated;
    public event Action<int, bool> OnInputProgress;
    public event Action<int> OnHurdleCleared;
    public event Action<int> OnHurdleFailed;
    public event Action OnAllHurdlesComplete;

    void Update()
    {
        if (nextHurdleIndex >= hurdleZPositions.Length) return;

        float z = transform.position.z;
        float targetZ = hurdleZPositions[nextHurdleIndex];
        float speed = runningController.currentSpeed;

        if (!inSequenceMode && !hurdleResultPending && speed > 0.1f)
        {
            float timeRemaining = (targetZ - z) / speed;
            if (timeRemaining <= approachWindowSeconds)
            {
                StartHurdleSequence();
            }
        }

        if (inSequenceMode && z >= targetZ)
        {
            FailHurdle();
        }

        // Same keys as your existing running controls — no new bindings needed
        if (Input.GetKeyDown(KeyCode.Keypad4)) OnTapSide(TapSide.Left);  // Green
        if (Input.GetKeyDown(KeyCode.Keypad6)) OnTapSide(TapSide.Right); // Blue
    }

    void StartHurdleSequence()
    {
        inSequenceMode = true;
        currentInputIndex = 0;
        currentSequence.Clear();

        for (int i = 0; i < sequenceLength; i++)
        {
            currentSequence.Add(UnityEngine.Random.value < 0.5f ? TapSide.Left : TapSide.Right);
        }

        runningController.FreezeRunningInput();
        OnSequenceGenerated?.Invoke(currentSequence);
    }

    public void OnTapSide(TapSide tapped)
    {
        if (!inSequenceMode) return;

        bool correct = tapped == currentSequence[currentInputIndex];
        OnInputProgress?.Invoke(currentInputIndex, correct);

        if (!correct)
        {
            FailHurdle();
            return;
        }

        currentInputIndex++;

        if (currentInputIndex >= currentSequence.Count)
        {
            ClearHurdle();
        }
    }

    // Call these from your EXISTING LeftTapZone / RightTapZone buttons
    public void OnLeftTap() => OnTapSide(TapSide.Left);   // Green
    public void OnRightTap() => OnTapSide(TapSide.Right); // Blue

    void ClearHurdle()
    {
        inSequenceMode = false;
        hurdleResultPending = true;
        StartCoroutine(HopThenContinue(true));
        OnHurdleCleared?.Invoke(nextHurdleIndex);
    }

    void FailHurdle()
    {
        inSequenceMode = false;
        hurdleResultPending = true;
        runningController.currentSpeed = Mathf.Max(0f, runningController.currentSpeed - wrongSequencePenalty);
        StartCoroutine(HopThenContinue(false));
        OnHurdleFailed?.Invoke(nextHurdleIndex);
    }

    IEnumerator HopThenContinue(bool success)
    {
        Vector3 startPos = transform.position;
        float duration = success ? jumpHopDuration : jumpHopDuration * 0.6f;
        float height = success ? jumpHopHeight : 0.15f;

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
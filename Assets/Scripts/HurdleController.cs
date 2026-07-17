using System;
using System.Collections;
using UnityEngine;

public class HurdleController : MonoBehaviour
{
    [Header("References")]
    public RunningController runningController;

    [Header("Hurdle Layout")]
    public int hurdleCount = 5;
    public float hurdleGap = 18f;
    private float[] hurdleZPositions;
    private float startZ;

    [Header("Timing")]
    public float stopRunningWindowSeconds = 2f; // NEW — dots stop this many seconds before the hurdle
    public float choiceWindowSeconds = 1f;      // NEW — jump/fake icons appear this many seconds before the hurdle

    [Header("Penalty / Jump")]
    public float wrongTapPenalty = 2f;
    public float missedHurdlePenalty = 2.5f;
    public float jumpHopHeight = 0.6f;
    public float jumpHopDuration = 0.35f;

    private int nextHurdleIndex = 0;
    private bool hasFrozenForThisHurdle = false; // NEW — tracks stage 1 (dots stopped)
    private bool inChoiceMode = false;            // stage 2 (icons showing)
    private bool hurdleResultPending = false;
    private TapSide jumpSide;

    public event Action<TapSide> OnHurdleChoiceShown;
    public event Action<int> OnHurdleCleared;
    public event Action<int> OnHurdleFailed;
    public event Action OnAllHurdlesComplete;

    void Awake()
    {
        startZ = transform.position.z;

        hurdleZPositions = new float[hurdleCount];
        for (int i = 0; i < hurdleCount; i++)
        {
            hurdleZPositions[i] = startZ + hurdleGap * (i + 1);
        }
    }

    void Update()
    {
        if (nextHurdleIndex >= hurdleZPositions.Length) return;

        float z = transform.position.z;
        float targetZ = hurdleZPositions[nextHurdleIndex];
        float speed = runningController.currentSpeed;

        if (!hurdleResultPending && speed > 0.1f)
        {
            float timeRemaining = (targetZ - z) / speed;

            // Stage 1 — stop the running dots first, no icons yet
            if (!hasFrozenForThisHurdle && timeRemaining <= stopRunningWindowSeconds)
            {
                hasFrozenForThisHurdle = true;
                runningController.FreezeRunningInput();
            }

            // Stage 2 — now show the Jump/Fake icons
            if (hasFrozenForThisHurdle && !inChoiceMode && timeRemaining <= choiceWindowSeconds)
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

        OnHurdleChoiceShown?.Invoke(jumpSide); // running is already frozen from Stage 1
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
        hasFrozenForThisHurdle = false; // NEW — reset for the next hurdle

        if (nextHurdleIndex >= hurdleZPositions.Length)
            OnAllHurdlesComplete?.Invoke();
        else
            runningController.UnfreezeRunningInput();
    }
}
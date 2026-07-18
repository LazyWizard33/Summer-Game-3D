using System;
using UnityEngine;

public enum TapSide { Left, Right }

public class RunningController : MonoBehaviour
{
    [Header("Race Start")]
    public bool raceStarted = false;
    private bool hasStartedMoving = false;

    [Header("Pace Settings")]
    public float targetFinishTime = 13.3f; // steady/baseline pace
    public float bestFinishTime = 9f;      // fastest possible pace with perfect tapping
    private float baseSpeed;               // calculated: raceDistance / targetFinishTime
    private float maxSpeed;                // calculated: raceDistance / bestFinishTime
    public float BaseSpeed => baseSpeed;
    public float MaxSpeed => maxSpeed;

    [Header("Timing Window")]
    public float perfectTapWindow = 0.15f; // tap within this time of prompt = max speed (bestFinishTime pace)
    public float maxTapWindow = 0.6f;      // tap slower than this = minimum gain, still correct, baseSpeed pace

    [Header("Speed Settings")]
    public float currentSpeed = 0f;
    public float speedDecayPerSecond = 0.6f;
    public float wrongTapPenalty = 2f;
    public float speedHeadroom = 0.5f; // only used when useRampSystem is OFF (100m Rush behavior)
    public float minimumSpeed = 2f; // NEW — player can never drop below this once moving

    [Header("Speed Ramp System (used for Long Jump / other build-up events)")]
    public bool useRampSystem = false; // false = old 100m behavior, true = ramp-based build-up
    [Range(0f, 1f)] public float fastTapThreshold = 0.45f;
    public float rampIncreasePerFastTap = 0.35f;
    public float rampDecreasePerSlowTap = 0.12f;
    public float rampDecayPerSecond = 0.15f;
    private float rampT = 0f; // 0 = at baseSpeed, 1 = at maxSpeed

    [Header("Race Settings")]
    public float raceDistance = 100f;
    public float distanceTravelled = 0f;
    private bool raceFinished = false;
    private float raceTimer = 0f;
    public float RaceTimer => raceTimer;

    public TapSide currentPrompt { get; private set; }
    private float promptStartTime;
    private int consecutiveSameSideCount = 0;

    private bool inputFrozen = false;

    public event Action<TapSide> OnNewPrompt;
    public event Action<bool, TapSide> OnTapResult;
    public event Action<float> OnRaceFinished;
    public event Action OnRunningFrozen;
    public event Action OnRunningUnfrozen;

    void Awake()
    {
        baseSpeed = raceDistance / targetFinishTime;

        maxSpeed = useRampSystem
            ? raceDistance / bestFinishTime
            : (raceDistance / bestFinishTime) * speedHeadroom;
    }

    void FixedUpdate()
    {
        if (!raceStarted || raceFinished || !hasStartedMoving) return;
        transform.position += Vector3.forward * currentSpeed * Time.fixedDeltaTime;
    }

    void Update()
    {
        if (!raceStarted || raceFinished) return;

        if (hasStartedMoving)
        {
            raceTimer += Time.deltaTime;

            if (!inputFrozen)
            {
                if (useRampSystem)
                {
                    rampT = Mathf.Max(0f, rampT - rampDecayPerSecond * Time.deltaTime);
                    currentSpeed = Mathf.Lerp(baseSpeed, maxSpeed, rampT);
                }
                else if (currentSpeed > baseSpeed)
                {
                    currentSpeed = Mathf.Max(baseSpeed, currentSpeed - speedDecayPerSecond * Time.deltaTime);
                }

                currentSpeed = Mathf.Max(currentSpeed, minimumSpeed); // NEW — enforce floor after any decay
            }

            distanceTravelled += currentSpeed * Time.deltaTime;

            if (distanceTravelled >= raceDistance)
            {
                distanceTravelled = raceDistance;
                raceFinished = true;
                OnRaceFinished?.Invoke(raceTimer);
            }
        }

        if (!inputFrozen)
        {
            if (Input.GetKeyDown(KeyCode.Keypad4)) OnLeftTap();
            if (Input.GetKeyDown(KeyCode.Keypad6)) OnRightTap();
        }
    }

    public void StartRace()
    {
        raceStarted = true;
        hasStartedMoving = true; // CHANGED — was false; now starts moving immediately on GO
        raceTimer = 0f;
        distanceTravelled = 0f;
        currentSpeed = minimumSpeed; // CHANGED — was 0; now starts at the speed floor instead of standing still
        rampT = 0f;
        consecutiveSameSideCount = 0;
        inputFrozen = false;
        PickNewPrompt();
    }

    public void FreezeRunningInput()
    {
        if (inputFrozen) return;
        inputFrozen = true;
        OnRunningFrozen?.Invoke();
    }

    public void UnfreezeRunningInput()
    {
        if (!inputFrozen) return;
        inputFrozen = false;
        PickNewPrompt();
        OnRunningUnfrozen?.Invoke();
    }

    public void OnLeftTap() => HandleTap(TapSide.Left);
    public void OnRightTap() => HandleTap(TapSide.Right);

    void HandleTap(TapSide tappedSide)
    {
        if (!raceStarted || raceFinished || inputFrozen) return;

        bool correct = tappedSide == currentPrompt;

        if (correct)
        {
            float reactionTime = Time.time - promptStartTime;
            float t = Mathf.InverseLerp(maxTapWindow, perfectTapWindow, reactionTime);
            t = Mathf.Clamp01(t);

            if (!hasStartedMoving)
            {
                hasStartedMoving = true;
            }

            if (useRampSystem)
            {
                if (t >= fastTapThreshold)
                    rampT = Mathf.Clamp01(rampT + rampIncreasePerFastTap);
                else
                    rampT = Mathf.Clamp01(rampT - rampDecreasePerSlowTap);

                currentSpeed = Mathf.Lerp(baseSpeed, maxSpeed, rampT);
            }
            else
            {
                float targetSpeed = Mathf.Lerp(baseSpeed, maxSpeed, t);
                currentSpeed = Mathf.Max(currentSpeed, targetSpeed);
                currentSpeed = Mathf.Min(currentSpeed, maxSpeed);
            }

            currentSpeed = Mathf.Max(currentSpeed, minimumSpeed); // NEW — enforce floor here too
            OnTapResult?.Invoke(true, tappedSide);
            PickNewPrompt();
        }
        else
        {
            if (useRampSystem)
            {
                rampT = Mathf.Clamp01(rampT - rampDecreasePerSlowTap);
                currentSpeed = Mathf.Max(baseSpeed * 0.3f, Mathf.Lerp(baseSpeed, maxSpeed, rampT) - wrongTapPenalty);
            }
            else
            {
                currentSpeed = Mathf.Max(0, currentSpeed - wrongTapPenalty);
            }

            currentSpeed = Mathf.Max(currentSpeed, minimumSpeed); // NEW — enforce floor after wrong-tap penalty
            OnTapResult?.Invoke(false, tappedSide);
        }
    }

    void PickNewPrompt()
    {
        TapSide newPrompt;

        if (consecutiveSameSideCount >= 2)
            newPrompt = (currentPrompt == TapSide.Left) ? TapSide.Right : TapSide.Left;
        else
            newPrompt = (UnityEngine.Random.value < 0.5f) ? TapSide.Left : TapSide.Right;

        if (newPrompt == currentPrompt)
            consecutiveSameSideCount++;
        else
            consecutiveSameSideCount = 1;

        currentPrompt = newPrompt;
        promptStartTime = Time.time;
        OnNewPrompt?.Invoke(currentPrompt);
    }
}
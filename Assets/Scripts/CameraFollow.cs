using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 3f, -4f);

    void LateUpdate()
    {
        if (target == null)
            return;

        transform.position = target.position + offset;
    }

    // Call this to change what the camera follows
    public void SetTarget(Transform newTarget)
    {
        if (newTarget == null)
            return;

        target = newTarget;
    }
}
using UnityEngine;
using UnityEngine.Splines;

public class MoveAroundClosedSpline : MonoBehaviour
{
    public SplineContainer splineContainer;
    public float duration = 5f; // Time to complete one full loop

    private float t = 0f;

    void Update()
    {
        if (splineContainer == null) return;

        float delta = Time.deltaTime / duration;
        t += delta;
        t %= 1f;

        splineContainer.Evaluate(t, out var position, out var tangent, out var upVector);
        transform.position = position;

        // Face movement direction
        Quaternion lookRotation = Quaternion.LookRotation(tangent, upVector);

        // Apply 90° Y rotation (world up) and 45° forward Z tilt (relative to object's local forward)
        Quaternion yRotation = Quaternion.Euler(0f, 90f, 0f);
        Quaternion zTilt = Quaternion.Euler(0f, 0f, 45f);

        // Final rotation
        transform.rotation = lookRotation * yRotation * zTilt;
    }
}

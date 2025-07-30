using UnityEngine;
using UnityEngine.Splines;

public class MoveAlongSpline : MonoBehaviour
{
    public SplineContainer splineContainer;
    public float duration = 5f;

    private float t = 0f;
    private bool movingForward = true;

    void Update()
    {
        if (splineContainer == null) return;

        float delta = Time.deltaTime / duration;
        t += movingForward ? delta : -delta;

        if (t >= 1f)
        {
            t = 1f;
            movingForward = false;
        }
        else if (t <= 0f)
        {
            t = 0f;
            movingForward = true;
        }

        splineContainer.Evaluate(t, out var position, out var tangent, out var upVector);
        transform.position = position;

        Quaternion lookRotation = Quaternion.LookRotation(
            movingForward ? tangent : -tangent,
            upVector
        );

        Quaternion rightTurn = Quaternion.AngleAxis(90f, upVector);
        Quaternion finalRotation = lookRotation * rightTurn;

        // Lock X rotation
        Vector3 euler = finalRotation.eulerAngles;
        euler.x = 0f;
        euler.z = 0f;
        transform.rotation = Quaternion.Euler(euler);
    }
}

using UnityEngine;

public class CopyRotationOfObject : MonoBehaviour
{
	[SerializeField] private Transform target;

	[Header("Clamp (degrees)")]
	[SerializeField] private float minX = -75f;
	[SerializeField] private float maxX = 75f;

	private void Update()
	{
		if (!target) return;

		// Get target's world euler
		Vector3 targetEuler = target.rotation.eulerAngles;

		// Convert 0–360 → -180–180 so clamping makes sense
		float x = targetEuler.x;
		if (x > 180f) x -= 360f;

		// Clamp
		x = Mathf.Clamp(x, minX, maxX);

		// Apply clamped X, keep current Y/Z
		Vector3 myEuler = transform.rotation.eulerAngles;
		transform.rotation = Quaternion.Euler(x, myEuler.y, myEuler.z);
	}
}

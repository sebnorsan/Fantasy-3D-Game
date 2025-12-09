using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class CopyRotationOfObject : NetworkBehaviour
{
	[SerializeField] private Transform target;
	[SerializeField] private Transform targetToRotate;

	[Header("Clamp (degrees)")]
	[SerializeField] private float minX = -75f;
	[SerializeField] private float maxX = 75f;

	private void Update()
	{
		if (!IsOwner) return;
		if (!target) return;

		// Get target's world euler
		Vector3 targetEuler = target.localRotation.eulerAngles;

		// Convert 0–360 → -180–180 so clamping makes sense
		float x = targetEuler.x;
		if (x > 180f) x -= 360f;

		// Clamp
		x = Mathf.Clamp(x, minX, maxX);

		// Apply clamped X, keep current Y/Z
		Vector3 myEuler = targetToRotate.localRotation.eulerAngles;
		targetToRotate.localRotation = Quaternion.Euler(x, myEuler.y, myEuler.z);
	}
}

using Unity.Netcode;
using UnityEngine;

public class CopyRotationOfObject : MonoBehaviour
{
	[SerializeField] private PlayerReferences pRef;
	[SerializeField] private Transform target;

	[Space(10)]
	[SerializeField] private bool followCamTransform;
	[SerializeField] private Transform camTransform;
	[SerializeField] private float reduceByExtra = -.3f;

	// For following camera height
	private Vector3 initialLocalPos;
	private float initialCamLocalY;

	[Header("Clamp (degrees)")]
	[SerializeField] private float minX = -75f;
	[SerializeField] private float maxX = 75f;

	private void Start()
	{
		if (camTransform)
		{
			initialLocalPos = transform.localPosition;
			initialCamLocalY = camTransform.localPosition.y;
		}
	}

	private void LateUpdate()
	{
		if (!GetComponentInParent<NetworkObject>().IsOwner) return;

		if (!target) return;

		// --- rotation clamp ---
		Vector3 targetEuler = target.localRotation.eulerAngles;

		float x = targetEuler.x;
		if (x > 180f) x -= 360f;

		x = Mathf.Clamp(x, minX, maxX);

		Vector3 myEuler = transform.localRotation.eulerAngles;
		transform.localRotation = Quaternion.Euler(x, myEuler.y, myEuler.z);

		// --- follow camera Y when crouching / moving camera ---
		if (followCamTransform && camTransform)
		{
			float camDeltaY = camTransform.localPosition.y - initialCamLocalY;

			if (pRef.playerController.isCrouching)
				camDeltaY -= reduceByExtra;

			Vector3 lp = transform.localPosition;
			lp.y = initialLocalPos.y + camDeltaY;
			transform.localPosition = lp;
		}
	}
}

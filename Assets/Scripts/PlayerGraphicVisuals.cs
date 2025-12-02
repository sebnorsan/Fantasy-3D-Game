using Unity.Netcode;
using UnityEngine;

public class PlayerGraphicVisuals : NetworkBehaviour
{
	private PlayerController localPlayer;
	private Quaternion baseRot;
	[SerializeField] private GameObject gfx;

	[SerializeField] private float maxForwardTilt = 10f; // forward/back
	[SerializeField] private float maxSideTilt = 8f;      // strafing
	[SerializeField] private float tiltSpeed = 8f;

	public override void OnNetworkSpawn()
	{
		if (IsOwner)
			gfx.SetActive(false);
	}

	private void Start()
	{
		localPlayer = GetComponentInParent<PlayerController>();
		baseRot = transform.localRotation;
	}

	private void Update()
	{
		if (localPlayer == null) return;

		// Forward (W/S) -> X tilt, Strafe (A/D) -> Z tilt
		float targetX = -localPlayer.inputVertical * maxForwardTilt; // forward = tilt back
		float targetZ = -localPlayer.inputHorizontal * maxSideTilt;  // right = tilt left

		Quaternion targetRot = baseRot * Quaternion.Euler(targetX, 0f, targetZ);
		gfx.transform.localRotation = Quaternion.Lerp(
			gfx.transform.localRotation,
			targetRot,
			Time.deltaTime * tiltSpeed
		);
	}
}

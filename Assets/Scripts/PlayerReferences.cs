using EZCameraShake;
using Unity.Netcode;
using UnityEngine;

public class PlayerReferences : NetworkBehaviour
{
	public PlayerController playerController;
    public PlayerAnimator playerAnimator;
    public PlayerGraphicVisuals playerGraphics;
    public PlayerDamagable playerDamagable;
	public EZCameraShake.CameraShaker cameraShaker;
    public CharacterController playerCharacterController;
	public Camera playerCam;

	[Space(10)]
	public Transform crouchCamPoint;

	[Space(10)]

    public BowScript bowScript;
    public BowNetCode bowNetCode;

    [Space(10)]

    public LayerMask groundLayerMask;

	public override void OnNetworkSpawn()
	{
		if (IsOwner)
			playerCharacterController.enabled = false;

        Invoke(nameof(EnableCharacterController), 1f);
	}
    private void EnableCharacterController()
    {
		playerCharacterController.enabled = true;
	}
}

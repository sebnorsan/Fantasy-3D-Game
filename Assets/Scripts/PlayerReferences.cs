using EZCameraShake;
using UnityEngine;

public class PlayerReferences : MonoBehaviour
{
	public PlayerController playerController;
    public PlayerAnimator playerAnimator;
    public PlayerGraphicVisuals playerGraphics;
    public PlayerDamagable playerDamagable;
	public EZCameraShake.CameraShaker cameraShaker;
    public CharacterController playerCharacterController;
	public Camera playerCam;

    [Space(10)]

    public BowScript bowScript;
    public BowNetCode bowNetCode;

    [Space(10)]

    public LayerMask groundLayerMask;

    public bool IsOwner => playerController.IsOwner;
}

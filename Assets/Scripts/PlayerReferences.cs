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
	public PlayerPvP playerPvP;

	[Space(10)]
	public Transform crouchCamPoint;

	[Space(10)]

    public BowScript bowScript;
    public BowEffects bowEffects;
	public ArrowParticles arrowParticles;
    public BowNetCode bowNetCode;

    [Space(10)]

    public LayerMask groundLayerMask;
    public LayerMask headLayerMask;

	public override void OnNetworkSpawn()
	{
		if (IsOwner)
			playerCharacterController.enabled = false;
	}

	#region InitialPlayerCheck
	private PlayerController playerCheck = null;
	private void Update()
	{
		if (playerCheck == null)
		{
			var localClient = NetworkManager.Singleton.LocalClient;
			if (localClient != null && localClient.PlayerObject != null)
			{
				playerCheck = localClient.PlayerObject.GetComponent<PlayerReferences>().playerController;
				Invoke(nameof(OnPlayerFound), .4f);
			}
		}
	}
	private void OnPlayerFound()
	{
		playerCharacterController.enabled = true;
	}
	#endregion;
}

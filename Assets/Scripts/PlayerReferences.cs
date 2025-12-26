using EZCameraShake;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerReferences : NetworkBehaviour
{
	public PlayerController playerController;
    public PlayerAnimator playerAnimator;
    public PlayerGraphicVisuals playerGraphics;
    public PlayerDamagable playerDamagable;
	public CameraShaker cameraShaker;
    public CharacterController playerCharacterController;
	public Camera playerCam;
	public PlayerPvP playerPvP;
	public PlayerMultipliers playerMultipliers;

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

	public static readonly List<PlayerReferences> All = new();

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (!All.Contains(this)) All.Add(this);

		if (IsOwner)
			playerCharacterController.enabled = false;
	}

	private void OnDisable()
	{
		All.Remove(this);
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

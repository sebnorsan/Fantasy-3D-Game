using TMPro;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using Steamworks;

public class PlayerGraphicVisuals : NetworkBehaviour
{
	private PlayerController localPlayer;
	private Quaternion baseRot;
	[SerializeField] private GameObject gfx;
	[SerializeField] private TextMeshProUGUI textUsername;

	[SerializeField] private float maxForwardTilt = 10f;
	[SerializeField] private float maxSideTilt = 8f;
	[SerializeField] private float tiltSpeed = 8f;

	// synced name
	public NetworkVariable<FixedString32Bytes> PlayerName =
		new NetworkVariable<FixedString32Bytes>("Player");

	public override void OnNetworkSpawn()
	{
		if (IsOwner)
		{
			gfx.SetActive(false);
			SetNameServerRpc(SteamClient.Name);   // use Steam username
		}

		PlayerName.OnValueChanged += OnNameChanged;
		OnNameChanged(default, PlayerName.Value);
	}

	[ServerRpc]
	void SetNameServerRpc(string name)
	{
		PlayerName.Value = name;
	}

	void OnNameChanged(FixedString32Bytes _, FixedString32Bytes newName)
	{
		textUsername.text = newName.ToString();
	}

	private void Start()
	{
		localPlayer = GetComponentInParent<PlayerController>();
		baseRot = transform.localRotation;
	}

	private void Update()
	{
		if (localPlayer == null) return;

		float targetX = -localPlayer.inputVertical * maxForwardTilt;
		float targetZ = -localPlayer.inputHorizontal * maxSideTilt;

		Quaternion targetRot = baseRot * Quaternion.Euler(targetX, 0f, targetZ);
		gfx.transform.localRotation = Quaternion.Lerp(
			gfx.transform.localRotation,
			targetRot,
			Time.deltaTime * tiltSpeed
		);
	}
}

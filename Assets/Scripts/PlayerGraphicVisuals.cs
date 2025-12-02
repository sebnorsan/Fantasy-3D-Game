using TMPro;
using Unity.Netcode;
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

	// store the name on the server so it can be re-sent
	private string cachedName;

	public override void OnNetworkSpawn()
	{
		localPlayer = GetComponentInParent<PlayerController>();
		baseRot = transform.localRotation;

		if (IsOwner)
		{
			if (gfx != null)
				gfx.SetActive(false);

			// send our Steam name once when we spawn
			SetNameServerRpc(SteamClient.Name);
		}
		else
		{
			// I’m a copy of SOMEONE ELSE’s player – ask server what their name is
			RequestNameServerRpc();
		}
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void SetNameServerRpc(string name)
	{
		cachedName = name;
		SetNameClientRpc(name);
	}

	// called by late joiners to get the name again
	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void RequestNameServerRpc()
	{
		if (!string.IsNullOrEmpty(cachedName))
			SetNameClientRpc(cachedName);
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void SetNameClientRpc(string name)
	{
		if (textUsername != null)
			textUsername.text = name;
	}

	private void Update()
	{
		if (localPlayer == null || gfx == null) return;

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

using Steamworks;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerPvP : NetworkBehaviour
{
	[SerializeField] private PlayerReferences pRef;

	public NetworkVariable<int> Kills = new NetworkVariable<int>();
	public NetworkVariable<int> Deaths = new NetworkVariable<int>();

	public NetworkVariable<FixedString64Bytes> PlayerName =
	new NetworkVariable<FixedString64Bytes>(
		default,
		NetworkVariableReadPermission.Everyone,
		NetworkVariableWritePermission.Owner);

	public NetworkVariable<ulong> SteamId =
		new NetworkVariable<ulong>(
			0,
			NetworkVariableReadPermission.Everyone,
			NetworkVariableWritePermission.Owner);

	public override void OnNetworkSpawn()
	{
		Invoke(nameof(GetSteamInformation), .1f);
	}
	private void GetSteamInformation()
	{
		if (IsOwner && SteamClient.IsValid)
		{
			PlayerName.Value = SteamClient.Name;
			SteamId.Value = SteamClient.SteamId.Value;

			if (PvPScoreboard.Instance != null)
				PvPScoreboard.Instance.RegisterPlayer(this);

			return;
		}

		Invoke(nameof(GetSteamInformation), .1f);
	}

	public override void OnNetworkDespawn()
	{
		if (PvPScoreboard.Instance != null)
			PvPScoreboard.Instance.UnregisterPlayer(this);
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	public void AddKillServerRpc()
	{
		Kills.Value++;
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	public void AddDeathServerRpc()
	{
		Deaths.Value++;
	}
}

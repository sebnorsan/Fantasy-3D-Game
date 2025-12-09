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


	public NetworkVariable<bool> extraDamage =
		new NetworkVariable<bool>(
			false,
			NetworkVariableReadPermission.Everyone,
			NetworkVariableWritePermission.Owner);

	public override void OnNetworkSpawn()
	{
		extraDamage.OnValueChanged += OnExtraDamageChanged;

		if (IsOwner && SteamClient.IsValid)
		{
			PlayerName.Value = SteamClient.Name;
			SteamId.Value = SteamClient.SteamId.Value;

			if (PvPScoreboard.Instance != null)
				PvPScoreboard.Instance.RegisterPlayer(this);
		}
	}


	public override void OnNetworkDespawn()
	{
		if (PvPScoreboard.Instance != null)
			PvPScoreboard.Instance.UnregisterPlayer(this);
	}

	public override void OnDestroy()
	{
		extraDamage.OnValueChanged -= OnExtraDamageChanged;
	}

	private void OnExtraDamageChanged(bool previous, bool current)
	{
		if (pRef != null && pRef.playerGraphics != null)
			pRef.playerGraphics.PlayParticle(PfxToPlay.Fire, current);
	}

	public void SetExtraDamage(bool b)
	{
		// allow either server OR owner to set it
		if (!IsServer && !IsOwner) return;

		extraDamage.Value = b;
	}



	public bool HasExtraDamage()
	{
		return extraDamage.Value;
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

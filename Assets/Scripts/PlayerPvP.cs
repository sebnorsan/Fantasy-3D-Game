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
		new NetworkVariable<FixedString64Bytes>();

	public NetworkVariable<ulong> SteamId =
		new NetworkVariable<ulong>();

	public NetworkVariable<bool> extraDamage =
		new NetworkVariable<bool>(
			false,
			NetworkVariableReadPermission.Everyone,
			NetworkVariableWritePermission.Server);

	public override void OnNetworkSpawn()
	{
		extraDamage.OnValueChanged += OnExtraDamageChanged;

		// apply initial state once spawned
		OnExtraDamageChanged(false, extraDamage.Value);

		if (IsOwner && SteamClient.IsValid)
		{
			PlayerName.Value = SteamClient.Name;
			SteamId.Value = SteamClient.SteamId.Value;
		}

		if (PvPScoreboard.Instance != null)
			PvPScoreboard.Instance.RegisterPlayer(this);
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
		// powerup (server) and local bow (owner) both allowed
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

using System.Net.NetworkInformation;
using Unity.Netcode;
using UnityEngine;

public class EnemyElementalAffection : NetworkBehaviour
{
	[SerializeField] private EnemyReferences eRef;

	[SerializeField] private ParticleSystem firePfx;
	[SerializeField] private ParticleSystem icePfx;

	public enum ElementPfxToPlay
	{
		Fire,
		Ice
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();

		firePfx.Stop();
		icePfx.Stop();
	}

	#region Fire



	#endregion

	#region Ice

	#endregion

	#region Lightning

	#endregion

	#region Helpers

	public void PlayPfx()
	{
		PlayPfxServerRpc();
		PlayPfxAction();
	}
	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void PlayPfxServerRpc()
	{
		PlayPfxClientRpc();
	}
	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void PlayPfxClientRpc()
	{
		if (IsOwner) return;

		PlayPfxAction();
	}
	private void PlayPfxAction()
	{

	}

	#endregion
}

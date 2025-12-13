using Unity.Netcode;
using UnityEngine;

public class PvPPowerup : NetworkBehaviour
{
	[SerializeField] private GameObject pfx;
	[SerializeField] private NetworkObject nwo;
	[SerializeField] private PowerUpType upType;

	private PvPPowerupSpawner spawner;

	private enum PowerUpType
	{
		Damage
	}

	public void Init(PvPPowerupSpawner spawner)
	{
		this.spawner = spawner;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!other.TryGetComponent(out PlayerReferences p))
			return;

		// Only let the owning player trigger the pickup
		if (!p.IsOwner) return;

		// LOCAL predicted FX
		if (EventManager.instance.netHelper != null && pfx != null)
		{
			var ps = pfx.GetComponent<ParticleSystem>();
			float life = ps != null
				? ps.totalTime
				: 2f;

			EventManager.instance.netHelper.NetInstantiate(
				pfx.gameObject,
				transform.position,
				pfx.transform.rotation,
				life
			);
		}

		// Tell the SERVER to actually give the powerup + despawn
		PickupServerRpc(p.OwnerClientId);

		SetPowerUp(p);
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void PickupServerRpc(ulong playerClientId)
	{
		// already taken?
		if (nwo != null && !nwo.IsSpawned)
			return;

		if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(playerClientId, out var cc))
			return;

		var p = cc.PlayerObject.GetComponent<PlayerReferences>();
		if (p == null) return;

		// give buff on server
		

		spawner?.NotifyPowerupConsumed(this);

		// despawn networked powerup so all clients lose it
		if (nwo != null)
			nwo.Despawn(true);
		else
			gameObject.SetActive(false);
	}

	private void SetPowerUp(PlayerReferences p)
	{
		switch (upType)
		{
			case PowerUpType.Damage:
				p.bowEffects.SetBigHit(true);
				break;
		}
	}
}

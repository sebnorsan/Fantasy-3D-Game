using Unity.Netcode;
using UnityEngine;

public class PvPPowerup : NetworkBehaviour
{
	[SerializeField] private ParticleSystem pfx;
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

		// client-side predicted FX (only on the player who picked it up)
		if (p.IsOwner && NetHelper.instance != null && pfx != null)
		{
			NetHelper.instance.NetInstantiate(pfx.gameObject, transform.position, Quaternion.identity, pfx.totalTime);
		}

		if (!IsServer) return;

		SetPowerUp(p);

		spawner?.NotifyPowerupConsumed(this);

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
				p.playerPvP.SetExtraDamage(true);
				break;
		}
	}
}

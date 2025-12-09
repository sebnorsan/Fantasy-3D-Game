using Unity.Netcode;
using UnityEngine;

public class Killzone : NetworkBehaviour
{
	private void OnTriggerEnter(Collider other)
	{
		if (!IsServer) return;

		if (other.TryGetComponent(out PlayerReferences p))
		{
			p.playerDamagable.KillPlayer();
		}
	}
}

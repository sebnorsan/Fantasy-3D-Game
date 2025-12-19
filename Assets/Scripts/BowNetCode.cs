using Unity.Netcode;
using UnityEngine;

public class BowNetCode : NetworkBehaviour
{
	[SerializeField] private PlayerReferences pRef;
	[SerializeField] private GameObject arrowPrefab;

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	public void SpawnArrowVisualServerRpc(Vector3 pos, Quaternion rot, Vector3 dir, float dmg, float spd, float size, ulong shooterClientId, ArrowEffect[] arrowEffects)
	{
		SpawnArrowVisualClientRpc(pos, rot, dir, dmg, spd, size, shooterClientId, arrowEffects);
	}
	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	public void SpawnArrowVisualClientRpc(Vector3 pos, Quaternion rot, Vector3 dir, float dmg, float spd, float size, ulong shooterClientId, ArrowEffect[] arrowEffects)
	{
		if (NetworkManager.Singleton.LocalClientId == shooterClientId) return;

		var arrowObj = Instantiate(arrowPrefab, pos, rot);
		var arrow = arrowObj.GetComponent<Arrow>();

		arrow.Initialize(dmg, spd, size, dir, shooterPos: Vector3.zero, shooterClientId, false, arrowEffects);
	}
	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	public void HitServerRpc(ulong targetNetId, float amount, Vector3 hitPoint, ulong shooterClientId, ArrowEffect[] arrowEffects)
	{
		var target = NetworkManager.Singleton.SpawnManager.SpawnedObjects[targetNetId];

		if (target.TryGetComponent<PlayerDamagable>(out var playerDmg))
		{
			playerDmg.SetLastHitBy(shooterClientId);
			playerDmg.TakeDamage(amount, hitPoint, arrowEffects);    // NEW overload
		}

		if (target.TryGetComponent<AbstractEnemy>(out var enemyDmg))
		{
			enemyDmg.SetLastHitBy(shooterClientId);
			enemyDmg.TakeDamage(amount, hitPoint, arrowEffects);
		}
	}

}
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Identifiers;

public class BowNetCode : NetworkBehaviour
{
	[SerializeField] private PlayerReferences pRef;
	[SerializeField] private GameObject arrowPrefab;

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	public void SpawnArrowVisualServerRpc(string identifier, Vector3 pos, Quaternion rot, Vector3 dir, float dmg, float spd, float size, ulong shooterClientId, ArrowEffect[] arrowEffects)
	{
		SpawnArrowVisualClientRpc(identifier, pos, rot, dir, dmg, spd, size, shooterClientId, arrowEffects);
	}
	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	public void SpawnArrowVisualClientRpc(string identifier, Vector3 pos, Quaternion rot, Vector3 dir, float dmg, float spd, float size, ulong shooterClientId, ArrowEffect[] arrowEffects)
	{
		if (NetworkManager.Singleton.LocalClientId == shooterClientId) return;

		var arrowObj = Instantiate(arrowPrefab, pos, rot);
		var arrow = arrowObj.GetComponent<Arrow>();

		arrow.Initialize(identifier, dmg, spd, size, dir, shooterPos: Vector3.zero, shooterClientId, false, arrowEffects);
	}
	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	public void HitServerRpc(ulong targetNetId, float amount, Vector3 hitPoint, ulong shooterClientId, ArrowEffect[] arrowEffects, float knockbackMultiplier)
	{
		var target = NetworkManager.Singleton.SpawnManager.SpawnedObjects[targetNetId];

		if (target.TryGetComponent<AbstractDamagable>(out var dmg))
		{
			dmg.SetLastHitBy(shooterClientId);
			dmg.TakeDamage(amount, hitPoint, arrowEffects, knockbackMultiplier);
		}
	}
}
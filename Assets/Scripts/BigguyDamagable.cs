using Unity.Netcode;
using UnityEngine;

public class BigguyDamagable : NetworkBehaviour
{
	[SerializeField] private GameObject damagePfx;

	public void TakeDamage(Transform hitTransform, int dmg)
	{
		if (!NetworkManager.Singleton.IsServer) return; // gameplay happens only on server

		// apply damage
		var killable = GetComponentInParent<KillableObject>();
		if (killable == null) return;

		killable.TakeDamage(dmg, hitTransform.position);

		// tell everyone to play hit VFX + anim
		PlayHitClientRpc(hitTransform.position);

		if (killable.CurrentHealth <= 0)
		{
			WinServer();
		}
		else
		{
			DamagedClientRpc();
		}
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void PlayHitClientRpc(Vector3 pos)
	{
		if (damagePfx != null)
			Instantiate(damagePfx, pos, Quaternion.identity);
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void DamagedClientRpc()
	{
		var anim = GetComponentInParent<Animator>();
		if (anim != null) anim.SetTrigger("Damaged");
	}

	private void WinServer()
	{
		// server decides the win
		WinClientRpc();

		// stop spawner + clear enemies (server-side)
		if (EnemySpawnerManager.instance != null)
		{
			EnemySpawnerManager.instance.StopAllCoroutines();
			Destroy(EnemySpawnerManager.instance.gameObject);
		}

		var allEnemies = FindObjectsByType<AbstractEnemy>(FindObjectsSortMode.None);
		foreach (var e in allEnemies)
			Destroy(e.gameObject);
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void WinClientRpc()
	{
		var anim = GetComponentInParent<Animator>();
		if (anim != null) anim.SetTrigger("Win");
	}
}

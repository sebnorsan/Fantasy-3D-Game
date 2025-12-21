using Unity.Netcode;
using UnityEngine;

public abstract class AbstractEnemyDamagable : AbstractDamagable
{
	[Space(5)]

	[SerializeField] private EnemyReferences eRef;

	protected override void OnPlayDamageAnimation()
	{
		eRef.enemyAnimator.A_TakeDamage();
	}

	protected override float DamageSet(float amount)
	{
		return Mathf.Max(0, syncedHealth.Value - amount);
	}
	protected override float HealSet(float amount)
	{
		return Mathf.Clamp(currentHealth + amount, 0, maxHealth);
	}
	protected override void HandleKnockback(Vector3 hitPoint)
	{
		eRef.enemyNavigation.DoKnockback(hitPoint);
	}
	protected override void LocalPredictedDie()
	{
		base.LocalPredictedDie();

		foreach (var rend in GetComponentsInChildren<MeshRenderer>(includeInactive: true))
			if (rend) rend.enabled = false;

		foreach (var col in GetComponentsInChildren<Collider>())
			col.enabled = false;

		eRef.enemyAnimator.A_SetWalk(false);
	}
	protected override void DieServer()
	{
		DieClientRpc(lastHitByClientId);
		StopAllCoroutines();
		DespawnObject();
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void DieClientRpc(ulong shooterClientId)
	{
		EnemyLODManager.instance?.enemies.Remove(eRef.enemy.lod_anim);
		GameManager.instance?.AddXp(eRef.enemy.xpDrop);
	}
	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	protected override void DieParticlesClientRpc()
	{
		bool isShooter = NetworkManager.Singleton != null &&
						 NetworkManager.Singleton.LocalClientId == lastHitByClientId;

		if (!isShooter)
		{
			PlayDeathPfx();
		}
	}
	protected override void PlayDeathPfx()
	{
		if (deathParticles != null)
		{
			var pfx = Instantiate(deathParticles, transform.position, Quaternion.identity);
			var text = pfx.GetComponentInChildren<TMPro.TextMeshProUGUI>();
			if (text != null)
				text.text = $"+{eRef.enemy.xpDrop}xp";
			Destroy(pfx, 5);
		}
	}
}
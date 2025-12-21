using Unity.Netcode;
using UnityEngine;
using static Steamworks.InventoryItem;

public abstract class AbstractDamagable : NetworkBehaviour, IDamagable
{
	[Header("Common Damage Feedback")]
	[SerializeField] protected DamageFeedback dmgFeedback;

	protected ulong lastHitByClientId = ulong.MaxValue;
	protected ArrowEffect[] currentAppliedEffects;

	[Space(15)]
	[SerializeField] protected ParticleSystem deathParticles;

	[Space(15)]
	public float maxHealth = 100;
	protected float currentHealth = 100;

	protected NetworkVariable<float> syncedHealth = new(
	writePerm: NetworkVariableWritePermission.Server);

	protected float baseMaxHealth = 100;
	protected float baseCurrentHealth = 100;

	protected bool locallyPredictedDead;

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();

		syncedHealth.OnValueChanged += OnSyncedHealthChanged;

		if (IsServer)
		{
			syncedHealth.Value = maxHealth; // or maxHealth if you prefer
			currentHealth = maxHealth;
		}

		NetworkSpawn();
	}
	protected virtual void NetworkSpawn() { }
	public override void OnDestroy()
	{
		if (syncedHealth != null)
			syncedHealth.OnValueChanged -= OnSyncedHealthChanged;
	}
	private void OnSyncedHealthChanged(float prev, float curr)
	{
		currentHealth = curr;
		OnHealthChangedClient(prev, curr);
	}

	protected virtual void OnHealthChangedClient(float prev, float curr) { }
	public void SetLastHitBy(ulong shooterClientId)
	{
		lastHitByClientId = shooterClientId;
	}

	protected void ApplyCurrentEffects(ArrowEffect[] arrowEffects)
	{
		currentAppliedEffects = arrowEffects;
	}

	// shooter client can call this locally to avoid waiting for rpc
	public virtual void PlayPredictedHitFeedback(Vector3 hitPoint, ArrowEffect[] arrowEffects)
	{
		ApplyCurrentEffects(arrowEffects);
		DamageEffects(hitPoint);
	}
	public virtual void LocalPredictedDamage(float amount, Vector3 hitPoint, ulong shooterClientId, ArrowEffect[] arrowEffects)
	{
		if (!IsClient) return;
		if (NetworkManager.Singleton.LocalClientId != shooterClientId) return;
		if (locallyPredictedDead) return;

		float newHealth = Mathf.Max(0, currentHealth - amount);
		currentHealth = newHealth;

		// instant local VFX
		DamageEffects(hitPoint);

		if (newHealth == 0)
		{
			locallyPredictedDead = true;
			LocalPredictedDie();
		}
	}
	protected virtual void LocalPredictedDie()
	{
		PlayDeathPfx();
	}
	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	protected virtual void DieParticlesClientRpc()
	{
		PlayDeathPfx();
	}
	protected void BroadcastDamageEffects(Vector3 hitPoint, ArrowEffect[] arrowEffects)
	{
		DamageEffectsClientRpc(hitPoint, lastHitByClientId, arrowEffects);
	}
	protected abstract void PlayDeathPfx();

	// default: skip shooter to prevent double VFX when doing prediction
	protected virtual bool SkipClientEffectsForShooter => true;

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void DamageEffectsClientRpc(Vector3 hitPoint, ulong shooterClientId, ArrowEffect[] arrowEffects)
	{
		ApplyCurrentEffects(arrowEffects);

		bool isShooter =
			NetworkManager.Singleton != null &&
			NetworkManager.Singleton.LocalClientId == shooterClientId;

		if (!(isShooter && SkipClientEffectsForShooter))
			DamageEffects(hitPoint);

		OnDamageEffectsClient(hitPoint, shooterClientId, isShooter);
	}

	protected virtual void DamageEffects(Vector3 hitPoint)
	{
		dmgFeedback?.PlayFlash();
		dmgFeedback?.PlayDamageParticle(currentAppliedEffects);
		OnPlayDamageAnimation();
	}

	protected virtual void OnPlayDamageAnimation() { }

	// Call this from TakeDamage (server-side)
	protected virtual void HandleKnockback(Vector3 hitPoint) { }

	// Extra client-side logic when damage rpc arrives (shake, extra anim, etc.)
	protected virtual void OnDamageEffectsClient(Vector3 hitPoint, ulong shooterClientId, bool isShooter) { }

	// IDamagable must still be implemented by the real types
	public virtual void TakeDamage(float amount, Vector3 hitPoint, ArrowEffect[] arrowEffects)
	{
		if (!NetworkManager.Singleton.IsServer) return;

		currentHealth = DamageSet(amount);
		syncedHealth.Value = currentHealth;

		ApplyCurrentEffects(arrowEffects);
		HandleKnockback(hitPoint);
		DamageEffectsClientRpc(hitPoint, lastHitByClientId, arrowEffects);
		CheckForDeath();
	}
	public virtual void Heal(float amount)
	{
		if (!NetworkManager.Singleton.IsServer) return;

		currentHealth = HealSet(amount);
		syncedHealth.Value = currentHealth;
	}
	protected abstract float DamageSet(float amount);
	protected abstract float HealSet(float amount);
	
	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	protected void CheckForDeathServerRpc()
	{
		CheckForDeath();
	}
	private void CheckForDeath()
	{
		if (currentHealth <= 0)
			DieServer();
	}
	protected virtual void DieServer()
	{
		DieParticlesClientRpc();
	}
	protected void AddKillForPlayer()
	{
		if (lastHitByClientId != ulong.MaxValue && NetworkManager.Singleton.ConnectedClients.TryGetValue(lastHitByClientId, out var killerCc))
		{
			var killerRefs = killerCc.PlayerObject.GetComponent<PlayerReferences>();
			if (killerRefs != null && killerRefs.playerPvP != null)
			{
				killerRefs.playerPvP.AddKillServerRpc();

				lastHitByClientId = ulong.MaxValue;
			}
		}
	}
	protected virtual void DespawnObject()
	{
		if (TryGetComponent(out NetworkObject nwo) && nwo.IsSpawned)
			nwo.Despawn(true);
		else
			Destroy(gameObject);
	}
	
}

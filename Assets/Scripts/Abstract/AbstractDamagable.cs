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
	[SerializeField] protected float currentHealth = 100;

	[SerializeField] protected NetworkVariable<float> syncedHealth = new(
	writePerm: NetworkVariableWritePermission.Server);

	protected float baseHealth = -1;

	protected bool locallyPredictedDead;

	private void Start()
	{
		baseHealth = maxHealth;
	}
	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();

		syncedHealth.OnValueChanged += OnSyncedHealthChanged;

		if (IsServer)
		{
			if (baseHealth <= 0f) // only init once
				baseHealth = maxHealth;  // store ORIGINAL max

			maxHealth = baseHealth;
			currentHealth = maxHealth;
			syncedHealth.Value = currentHealth;
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
	
	public virtual void LocalPredictedDamage(float amount, Vector3 hitPoint, ulong shooterClientId, ArrowEffect[] arrowEffects)
	{
		if (!IsClient) return;
		if (NetworkManager.Singleton.LocalClientId != shooterClientId) return;
		if (locallyPredictedDead) return;

		// instant local VFX
		ApplyCurrentEffects(arrowEffects);
		DamageEffects(hitPoint, true);

		float newHealth = Mathf.Max(0, currentHealth - amount);

		if (newHealth == 0)
			LocalPredictedDie();

		if (IsServer) return;
		
		currentHealth = newHealth;
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

	protected virtual void DamageEffects(Vector3 hitPoint, bool owner = false)
	{
		dmgFeedback?.PlayFlash();
		dmgFeedback?.PlayDamageParticle(transform.position, currentAppliedEffects, owner);
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

using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public abstract class AbstractEnemyDamagable : AbstractDamagable
{
	[Space(5)]
	[SerializeField] private EnemyReferences eRef;

	[Space(15)]
	[Header("Health UI (world / multiplayer)")]
	[SerializeField] private Slider healthSlider;
	[SerializeField] private float sliderLerpSpeed = 5f;
	[SerializeField] private CanvasGroup healthBarCanvasGroup;

	[SerializeField] private float showHealthBarSeconds = 2.5f; // NEW

	private float displayedHealth;
	private Coroutine showRoutine; // NEW

	private void Start()
	{
		displayedHealth = syncedHealth.Value;
		SetHealthBar();

		if (healthBarCanvasGroup != null)
		{
			healthBarCanvasGroup.alpha = 0f;          // NEW default hidden
			healthBarCanvasGroup.interactable = false;
			healthBarCanvasGroup.blocksRaycasts = false;
		}
	}

	private void Update()
	{
		if (healthSlider == null) return;

		// NEW: don't run if hidden
		if (healthBarCanvasGroup != null && healthBarCanvasGroup.alpha <= 0.001f)
			return;

		displayedHealth = Mathf.Lerp(
			displayedHealth,
			syncedHealth.Value,
			Time.deltaTime * sliderLerpSpeed
		);

		healthSlider.value = displayedHealth;
	}

	private void SetHealthBar()
	{
		if (healthSlider == null) return;

		healthSlider.maxValue = heldMaxHealth;
		healthSlider.value = syncedHealth.Value;
		displayedHealth = syncedHealth.Value;
	}

	// NEW: show locally for a few seconds
	private void ShowHealthBarLocal(float seconds)
	{
		if (healthBarCanvasGroup == null) return;

		if (showRoutine != null)
			StopCoroutine(showRoutine);

		showRoutine = StartCoroutine(ShowRoutine(seconds));
	}

	private IEnumerator ShowRoutine(float seconds)
	{
		// snap correct values when shown
		SetHealthBar();

		float inTime = Mathf.Max(0.001f, .1f);
		float outTime = Mathf.Max(0.001f, seconds);

		// Fade in fast
		for (float t = 0f; t < inTime; t += Time.deltaTime)
		{
			float a = t / inTime;
			healthBarCanvasGroup.alpha = a;
			yield return null;
		}
		healthBarCanvasGroup.alpha = 1f;

		// Fade out over the duration
		for (float t = 0f; t < outTime; t += Time.deltaTime)
		{
			float a = 1f - (t / outTime);
			healthBarCanvasGroup.alpha = a;

			// while visible, keep slider smooth
			displayedHealth = Mathf.Lerp(displayedHealth, syncedHealth.Value, Time.deltaTime * sliderLerpSpeed);
			if (healthSlider != null)
				healthSlider.value = displayedHealth;

			yield return null;
		}

		healthBarCanvasGroup.alpha = 0f;
		showRoutine = null;
	}


	// NEW: called from AbstractDamagable like in your PlayerDamagable
	protected override void OnDamageEffectsClient(Vector3 hitPoint, ulong shooterClientId, bool isShooter)
	{
		if (isShooter)
			ShowHealthBarLocal(showHealthBarSeconds);
	}

	public void SetHealthMultiplier()
	{
		if (!IsServer)
		{
			RequestSetHealthMultiplierServerRpc();
			return;
		}

		ApplyHealthMultiplierServer();
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void RequestSetHealthMultiplierServerRpc()
	{
		ApplyHealthMultiplierServer();
		CheckForDeathServerRpc();
	}

	private void ApplyHealthMultiplierServer()
	{
		float oldMax = heldMaxHealth;

		heldMaxHealth = GetMaxHealth();
		float deltaMax = heldMaxHealth - oldMax;

		if (deltaMax > 0f)
			currentHealth += deltaMax;

		currentHealth = Mathf.Clamp(currentHealth, 0f, heldMaxHealth);
		syncedHealth.Value = currentHealth;

		SyncMaxHealthClientRpc(heldMaxHealth);
	}

	private float GetMaxHealth() => eRef.enemyMultipliers.GetHealthMulti(baseHealth);

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void SyncMaxHealthClientRpc(float newMax)
	{
		heldMaxHealth = newMax;
		SetHealthBar();
	}

	protected override void OnPlayDamageAnimation()
	{
		eRef.enemyAnimator.A_TakeDamage();
	}

	protected override float DamageSet(float amount) => Mathf.Max(0, syncedHealth.Value - amount);
	protected override float HealSet(float amount) => Mathf.Clamp(currentHealth + amount, 0, heldMaxHealth);

	protected override void HandleKnockback(Vector3 hitPoint, float knockbackMultiplier)
	{
		eRef.enemyNavigation.DoKnockback(hitPoint, knockbackMultiplier);
	}

	protected override void LocalPredictedDie()
	{
		base.LocalPredictedDie();

		foreach (var rend in GetComponentsInChildren<MeshRenderer>(includeInactive: true))
			if (rend) rend.enabled = false;

		foreach (var col in GetComponentsInChildren<Collider>())
			col.enabled = false;

		eRef.enemyAnimator.A_SetWalk(false);

		// optional: hide UI on death
		if (healthBarCanvasGroup != null)
			healthBarCanvasGroup.alpha = 0f;
	}

	protected override void DieServer()
	{
		base.DieServer();

		DieClientRpc(lastHitByClientId);
		StopAllCoroutines();
		StartCoroutine(DespawnNextFrame());
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void DieClientRpc(ulong shooterClientId)
	{
		EnemyLODManager.instance?.enemies.Remove(eRef.enemy.lod_anim);
		eRef.experienceEmitter.StartEmit();
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	protected override void DieParticlesClientRpc()
	{
		bool isShooter = NetworkManager.Singleton != null &&
						 NetworkManager.Singleton.LocalClientId == lastHitByClientId;

		if (!isShooter)
			PlayDeathPfx();
	}

	protected override void PlayDeathPfx()
	{
		if (deathParticles != null)
		{
			var pfx = Instantiate(deathParticles.gameObject, transform.position, Quaternion.identity);
			Destroy(pfx, 5);
		}
	}
}

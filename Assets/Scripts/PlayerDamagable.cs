using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerDamagable : AbstractDamagable
{
	[SerializeField] private PlayerReferences pRef;

	[Space(15)]
	[SerializeField] private float deathTime;
	[SerializeField] private GameObject deathCam;

	[Space(15)]
	[Header("Health UI (world / multiplayer)")]
	[SerializeField] private Slider healthSlider;
	[SerializeField] private float sliderLerpSpeed = 10f;

	[Header("Health UI (local HUD)")]
	[SerializeField] private Slider localHealthSlider;   // NEW: local-only bar

	[Space(15)]
	[Header("Knockback")]
	[SerializeField] private float knockbackStrength = 10f;

	private float displayedHealth;

	private Vector3 deathPosition = new Vector3(9999, 9999, 9999);

	private Coroutine regenerationCoroutine;

	public void SetHealthMultiplier()
	{
		if (regenerationCoroutine == null && pRef.playerPermanents.GetFlatMultiplier(FlatMultiplier.HealthRegenTime) > 0)
			regenerationCoroutine = StartCoroutine(HealthRegeneration());

		if (!IsServer)
		{
			RequestSetHealthMultiplierServerRpc();
			return;
		}

		ApplyHealthMultiplierServer();
	}
	private IEnumerator HealthRegeneration()
	{
		while (true)
		{
			yield return new WaitForSeconds(pRef.playerPermanents.GetFlatMultiplier(FlatMultiplier.HealthRegenTime));
			Heal(pRef.playerPermanents.GetFlatMultiplier(FlatMultiplier.HealthRegenAmount));
		}
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

	private float GetMaxHealth() => pRef.playerMultipliers.GetMulti(pRef.playerPermanents.GetMaxHealthFlat(), Multiplier.Health);

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void SyncMaxHealthClientRpc(float newMax)
	{
		heldMaxHealth = newMax;
		SetHealthBar();
	}
	protected override void NetworkSpawn()
	{
		if (!IsOwner && localHealthSlider != null)
			localHealthSlider.gameObject.SetActive(false);
	}
	private void Start()
	{
		heldMaxHealth = pRef.playerPermanents.maxHealthBase;

		ScreenSummoner.SummonScreen(Color.black, 1f, false);

		displayedHealth = currentHealth;

		SetHealthBar();
	}

	private void Update()
	{
		// if neither slider exists, nothing to do
		if (healthSlider == null && localHealthSlider == null) return;

		displayedHealth = Mathf.Lerp(
			displayedHealth,
			currentHealth,
			Time.deltaTime * sliderLerpSpeed
		);

		// keep old multiplayer/world bar behaviour
		if (healthSlider != null)
			healthSlider.value = displayedHealth;

		// local HUD bar only for owner
		if (IsOwner && localHealthSlider != null)
			localHealthSlider.value = displayedHealth;
	}
	
	
	private void SetHealthBar()
	{
		// existing multiplayer slider init
		if (healthSlider != null)
		{
			healthSlider.maxValue = heldMaxHealth;
			healthSlider.value = currentHealth;
		}

		// local HUD slider init (owner only)
		if (IsOwner && localHealthSlider != null)
		{
			localHealthSlider.maxValue = heldMaxHealth;
			localHealthSlider.value = currentHealth;
		}
	}
	public void KillPlayer()
	{
		TakeDamage(currentHealth, transform.position, null, 0);
	}
	protected override void HandleKnockback(Vector3 hitPoint, float knockbackMultiplier)
	{
		Vector3 dir = (transform.position - hitPoint);
		dir.y = 0f;
		if (dir.sqrMagnitude > 0.001f)
		{
			dir.Normalize();
			ApplyKnockbackOwnerRpc(dir * (knockbackStrength * knockbackMultiplier));
		}
	}
	protected override float DamageSet(float amount)
	{
		return Mathf.Max(0, currentHealth - amount);
	}
	protected override float HealSet(float amount)
	{
		float healthHolder = Mathf.Clamp(currentHealth + amount, 0, heldMaxHealth);

		if (healthHolder > 0)
		{
			locallyPredictedDead = false;
			SetAliveStateClientRpc();
		}

		return healthHolder;
	}
	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void SetAliveStateClientRpc()
	{
		locallyPredictedDead = false;

		var pc = GetComponent<PlayerController>();
		if (pc != null)
			pc.canMove = true;
	}

	#region DamageEffects

	protected override void OnDamageEffectsClient(Vector3 hitPoint, ulong shooterClientId, bool isShooter)
	{
		if (IsOwner)
		{
			OwnerPlayerShake();
			AudioManagement.instance.PlayThisSound("Damage", "PlayerDamage");
		}
	}

	protected override void OnPlayDamageAnimation()
	{
		if (pRef != null)
			pRef.playerAnimator.A_TakeDamage();
	}

	#endregion
	#region Knockback

	[Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
	private void ApplyKnockbackOwnerRpc(Vector3 force)
	{
		if (pRef.playerController != null)
			pRef.playerController.AddKnockback(force);
	}

	#endregion
	#region Dying
	protected override void DieServer()
	{
		base.DieServer();

		SetDeadStateClientRpc();
		SpawnDeathCamClientRpc(lastHitByClientId);

		StartCoroutine(DeathFlowServer());
		pRef.playerController.canMove = false;

		AddDeathForPlayer();
		AddKillForPlayer();
	}
	private void AddDeathForPlayer()
	{
		pRef.playerPvP?.AddDeathServerRpc();
	}
	protected override void PlayDeathPfx()
	{
		if (deathParticles != null)
		{
			GameObject pfx = Instantiate(deathParticles.gameObject, transform.position, Quaternion.identity);
			Destroy(pfx, 5);
		}
	}
	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void SpawnDeathCamClientRpc(ulong killerClientId)
	{
		if (!IsOwner) return;           // only this player's client

		StartCoroutine(DeathCamFlow(killerClientId));
	}

	private IEnumerator DeathCamFlow(ulong killerClientId)
	{
		pRef.playerCam.GetComponent<AudioListener>().enabled = false;

		var dCam = Instantiate(
			deathCam,
			pRef.playerCam.transform.position,
			pRef.playerCam.transform.rotation);

		if (NetworkManager.Singleton.ConnectedClients.TryGetValue(killerClientId, out var killerCc))
		{
			var killerPc = killerCc.PlayerObject.GetComponent<PlayerController>();
			dCam.GetComponent<DeathCam>().playerWhoKilled = killerPc;
		}

		EventManager.instance.TeleportPlayer(pRef.playerController, deathPosition);

		yield return new WaitForSeconds(deathTime - 2);
		ScreenSummoner.SummonScreen(Color.black, 1f, true);
		yield return new WaitForSeconds(1);
		ScreenSummoner.SummonScreen(Color.black, 1f, false);
		Destroy(dCam);

		pRef.playerCam.GetComponent<AudioListener>().enabled = true;

		EventManager.instance.TeleportPlayer(
			pRef.playerController,
			FindFirstObjectByType<GameSceneSpawnManager>().FindValidSpawnPoint());
	}

	private IEnumerator DeathFlowServer()
	{
		yield return new WaitForSeconds(deathTime);
		Heal(heldMaxHealth); // still server-side
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void SetDeadStateClientRpc()
	{
		pRef.playerController.canMove = false;

		if (IsOwner)
		{
			// e.g. show death screen, disable bow input, etc.
		}
	}
	

	#endregion

	#region ArrowEffects
	private void OwnerPlayerShake()
	{
		if (currentAppliedEffects != null)
		{
			foreach (var effect in currentAppliedEffects)
			{
				switch (effect)
				{
					case ArrowEffect.BigHit:
						pRef.cameraShaker.ShakeOnce(18f, 5f, .1f, 1.5f);
						return;
				}
			}
		}

		// default shake
		pRef.cameraShaker.ShakeOnce(7f, 3f, .1f, .4f);
	}
	#endregion
}

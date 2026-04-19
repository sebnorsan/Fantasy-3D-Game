using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

[RequireComponent(typeof(SphereCollider))]
public class Explosion : NetworkBehaviour
{
	private SphereCollider sphere;
	public int explosionDamage = 10;
	private PlayerReferences pRef;

	private ParticleSystem pfx;
	private ParticleSystem.MainModule pfxMain;
	private ParticleSystem.ShapeModule pfxShape;
	private AudioSource audioSource;

	private float baseRadius;
	private float baseScale;

	[SerializeField] private float minimumLifetime = 2f;

	private void Awake()
	{
		sphere = GetComponent<SphereCollider>();
		baseRadius = sphere.radius;

		if (TryGetComponent(out ParticleSystem particles))
		{
			pfx = particles;
			pfxMain = pfx.main;
			pfxShape = pfx.shape;
			baseScale = pfxMain.startSize.constant;
		}

		if (TryGetComponent(out AudioSource source))
			audioSource = source;
	}

	public void InitializeFromServer(ulong playerId)
	{
		if (!IsServer) return;

		if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(playerId, out var playerCc))
			return;

		pRef = playerCc.PlayerObject.GetComponent<PlayerReferences>();
		if (pRef == null)
			return;

		float explosionRadius = pRef.playerMultipliers.GetMulti(1f, Multiplier.ExplosionRadius);

		InitializeDamage(explosionRadius);
		SetPlayerRefClientRpc(explosionRadius, playerId);

		StartCoroutine(DespawnAfterDelay(GetLifetime()));
	}

	[Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
	private void SetPlayerRefClientRpc(float explosionRadius, ulong playerId)
	{
		InitializeVisuals(explosionRadius, playerId);
	}

	private void InitializeDamage(float explosionRadius)
	{
		if (!IsServer) return;

		if (!sphere.isTrigger)
			Debug.LogWarning("Explosion SphereCollider should be set to Trigger for detection.");

		sphere.radius = baseRadius * explosionRadius;

		float dmgToUse = pRef.playerMultipliers.GetMulti(explosionDamage, Multiplier.ExplosionDamage);
		float knockbackToUse = pRef.playerMultipliers.GetMulti(1f, Multiplier.ExplosionKnockback);

		foreach (var damagable in FindDamagableInSphere())
		{
			damagable.TakeDamage(dmgToUse, transform.position, null, knockbackToUse);
		}

		sphere.enabled = false;
	}

	private void InitializeVisuals(float explosionRadius, ulong playerId)
	{
		if (sphere != null)
			sphere.radius = baseRadius * explosionRadius;

		if (pfx != null)
		{
			pfxMain.startSize = new ParticleSystem.MinMaxCurve(baseScale * explosionRadius);
			MatchPfxToSphere();
		}

		if (sphere != null)
			sphere.enabled = false;

		pfx?.Play();
		audioSource?.Play();
	}

	private void MatchPfxToSphere()
	{
		if (pfx == null || sphere == null) return;

		pfxShape.enabled = true;
		pfxShape.shapeType = ParticleSystemShapeType.Sphere;
		pfxShape.radius = sphere.radius;
		pfxShape.radiusThickness = 1f;
	}

	private float GetLifetime()
	{
		float delay = minimumLifetime;

		if (pfx != null)
			delay = Mathf.Max(delay, pfx.main.duration + pfx.main.startLifetime.constantMax);

		if (audioSource != null && audioSource.clip != null)
			delay = Mathf.Max(delay, audioSource.clip.length);

		return delay;
	}

	private IEnumerator DespawnAfterDelay(float delay)
	{
		yield return new WaitForSeconds(delay);

		if (!IsServer) yield break;

		NetworkObject netObj = NetworkObject;
		if (netObj != null && netObj.IsSpawned)
			netObj.Despawn(true);
		else
			Destroy(gameObject);
	}

	private List<AbstractDamagable> FindDamagableInSphere()
	{
		HashSet<AbstractDamagable> results = new HashSet<AbstractDamagable>();

		Collider[] hits = Physics.OverlapSphere(transform.position, sphere.radius);
		foreach (var hit in hits)
		{
			if (hit.TryGetComponent(out AbstractDamagable enemy))
				results.Add(enemy);
		}

		return new List<AbstractDamagable>(results);
	}
}
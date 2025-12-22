using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class DamageFeedback : MonoBehaviour
{
	[SerializeField] private ParticleSystem hitParticle;
	[SerializeField] private ParticleSystem bigHitParticle;

	[Space(5)]

	[SerializeField] private float flashDuration = 0.1f;
	[SerializeField] private Material flashMaterial;

	private Coroutine flashCo;
	private MeshRenderer[] renderers;

	private void Awake()
	{
		renderers = GetComponentsInChildren<MeshRenderer>(includeInactive: true);

		if (flashMaterial == null)
			flashMaterial = Resources.Load<Material>("Materials/FlashMaterial");
	}
	public void PlayDamageParticle(ArrowEffect[] arrowEffects, bool owner = false)
	{
		PlayDamageParticle(Vector3.zero, arrowEffects, owner);
	}
	public void PlayDamageParticle(Vector3 pos ,ArrowEffect[] arrowEffects, bool owner = false)
	{
		ParticleSystem pfxToPlay = GetHitParticles(arrowEffects);

		var pfx = Instantiate(pfxToPlay, pos, Quaternion.identity);

		var source = pfx.GetComponent<MultiplayerAudioSource>();
		source?.Play(owner);

		Destroy(pfx, 5f);
	}
	public ParticleSystem GetHitParticles(ArrowEffect[] arrowEffects)
	{
		if (arrowEffects != null)
			foreach (var arrowEffect in arrowEffects)
			{
				switch (arrowEffect)
				{
					case ArrowEffect.BigHit:
						return bigHitParticle;
					default:
						break;
				}
			}

		return hitParticle;
	}
	public void PlayFlash()
	{
		if (flashCo == null)
			flashCo = StartCoroutine(FlashRoutine());
	}

	private IEnumerator FlashRoutine()
	{
		var affected = new List<(MeshRenderer r, Material[] original)>(renderers.Length);

		foreach (var rend in renderers)
		{
			if (!rend) continue;

			affected.Add((rend, rend.materials));

			var mats = new Material[rend.materials.Length];
			for (int i = 0; i < mats.Length; i++)
				mats[i] = flashMaterial;

			rend.materials = mats;
		}

		yield return new WaitForSeconds(flashDuration);

		foreach (var (r, original) in affected)
			if (r) r.materials = original;

		flashCo = null;
	}
}

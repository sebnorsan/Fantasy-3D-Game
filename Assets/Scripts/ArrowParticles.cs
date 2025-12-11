using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class ArrowParticles : NetworkBehaviour
{
    [SerializeField] private PlayerReferences pRef;
	[SerializeField] private GameObject ownerPfxParent;
	[SerializeField] private GameObject nonownerPfxParent;

    [Header("Particles")]

    [SerializeField] private ParticleSystem[] arrowFirePfx = new ParticleSystem[2];


	public void Start()
	{
		if (pRef.IsOwner)
			nonownerPfxParent.SetActive(false);
		else if (!pRef.IsOwner)
			ownerPfxParent.SetActive(false);
	}
	public void ApplyArrowEffects()
	{
		foreach (var effect in pRef.bowEffects.activeArrowEffects.Value)
		{
			switch (effect)
			{
				case ArrowEffect.BigHit:
					PlayParticle(ArrowPfxToPlay.Fire, true);
					break;
				default:
					break;
			}
		}
	}
	public void UnApplyArrowEffects()
	{
		foreach (var effect in pRef.bowEffects.activeArrowEffects.Value)
		{
			switch (effect)
			{
				case ArrowEffect.BigHit:
					PlayParticle(ArrowPfxToPlay.Fire, false);
					break;
				default:
					break;
			}
		}
	}
	public void PlayParticle(ArrowPfxToPlay pfx, bool play = true, float delay = 0f)
	{
		StartCoroutine(PlayParticleIE(pfx, play, delay));
	}
	private IEnumerator PlayParticleIE(ArrowPfxToPlay pfx, bool play = true, float delay = 0f)
	{
		yield return new WaitForSeconds(delay);

		PlayParticleFunctionality(pfx, play);
		PlayParticleServerRpc(pfx, play);
	}
	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void PlayParticleServerRpc(ArrowPfxToPlay pfx, bool play)
	{
		PlayParticleClientRpc(pfx, play);
	}
	[Rpc(SendTo.NotOwner, InvokePermission = RpcInvokePermission.Server)]
	private void PlayParticleClientRpc(ArrowPfxToPlay pfx, bool play)
	{
		PlayParticleFunctionality(pfx, play);
	}
	private void PlayParticleFunctionality(ArrowPfxToPlay pfx, bool play)
	{
		ParticleSystem[] pfxToPlay = null;

		switch (pfx)
		{
			case ArrowPfxToPlay.Fire:                 // <-- ADD THIS
				pfxToPlay = arrowFirePfx;
				break;
		}

		if (pfxToPlay == null) return;

		foreach (var system in pfxToPlay)
		{
			var main = system.main;

			if (play)
			{
				if (!system.isPlaying)
				{
					main.playOnAwake = true;
					system.Play();
				}
			}
			else
			{
				if (system.isPlaying)
				{
					main.playOnAwake = false;
					system.Stop();
				}
			}
		}
	}
}
public enum ArrowPfxToPlay
{
	Fire
}
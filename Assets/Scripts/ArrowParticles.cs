using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class ArrowParticles : MonoBehaviour
{
    [SerializeField] private PlayerReferences pRef;
	[SerializeField] private GameObject pfxParent;
	[SerializeField] private bool multiplayerArrow;

    [Header("Particles")]

    [SerializeField] private ParticleSystem arrowFirePfx;

	public void Start()
	{
		if (multiplayerArrow && pRef.IsOwner)
			pfxParent.SetActive(false);
		else if (!multiplayerArrow && !pRef.IsOwner)
			pfxParent.SetActive(false);
	}
	public void ApplyArrowEffects()
	{
		foreach (var effect in pRef.bowEffects.activeArrowEffects.Value)
		{
			switch (effect)
			{
				case ArrowEffect.BigHit:
					EffectHelper(arrowFirePfx, true);
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
					EffectHelper(arrowFirePfx, false);
					break;
				default:
					break;
			}
		}
	}
	private void EffectHelper(ParticleSystem pfx, bool play)
	{
		var main = pfx.main;

		if (play)
		{
			main.playOnAwake = true;
			pfx.Play();
		}
		else
		{
			main.playOnAwake = false;
			pfx.Stop();
			pfx.Clear();
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
		ParticleSystem pfxToPlay = null;

		switch (pfx)
		{
			case ArrowPfxToPlay.Fire:                 // <-- ADD THIS
				pfxToPlay = arrowFirePfx;
				break;
		}

		if (pfxToPlay == null) return;

		var main = pfxToPlay.main;

		if (play)
		{
			if (!pfxToPlay.isPlaying)
			{
				main.playOnAwake = true;
				pfxToPlay.Play();
			}
		}
		else
		{
			if (pfxToPlay.isPlaying)
			{
				main.playOnAwake = false;
				pfxToPlay.Stop();
			}
		}
	}
}
public enum ArrowPfxToPlay
{
	Fire
}
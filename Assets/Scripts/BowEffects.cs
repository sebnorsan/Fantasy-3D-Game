using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using System;

public class BowEffects : NetworkBehaviour
{
	public PlayerReferences pRef;

	[Space(10)]

	public NetworkVariable<List<ArrowEffect>> activeArrowEffects =
		new NetworkVariable<List<ArrowEffect>>(
			new List<ArrowEffect>(),
			NetworkVariableReadPermission.Everyone,
			NetworkVariableWritePermission.Owner);

	public void UpdateEffects()
	{
		pRef.arrowParticles.UnApplyArrowEffects();

		// WARNING:
		// This fully removes all active effects.
		ResetEffects();
	}

	private void ResetEffects()
	{
		EnsureList();

		// Copy the list first, because SetBowEffect(false) removes from it
		List<ArrowEffect> effectsToRemove = new List<ArrowEffect>(activeArrowEffects.Value);

		foreach (ArrowEffect effect in effectsToRemove)
		{
			SetBowEffect(effect, false);
		}
	}

	public void SetBowEffect(ArrowEffect arrowEffect, bool b)
	{
		if (!IsServer && !IsOwner) return;

		switch (arrowEffect)
		{
			case ArrowEffect.BigHit:
				SetBigHitFx(b);
				break;
		}

		SetHelper(arrowEffect, b);
	}

	public bool GetBowEffect(ArrowEffect arrowEffect) => GetHelper(arrowEffect);

	private void SetBigHitFx(bool b)
	{
		if (b && GetBowEffect(ArrowEffect.BigHit)) return;

		pRef.playerGraphics.PlayParticle(PlayerPfxToPlay.Fire, b);
	}

	#region Helpers

	private void EnsureList()
	{
		if (activeArrowEffects.Value == null)
			activeArrowEffects.Value = new List<ArrowEffect>();
	}

	private bool GetHelper(ArrowEffect effect)
	{
		EnsureList();
		return activeArrowEffects.Value.Contains(effect);
	}

	private void SetHelper(ArrowEffect effect, bool b)
	{
		EnsureList();

		// Safer to reassign the list instead of editing the same reference
		List<ArrowEffect> newList = new List<ArrowEffect>(activeArrowEffects.Value);

		if (b)
		{
			if (!newList.Contains(effect))
				newList.Add(effect);
		}
		else
		{
			newList.Remove(effect);
		}

		activeArrowEffects.Value = newList;

		pRef.arrowParticles.ApplyArrowEffects();
	}

	#endregion
}

public enum ArrowEffect
{
	BigHit,
	Critical
}
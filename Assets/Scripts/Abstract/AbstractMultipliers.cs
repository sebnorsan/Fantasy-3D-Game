using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class AbstractMultipliers : MonoBehaviour
{
	public float damageMultiplier = 100;
	public float healthMultiplier = 100;
	public float speedMultiplier = 100;
	public float jumpMultiplier = 100;
	public float attackSpeedMultiplier = 100;
	public float projectileSizeMultiplier = 100;
	public float projectileSpeedMultiplier = 100;
	public float knockbackMultiplier = 100;
	public float experienceMultiplier = 100;

	public float GetDamageMulti(float baseVal) => baseVal * (damageMultiplier / 100);
	public float GetHealthMulti(float baseVal) => baseVal * (healthMultiplier / 100);
	public float GetSpeedMulti(float baseVal) => baseVal * (speedMultiplier / 100);
	public float GetJumpMulti(float baseVal) => baseVal * (jumpMultiplier / 100);
	public float GetAttackSpeedMulti(float baseVal) => baseVal * (attackSpeedMultiplier / 100);
	public float GetProjectileSizeMulti(float baseVal) => baseVal * (projectileSizeMultiplier / 100);
	public float GetProjectileSpeedMulti(float baseVal) => baseVal * (projectileSpeedMultiplier / 100);
	public float GetKnockbackMulti(float baseVal) => baseVal * (knockbackMultiplier / 100);
	public float GetExperienceMulti(float baseVal) => baseVal * (experienceMultiplier / 100);

	protected List<Coroutine> currentTempMults = new List<Coroutine>();

	private void OnValidate()
	{
		if (!NetworkManager.Singleton) return;
		if (!GetComponent<NetworkObject>()) return;
		if (!GetComponent<NetworkObject>().IsSpawned) return;

		if (Application.isPlaying && Application.isEditor)
			ApplyValues();
	}

	public float ApplyInverseMultiplier(float baseVal, float percentMultiplier)
	{
		float m = percentMultiplier / 100f;
		if (m <= 0.0001f) m = 0.0001f;
		return baseVal / m;
	}

	public void SetPermanentMultiplier(Multiplier type, float multiplier)
	{
		SetMultiplier(type, multiplier);
	}
	public void ApplyPermanentMultiplier(Multiplier type, float percent)
	{
		AddMultiplier(type, percent);
	}
	public void ApplyTemporaryMultiplier(Multiplier type, float percent, float resetTime)
	{
		AddMultiplier(type, percent);
		currentTempMults.Add(StartCoroutine(TempMultiplierRemover(type, percent, resetTime)));
	}
	private IEnumerator TempMultiplierRemover(Multiplier type, float amount, float resetTime)
	{
		yield return new WaitForSeconds(resetTime);
		RemoveMultiplier(type, amount);
	}

	private void SetMultiplier(Multiplier type, float multiplier)
	{
		float percent = 100 + (100 * multiplier);

		switch (type)
		{
			case Multiplier.Damage: damageMultiplier = percent; break;
			case Multiplier.Health: healthMultiplier = percent; break;
			case Multiplier.Speed: speedMultiplier = percent; break;
			case Multiplier.Jump: jumpMultiplier = percent; break;
			case Multiplier.AtkSpd: attackSpeedMultiplier = percent; break;
			case Multiplier.PrjSize: projectileSizeMultiplier = percent; break;
			case Multiplier.PrjSpeed: projectileSpeedMultiplier = percent; break;
			case Multiplier.Knockback: knockbackMultiplier = percent; break;
			case Multiplier.Experience: experienceMultiplier = percent; break;

			default: break;
		}

		ApplyValues();
	}

	private void AddMultiplier(Multiplier type, float percent)
	{
		switch (type)
		{
			case Multiplier.Damage: damageMultiplier += percent; break;
			case Multiplier.Health: healthMultiplier += percent; break;
			case Multiplier.Speed: speedMultiplier += percent; break;
			case Multiplier.Jump: jumpMultiplier += percent; break;
			case Multiplier.AtkSpd: attackSpeedMultiplier += percent; break;
			case Multiplier.PrjSize: projectileSizeMultiplier += percent; break;
			case Multiplier.PrjSpeed: projectileSpeedMultiplier += percent; break;
			case Multiplier.Knockback: knockbackMultiplier += percent; break;
			case Multiplier.Experience: experienceMultiplier += percent; break;

			default: break;
		}

		ApplyValues();
	}

	private void RemoveMultiplier(Multiplier type, float percent)
	{
		switch (type)
		{
			case Multiplier.Damage:
				damageMultiplier -= percent; damageMultiplier = Mathf.Max(1f, damageMultiplier); break;
			case Multiplier.Health:
				healthMultiplier -= percent; healthMultiplier = Mathf.Max(1f, healthMultiplier); break;
			case Multiplier.Speed:
				speedMultiplier -= percent; speedMultiplier = Mathf.Max(1f, speedMultiplier); break;
			case Multiplier.Jump:
				jumpMultiplier -= percent; jumpMultiplier = Mathf.Max(1f, jumpMultiplier); break;
			case Multiplier.AtkSpd:
				attackSpeedMultiplier -= percent; attackSpeedMultiplier = Mathf.Max(1f, attackSpeedMultiplier); break;
			case Multiplier.PrjSize:
				projectileSizeMultiplier -= percent; projectileSizeMultiplier = Mathf.Max(1f, projectileSizeMultiplier); break;
			case Multiplier.PrjSpeed:
				projectileSpeedMultiplier -= percent; projectileSpeedMultiplier = Mathf.Max(1f, projectileSpeedMultiplier); break;
			case Multiplier.Knockback:
				knockbackMultiplier -= percent; knockbackMultiplier = Mathf.Max(1f, knockbackMultiplier); break;
			case Multiplier.Experience:
				experienceMultiplier -= percent; experienceMultiplier = Mathf.Max(1f, experienceMultiplier); break;

			default: break;
		}

		ApplyValues();
	}

	protected abstract void ApplyValues();
}

public enum Multiplier
{
	Damage,
	Health,
	Speed,
	Jump,
	AtkSpd,
	PrjSize,
	PrjSpeed,
	Knockback,
	Experience
}

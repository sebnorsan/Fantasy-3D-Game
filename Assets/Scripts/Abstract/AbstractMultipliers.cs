using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AbstractMultipliers : MonoBehaviour
{
	[SerializeField] protected float damageMultiplier = 100;
	[SerializeField] protected float healthMultiplier = 100;
	[SerializeField] protected float speedMultiplier = 100;
	[SerializeField] protected float jumpMultiplier = 100;
	[SerializeField] protected float attackSpeedMultiplier = 100;
	[SerializeField] protected float projectileSizeMultiplier = 100;

	public float GetDamageMulti(float baseVal) => baseVal * (damageMultiplier / 100);
	public float GetHealthMulti(float baseVal) => baseVal * (healthMultiplier / 100);
	public float GetSpeedMulti(float baseVal) => baseVal * (speedMultiplier / 100);
	public float GetJumpMulti(float baseVal) => baseVal * (jumpMultiplier / 100);
	public float GetAttackSpeedMulti(float baseVal) => baseVal * (attackSpeedMultiplier / 100);
	public float GetProjectileSizeMulti(float baseVal) => baseVal * (projectileSizeMultiplier / 100);


	protected List<Coroutine> currentTempMults = new List<Coroutine>();


	private void OnValidate()
	{
		if (Application.isPlaying && Application.isEditor)
			ApplyValues();
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
	private void AddMultiplier(Multiplier type, float percent)
	{
		switch (type)
		{
			case Multiplier.Damage:
				damageMultiplier += percent;
				break;
			case Multiplier.Health:
				healthMultiplier += percent;
				break;
			case Multiplier.Speed:
				speedMultiplier += percent;
				break;
			case Multiplier.Jump:
				jumpMultiplier += percent;
				break;
			case Multiplier.AtkSpd:
				attackSpeedMultiplier += percent;
				break;
			case Multiplier.PrjSize:
				projectileSizeMultiplier += percent;
				break;
			default:
				break;
		}

		ApplyValues();
	}
	private void RemoveMultiplier(Multiplier type, float percent)
	{
		switch (type)
		{
			case Multiplier.Damage:
				damageMultiplier -= percent;
				damageMultiplier = Mathf.Max(1f, damageMultiplier);
				break;
			case Multiplier.Health:
				healthMultiplier -= percent;
				healthMultiplier = Mathf.Max(1f, healthMultiplier);
				break;
			case Multiplier.Speed:
				speedMultiplier -= percent;
				speedMultiplier = Mathf.Max(1f, speedMultiplier);
				break;
			case Multiplier.Jump:
				jumpMultiplier -= percent;
				jumpMultiplier = Mathf.Max(1f, jumpMultiplier);
				break;
			case Multiplier.AtkSpd:
				attackSpeedMultiplier -= percent;
				attackSpeedMultiplier = Mathf.Max(1f, jumpMultiplier);
				break;
			case Multiplier.PrjSize:
				projectileSizeMultiplier -= percent;
				projectileSizeMultiplier = Mathf.Max(1f, jumpMultiplier);
				break;
			default:
				break;
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
	PrjSize
}

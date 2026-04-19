using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Transforms;
using UnityEngine;

[Serializable]
public struct MultiplierEntry
{
	public Multiplier type;
	public float value;
}

[Serializable]
public struct ChanceEntry
{
	public Chance type;
	public float value;
}

public abstract class AbstractMultipliers : MonoBehaviour
{
	[SerializeField] private List<MultiplierEntry> multiplierList = new();
	[SerializeField] private List<ChanceEntry> chanceList = new();

	protected Dictionary<Multiplier, float> multipliers = new();
	protected Dictionary<Chance, float> chances = new();

	protected List<Coroutine> currentTempMults = new();

	private void OnValidate()
	{
		EnsureDefaults();
		RebuildDictionaries();

		if (!Application.isPlaying) return;
		if (!NetworkManager.Singleton) return;

		NetworkObject netObj = GetComponent<NetworkObject>();
		if (!netObj || !netObj.IsSpawned) return;

		ApplyValues();
	}

	protected virtual void Awake()
	{
		//InitializeDefaults();
		EnsureDefaults();
		RebuildDictionaries();
	}

	private void EnsureDefaults()
	{
		foreach (Multiplier type in Enum.GetValues(typeof(Multiplier)))
		{
			if (!multiplierList.Exists(x => x.type == type))
				multiplierList.Add(new MultiplierEntry { type = type, value = 100f });
		}

		foreach (Chance type in Enum.GetValues(typeof(Chance)))
		{
			if (!chanceList.Exists(x => x.type == type))
				chanceList.Add(new ChanceEntry { type = type, value = 0f });
		}
	}

	private void RebuildDictionaries()
	{
		multipliers.Clear();
		chances.Clear();

		foreach (var entry in multiplierList)
			multipliers[entry.type] = Mathf.Max(1f, entry.value);

		foreach (var entry in chanceList)
			chances[entry.type] = Mathf.Max(0f, entry.value);
	}

	private void SetMultiplierListValue(Multiplier type, float value)
	{
		for (int i = 0; i < multiplierList.Count; i++)
		{
			if (multiplierList[i].type == type)
			{
				multiplierList[i] = new MultiplierEntry { type = type, value = value };
				return;
			}
		}

		multiplierList.Add(new MultiplierEntry { type = type, value = value });
	}

	private void SetChanceListValue(Chance type, float value)
	{
		for (int i = 0; i < chanceList.Count; i++)
		{
			if (chanceList[i].type == type)
			{
				chanceList[i] = new ChanceEntry { type = type, value = value };
				return;
			}
		}

		chanceList.Add(new ChanceEntry { type = type, value = value });
	}
	//private void InitializeDefaults()
	//{
	//	multipliers.Clear();
	//	chances.Clear();

	//	foreach (Multiplier multiplier in Enum.GetValues(typeof(Multiplier)))
	//		multipliers[multiplier] = 100f;

	//	foreach (Chance chance in Enum.GetValues(typeof(Chance)))
	//		chances[chance] = 0f;
	//}

	public float GetMulti(float baseVal, Multiplier multiplier)
	{
		return baseVal * (GetMultiplierPercent(multiplier) / 100f);
	}

	public float GetMultiplierPercent(Multiplier multiplier)
	{
		if (!multipliers.TryGetValue(multiplier, out float value))
			return 100f;

		return value;
	}

	public float GetChance(Chance chance)
	{
		if (!chances.TryGetValue(chance, out float value))
			return 0f;

		return value;
	}

	public float ApplyInverseMultiplier(float baseVal, float percentMultiplier)
	{
		float m = percentMultiplier / 100f;
		if (m <= 0.0001f) m = 0.0001f;
		return baseVal / m;
	}

	public void SetPermanentMultiplier(Multiplier type, float multiplier)
	{
		SetMultiplier(type, 100f + (100f * multiplier));
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

	public void SetMultiplier(Multiplier type, float percent)
	{
		float finalValue = Mathf.Max(1f, percent);
		multipliers[type] = finalValue;
		SetMultiplierListValue(type, finalValue);
		ApplyValues();
	}
	public void ApplyMultiplierMultiplied(Multiplier type, float multiplication = 1f, float division = 1f)
	{
		float value = GetMultiplierPercent(type);

		value *= multiplication;
		value /= division;
		value = Mathf.Max(1f, value);

		multipliers[type] = value;
		SetMultiplierListValue(type, value);
		ApplyValues();
	}
	public void AddMultiplier(Multiplier type, float percent)
	{
		multipliers[type] = Mathf.Max(1f, GetMultiplierPercent(type) + percent);
		ApplyValues();
	}

	public void RemoveMultiplier(Multiplier type, float percent)
	{
		multipliers[type] = Mathf.Max(1f, GetMultiplierPercent(type) - percent);
		ApplyValues();
	}

	public void SetChance(Chance type, float value)
	{
		float finalValue = Mathf.Max(0f, value);
		chances[type] = finalValue;
		SetChanceListValue(type, finalValue);
		ApplyValues();
	}

	public void AddChance(Chance type, float value)
	{
		chances[type] = Mathf.Max(0f, GetChance(type) + value);
		ApplyValues();
	}

	public void RemoveChance(Chance type, float value)
	{
		chances[type] = Mathf.Max(0f, GetChance(type) - value);
		ApplyValues();
	}

	public Chance[] RollAllChances()
	{
		List<Chance> hits = new();

		foreach (Chance chance in Enum.GetValues(typeof(Chance)))
		{
			if (RollChance(chance))
				hits.Add(chance);
		}

		return hits.ToArray();
	}

	public bool RollChance(Chance chance)
	{
		float value = GetChance(chance);

		if (value <= 0f) return false;
		if (value >= 100f) return true;

		return UnityEngine.Random.value * 100f < value;
	}

	protected abstract void ApplyValues();
}

public enum Multiplier
{
	Damage,
	Health,
	Speed,
	Jump,
	AtkSpeed,
	ProjectileSize,
	ProjectileSpeed,
	Knockback,
	Experience,
	GlobalExperience,

	Luck,
	CritDamage,

	PickupRadius,
	BurnDamage,
	BurnTime,
	LightningDamage,
	LightningRange,
	ExplosionDamage,
	ExplosionRadius,
	ExplosionKnockback,
	BounceRange,
	BounceDamageFallOff,
	
	DamageToFlying,
	DamageFromBehind,
	StationaryDamage,
	StationaryAttackSpeed,
	AfterKillSpeed,
	HealingOrbAmount,
	ElementalDamage,
	EntitySize,
	
	PushbackKnockback,
	PushbackDistance
}
public enum Chance
{
	HealingOrbChance,

	//Chance to reflect projectiles
	ReflectionChance,

	//Chance for enemies to re-freeze once they thaw
	EnemyFreezeOnThawChance,

	//Chance for doubling projectile per projectile shot (if 3 arrows shot, x percent amount for each arrow to double)
	DoubleProjectileChance,

	BurnChance,
	CritChance,
	LightningKillSummonNewChance,
	LightningChance,
	FreezeChance,
	CritPointChance,
	DodgeChance,
}
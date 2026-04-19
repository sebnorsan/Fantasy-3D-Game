using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System.Linq;

[Serializable]
public class FlatMultiplierEntry
{
	public FlatMultiplier type;
	public float value = 0;
}

public class PlayerPermanents : MonoBehaviour
{
	[SerializeField] private PlayerReferences pRef;

	public float maxHealthBase = 100f;

	// Kept so your old code can still read it if needed
	//public float maxHealthMultiplier = 0f;

	[SerializeField] private List<FlatMultiplierEntry> flatMultiplierList = new();
	private readonly Dictionary<FlatMultiplier, float> flatMultipliers = new();

	[SerializeField] private List<PermanentUpgrades> activePermanents = new();
	[SerializeField] private List<SynergyComponent> activeSynergyComponents = new();
	[SerializeField] private List<Synergies> activeSynergies = new();

	public float GetMaxHealthFlat() => maxHealthBase + GetFlatMultiplier(FlatMultiplier.HealthMax);

	protected List<Coroutine> currentTempMults = new();

	private void Awake()
	{
		EnsureFlatMultiplierDefaults();
		RebuildFlatMultiplierDictionary();
	}

	private void OnValidate()
	{
		EnsureFlatMultiplierDefaults();
		RebuildFlatMultiplierDictionary();

		if (!NetworkManager.Singleton) return;
		if (!GetComponent<NetworkObject>()) return;
		if (!GetComponent<NetworkObject>().IsSpawned) return;

		if (Application.isPlaying && Application.isEditor)
			ApplyValues();
	}

	private void EnsureFlatMultiplierDefaults()
	{
		foreach (FlatMultiplier type in Enum.GetValues(typeof(FlatMultiplier)))
		{
			if (!flatMultiplierList.Exists(x => x.type == type))
			{
				flatMultiplierList.Add(new FlatMultiplierEntry
				{
					type = type,
					value = 0
				});
			}
		}

		flatMultiplierList = flatMultiplierList
			.GroupBy(x => x.type)
			.Select(x => x.First())
			.OrderBy(x => x.type)
			.ToList();
	}

	private void RebuildFlatMultiplierDictionary()
	{
		flatMultipliers.Clear();

		for (int i = 0; i < flatMultiplierList.Count; i++)
			flatMultipliers[flatMultiplierList[i].type] = flatMultiplierList[i].value;
	}

	private void SetFlatMultiplierListValue(FlatMultiplier type, float value)
	{
		for (int i = 0; i < flatMultiplierList.Count; i++)
		{
			if (flatMultiplierList[i].type == type)
			{
				flatMultiplierList[i].value = value;
				return;
			}
		}

		flatMultiplierList.Add(new FlatMultiplierEntry
		{
			type = type,
			value = value
		});
	}

	public float GetFlatMultiplier(FlatMultiplier type)
	{
		if (!flatMultipliers.TryGetValue(type, out float value))
			return 0;

		return value;
	}

	public void SetPermanentUpgrade(PermanentUpgrades permanent)
	{
		if (!activePermanents.Contains(permanent))
		{
			activePermanents.Add(permanent);
			UpdatePermanentChanges(permanent);
		}
	}

	public bool IsPermanentActive(PermanentUpgrades permanent)
	{
		if (!activePermanents.Contains(permanent))
			return false;

		return true;
	}

	private void UpdatePermanentChanges(PermanentUpgrades permanent)
	{
		switch (permanent)
		{
			case PermanentUpgrades.AxeArrows:
			case PermanentUpgrades.ShurikenArrows:
			case PermanentUpgrades.HammerArrows:
			case PermanentUpgrades.BoomerangArrows:
			case PermanentUpgrades.BoomArrows:
			case PermanentUpgrades.NukeArrows:
			case PermanentUpgrades.Inferno:
			case PermanentUpgrades.ExpOnDodge:
			case PermanentUpgrades.ForceField:
			case PermanentUpgrades.ForceFieldExplosion:
			case PermanentUpgrades.NoExplosionDamage:
			case PermanentUpgrades.CritPoint:
			case PermanentUpgrades.SlowZone:
			case PermanentUpgrades.FrostBite:
			case PermanentUpgrades.EnemyKillsGainHp:
			case PermanentUpgrades.Doggy:
			default:
				break;
		}
	}

	#region Synergies

	private static readonly Dictionary<Synergies, SynergyComponent[]> synergyRequirements = new()
	{
		{ Synergies.Ice_Crit, new[] { SynergyComponent.Ice, SynergyComponent.Critical } },
		{ Synergies.Ice_Fire, new[] { SynergyComponent.Ice, SynergyComponent.Fire } },
		{ Synergies.Ice_Fire_Lightning, new[] { SynergyComponent.Ice, SynergyComponent.Fire, SynergyComponent.Lightning } },
		{ Synergies.Sniper_Crit, new[] { SynergyComponent.Sniper, SynergyComponent.Critical } },
		{ Synergies.ManyArrows_Jump, new[] { SynergyComponent.Many, SynergyComponent.Jumping } },
		{ Synergies.Lucky_Lightning, new[] { SynergyComponent.Lucky, SynergyComponent.Lightning } },
		{ Synergies.Lucky_Fire, new[] { SynergyComponent.Lucky, SynergyComponent.Fire } },
		{ Synergies.Lightning_ManyArrows, new[] { SynergyComponent.Lightning, SynergyComponent.Many } },
	};

	public void SetSynergyComponent(SynergyComponent synergy)
	{
		if (!activeSynergyComponents.Contains(synergy))
		{
			activeSynergyComponents.Add(synergy);
			CheckToAddSynergies(synergy);
		}
	}

	private void CheckToAddSynergies(SynergyComponent addedPermanent)
	{
		foreach (var pair in synergyRequirements)
		{
			Synergies synergy = pair.Key;
			SynergyComponent[] needed = pair.Value;

			if (activeSynergies.Contains(synergy))
				continue;

			bool usesAddedPermanent = false;
			foreach (var req in needed)
			{
				if (req == addedPermanent)
				{
					usesAddedPermanent = true;
					break;
				}
			}

			if (!usesAddedPermanent)
				continue;

			bool hasAllRequirements = true;
			foreach (var req in needed)
			{
				if (!activeSynergyComponents.Contains(req))
				{
					hasAllRequirements = false;
					break;
				}
			}

			if (hasAllRequirements)
				SetSynergery(synergy);
		}
	}

	private void SetSynergery(Synergies synergy)
	{
		if (!activeSynergies.Contains(synergy))
		{
			activeSynergies.Add(synergy);
			UpdateSynergyChanges(synergy);
		}
	}

	private void UpdateSynergyChanges(Synergies synergy)
	{
		switch (synergy)
		{
			case Synergies.Ice_Crit:
			case Synergies.Ice_Fire:
			case Synergies.Ice_Fire_Lightning:
			case Synergies.Sniper_Crit:
			case Synergies.ManyArrows_Jump:
			case Synergies.Lucky_Lightning:
			case Synergies.Lucky_Fire:
			case Synergies.Lightning_ManyArrows:
			default:
				break;
		}
	}

	#endregion

	public void SetPermanentFlatMultiplier(FlatMultiplier type, float amount)
	{
		SetFlatMultiplier(type, amount);
	}

	public void ApplyPermanentFlatMultiplier(FlatMultiplier type, float amount)
	{
		AddFlatMultiplier(type, amount);
	}

	public void ApplyTemporaryFlatMultiplier(FlatMultiplier type, float amount, float resetTime)
	{
		AddFlatMultiplier(type, amount);
		currentTempMults.Add(StartCoroutine(TempMultiplierFlatMultiplier(type, amount, resetTime)));
	}

	private IEnumerator TempMultiplierFlatMultiplier(FlatMultiplier type, float amount, float resetTime)
	{
		yield return new WaitForSeconds(resetTime);
		RemoveFlatMultiplier(type, amount);
	}

	private void SetFlatMultiplier(FlatMultiplier type, float amount)
	{
		// Only HealthMax was clamped in your old code
		if (type == FlatMultiplier.HealthMax)
			amount = Mathf.Max(1, amount);

		flatMultipliers[type] = amount;
		SetFlatMultiplierListValue(type, amount);
		ApplyValues();
	}

	private void AddFlatMultiplier(FlatMultiplier type, float amount)
	{
		SetFlatMultiplier(type, GetFlatMultiplier(type) + amount);
	}

	private void RemoveFlatMultiplier(FlatMultiplier type, float amount)
	{
		SetFlatMultiplier(type, GetFlatMultiplier(type) - amount);
	}

	private void ApplyValues()
	{
		pRef.playerDamagable.SetHealthMultiplier();
	}
}

public enum FlatMultiplier
{
	ForceFieldRespawn,
	AfterKillSpdDuration,
	BounceCount,
	BounceRange,
	ExtraJump,
	Pierce,
	HealthMax,
	HealthRegenTime,
	HealthRegenAmount,
	FreezeTime,
	LightningChainCount,

	//Frenzy for x amount of seconds after taking damage
	FrenzyDuration,
	
	LifeStealFlatAmount,
	CritLifeStealAmount,
	BounceKillLifeStealAmount,
	ProjectileCount,
	StationaryProjectileCount,
	RandomProjectileCount,
	BurnDamage,
	BurnTime,
	SlowZoneRange,
	FrostBiteDamage,
	ExpOnDodgeAmount,
	ForceFieldAmount,

	EnemyPushbackDamage
}
public enum PermanentUpgrades
{
	AxeArrows,
	ShurikenArrows,
	HammerArrows,
	BoomerangArrows,
	BoomArrows,
	NukeArrows,

	//If burning enemy dies, it explodes
	Inferno,

	ExpOnDodge,
	ForceField,
	ForceFieldExplosion,
	NoExplosionDamage,

	//A crit point can spawn on an enemy, where hits on it will always be critical
	CritPoint,

	//Frozen enemies slows other enemies in range
	SlowZone,

	//Enemies take damage while frozen
	FrostBite,

	//Every 100 enemies killed, the player gains 1 maxHp
	EnemyKillsGainHp,

	Doggy
}

public enum Synergies
{
	Ice_Crit,
	Ice_Fire,
	Ice_Fire_Lightning,
	Sniper_Crit,
	ManyArrows_Jump,
	Lucky_Lightning,
	Lucky_Fire,
	Lightning_ManyArrows
}
public enum SynergyComponent
{
	MovementSpeed,
	Jumping,
	Damage,
	AttackSpeed,
	Lucky,
	Critical,
	Sniper,
	Many,
	Chain,
	Lightning,
	Fire,
	Ice,
	Boom,
	Health,
	Dodge,
	ForceField,
	Pickup
}
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using Unity.Netcode;
using UnityEditor;
using System.Linq;

public class PlayerPermanents : MonoBehaviour
{
    [SerializeField] private PlayerReferences pRef;

	public float maxHealthBase = 100;
	public float maxHealthMultiplier = 0;

	[SerializeField] private List<PermanentUpgrades> activePermanents = new List<PermanentUpgrades>();
	[SerializeField] private List<SynergyComponent> activeSynergyComponents = new List<SynergyComponent>();
	[SerializeField] private List<Synergies> activeSynergies = new List<Synergies>();

	public float GetMaxHealthFlat() => maxHealthBase + maxHealthMultiplier;

	protected List<Coroutine> currentTempMults = new List<Coroutine>();

	private void OnValidate()
	{
		if (!NetworkManager.Singleton) return;
		if (!GetComponent<NetworkObject>()) return;
		if (!GetComponent<NetworkObject>().IsSpawned) return;

		if (Application.isPlaying && Application.isEditor)
			ApplyValues();
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
				break;
			case PermanentUpgrades.ShurikenArrows:
				break;
			case PermanentUpgrades.HammerArrows:
				break;
			case PermanentUpgrades.BoomerangArrows:
				break;
			case PermanentUpgrades.BoomArrows:
				break;
			case PermanentUpgrades.NukeArrows:
				break;
			case PermanentUpgrades.Inferno:
				break;
			case PermanentUpgrades.ExpOnDodge:
				break;
			case PermanentUpgrades.ForceField:
				break;
			case PermanentUpgrades.ForceFieldExplosion:
				break;
			case PermanentUpgrades.NoExplosionDamage:
				break;
			case PermanentUpgrades.CritPoint:
				break;
			case PermanentUpgrades.SlowZone:
				break;
			case PermanentUpgrades.FrostBite:
				break;
			case PermanentUpgrades.EnemyKillsGainHp:
				break;
			case PermanentUpgrades.Doggy:
				break;
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

			// Skip if this new permanent is not part of this synergy
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
				break;
			case Synergies.Ice_Fire:
				break;
			case Synergies.Ice_Fire_Lightning:
				break;
			case Synergies.Sniper_Crit:
				break;
			case Synergies.ManyArrows_Jump:
				break;
			case Synergies.Lucky_Lightning:
				break;
			case Synergies.Lucky_Fire:
				break;
			case Synergies.Lightning_ManyArrows:
				break;
			default:
				break;
		}
	}
	#endregion

	public void SetPermanentFlatMultiplier(FlatMultiplier type, float amount)
	{
		SetMultiplier(type, amount);
	}
	public void ApplyPermanentFlatMultiplier(FlatMultiplier type, float amount)
	{
		AddMultiplier(type, amount);
	}
	public void ApplyTemporaryFlatMultiplier(FlatMultiplier type, float amount, float resetTime)
	{
		AddMultiplier(type, amount);
		currentTempMults.Add(StartCoroutine(TempMultiplierFlatMultiplier(type, amount, resetTime)));
	}
	private IEnumerator TempMultiplierFlatMultiplier(FlatMultiplier type, float amount, float resetTime)
	{
		yield return new WaitForSeconds(resetTime);
		RemoveMultiplier(type, amount);
	}

	private void SetMultiplier(FlatMultiplier type, float flatAmount)
	{
		//float percent = 100 + (100 * multiplier);

		switch (type)
		{
			case FlatMultiplier.AfterKillSpdDuration:
				break;
			case FlatMultiplier.ForceFieldRespawn:
				break;
			case FlatMultiplier.BounceCount:
				break;
			case FlatMultiplier.ExtraJump:
				break;
			case FlatMultiplier.Pierce:
				break;
			case FlatMultiplier.HealthRegenTime:
				break;
			case FlatMultiplier.HealthRegenAmount:
				break;
			case FlatMultiplier.FreezeDuration:
				break;
			case FlatMultiplier.HealthMax:
				maxHealthMultiplier = flatAmount;
				break;
			case FlatMultiplier.LightningChainCount:
				break;
			case FlatMultiplier.FrenzyDuration:
				break;
			case FlatMultiplier.LifeStealFlatAmount:
				break;
			case FlatMultiplier.CritLifeStealAmount:
				break;
			case FlatMultiplier.BounceKillLifeStealAmount:
				break;
			case FlatMultiplier.ProjectileCount:
				break;
			case FlatMultiplier.StationaryProjectileCount:
				break;
			case FlatMultiplier.RandomProjectileCount:
				break;
			case FlatMultiplier.BurnDamage:
				break;
			case FlatMultiplier.SlowZoneRange:
				break;
			case FlatMultiplier.FrostBiteDamage:
				break;
			case FlatMultiplier.ExpOnDodgeAmount:
				break;
			case FlatMultiplier.ForceFieldAmount:
				break;
			case FlatMultiplier.BounceRange:
				break;
			case FlatMultiplier.BurnTime:
				break;
			default:
				break;
		}

		ApplyValues();
	}

	private void AddMultiplier(FlatMultiplier type, float flatAmount)
	{
		switch (type)
		{
			case FlatMultiplier.ForceFieldRespawn:
				break;
			case FlatMultiplier.AfterKillSpdDuration:
				break;
			case FlatMultiplier.BounceCount:
				break;
			case FlatMultiplier.ExtraJump:
				break;
			case FlatMultiplier.Pierce:
				break;
			case FlatMultiplier.HealthRegenTime:
				break;
			case FlatMultiplier.HealthRegenAmount:
				break;
			case FlatMultiplier.FreezeDuration:
				break;
			case FlatMultiplier.HealthMax:
				maxHealthMultiplier += flatAmount;
				break;
			case FlatMultiplier.LightningChainCount:
				break;
			case FlatMultiplier.FrenzyDuration:
				break;
			case FlatMultiplier.LifeStealFlatAmount:
				break;
			case FlatMultiplier.CritLifeStealAmount:
				break;
			case FlatMultiplier.BounceKillLifeStealAmount:
				break;
			case FlatMultiplier.ProjectileCount:
				break;
			case FlatMultiplier.StationaryProjectileCount:
				break;
			case FlatMultiplier.RandomProjectileCount:
				break;
			case FlatMultiplier.BurnDamage:
				break;
			case FlatMultiplier.SlowZoneRange:
				break;
			case FlatMultiplier.FrostBiteDamage:
				break;
			case FlatMultiplier.ExpOnDodgeAmount:
				break;
			case FlatMultiplier.ForceFieldAmount:
				break;
			case FlatMultiplier.BounceRange:
				break;
			case FlatMultiplier.BurnTime:
				break;
			default:
				break;
		}

		ApplyValues();
	}

	private void RemoveMultiplier(FlatMultiplier type, float flatAmount)
	{
		switch (type)
		{
			case FlatMultiplier.ForceFieldRespawn:
				// forceFieldRespawn -= flatAmount;
				// forceFieldRespawn = Mathf.Max(1f, forceFieldRespawn);
				break;
			case FlatMultiplier.AfterKillSpdDuration:
				break;
			case FlatMultiplier.BounceCount:
				break;
			case FlatMultiplier.ExtraJump:
				break;
			case FlatMultiplier.Pierce:
				break;
			case FlatMultiplier.HealthRegenTime:
				break;
			case FlatMultiplier.HealthRegenAmount:
				break;
			case FlatMultiplier.FreezeDuration:
				break;
			case FlatMultiplier.HealthMax:
				maxHealthMultiplier -= flatAmount;
				maxHealthMultiplier = Mathf.Max(0, maxHealthMultiplier);
				break;
			case FlatMultiplier.LightningChainCount:
				break;
			case FlatMultiplier.FrenzyDuration:
				break;
			case FlatMultiplier.LifeStealFlatAmount:
				break;
			case FlatMultiplier.CritLifeStealAmount:
				break;
			case FlatMultiplier.BounceKillLifeStealAmount:
				break;
			case FlatMultiplier.ProjectileCount:
				break;
			case FlatMultiplier.StationaryProjectileCount:
				break;
			case FlatMultiplier.RandomProjectileCount:
				break;
			case FlatMultiplier.BurnDamage:
				break;
			case FlatMultiplier.SlowZoneRange:
				break;
			case FlatMultiplier.FrostBiteDamage:
				break;
			case FlatMultiplier.ExpOnDodgeAmount:
				break;
			case FlatMultiplier.ForceFieldAmount:
				break;
			case FlatMultiplier.BounceRange:
				break;
			case FlatMultiplier.BurnTime:
				break;
			default: break;
		}

		ApplyValues();
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
	FreezeDuration,
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
	ForceFieldAmount
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
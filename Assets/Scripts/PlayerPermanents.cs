using UnityEngine;

public class PlayerPermanents : MonoBehaviour
{
    [SerializeField] private PlayerReferences pRef;
}

public enum FlatMultipliers
{
	ForceFieldRespawn,
	AfterKillSpdDuration,
	BounceCount,
	ExtraJump,
	Pierce,
	HealthRegenTime,
	HealthRegenAmount,
	FreezeDuration,
}
public enum PermanentUpgrades
{
	AxeArrows,
	ShurikenArrows,
	HammerArrows,
	BoomerangArrows,
	NukeArrows,
	//If burning enemy dies, it explodes
	Inferno,
	ExpOnDodge,
	ForceField,
	ForceFieldExplosion,
	NoExplosionDamage
}

public enum Synergies
{
}
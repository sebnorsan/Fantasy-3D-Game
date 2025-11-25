using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

public class SkillTree : MonoBehaviour
{
	private PlayerController player;
	private CrystalScript crystal;
	private BowScript bow;

	private void Start()
	{
		// Get the local player's NetworkObject
		var nm = NetworkManager.Singleton;
		var localClient = nm?.LocalClient;
		var playerObj = localClient?.PlayerObject;

		if (playerObj != null)
		{
			// PlayerController should be on the player root
			player = playerObj.GetComponent<PlayerController>();

			// Bow is usually a child of the player
			bow = playerObj.GetComponentInChildren<BowScript>(true);
		}

		// Crystal is global (only one), so normal find is fine
		crystal = FindFirstObjectByType<CrystalScript>();
	}

	// Each method now takes the Skill that was passed from the button click
	public void UpgradeArrowSize(Skill skill)
	{
		if (TryBuy(skill))
			bow.arrowSize++;
	}

	public void UpgradeCrystalHp(Skill skill)
	{
		if (TryBuy(skill))
		{
			crystal.maxHealth += 1000;
			crystal.currentHealth += 1000;

			crystal.healthSlider.maxValue = crystal.maxHealth;
			crystal.healthSlider.value = crystal.currentHealth;
		}
	}

	public void UpgradeDrawSpeed(Skill skill)
	{
		if (TryBuy(skill))
			bow.arrowDrawSpeed *= 3;
	}

	public void UpgradeArrowDamage(Skill skill)
	{
		if (TryBuy(skill))
			bow.arrowDamage *= 4;
	}

	public void EnableIceArrow(Skill skill)
	{
		if (TryBuy(skill))
			bow.arrowEffect.Add(ArrowEffect.Ice);
	}

	public void UpgradeXpGain(Skill skill)
	{
		if (TryBuy(skill))
			GameManager.instance.xpMultiplier += 5;
	}

	public void EnableCrystalRegeneration(Skill skill)
	{
		if (TryBuy(skill))
			crystal.Regeneration();
	}

	public void EnableFireArrow(Skill skill)
	{
		if (TryBuy(skill))
			bow.arrowEffect.Add(ArrowEffect.Fire);
	}

	public void EnableCrystalThorns(Skill skill)
	{
		if (TryBuy(skill))
			crystal.SetThorns();
	}

	public void EnableLightningArrow(Skill skill)
	{
		if (TryBuy(skill))
			bow.arrowEffect.Add(ArrowEffect.Lightning);
	}

	public void EnableCrystalDeath(Skill skill)
	{
		if (TryBuy(skill))
			crystal.deadly = true;
	}

	public void EnableBombArrows(Skill skill)
	{
		if (TryBuy(skill))
			bow.arrowEffect.Add(ArrowEffect.Bomb);
	}

	public void UpgradeLightningChain(Skill skill)
	{
		if (TryBuy(skill))
			bow.lightningChain += 3;
	}

	/// <summary>
	/// Attempts to spend a skill point on the given Skill and mark it bought.
	/// </summary>
	private bool TryBuy(Skill skill)
	{
		if (skill == null)
			return false;

		if (GameManager.instance.TrySpendSkillPoint())
		{
			skill.BuySkill();
			return true;
		}
		return false;
	}
}

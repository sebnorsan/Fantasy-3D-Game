using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.VisualScripting;
public class PlayerUpgrades : MonoBehaviour
{
	[SerializeField] private KeyCode openKey;

	[Space(3)]

	[SerializeField] private int cardUpgradeAmounts = 3;
	[SerializeField] private Animator anim;

	[Space(3)]

	[SerializeField] private GameObject upgradeCard;
	[SerializeField] private Transform cardParent;

	private List<UpgradeCard> upgCards = new();

	[Space(6)]

	[SerializeField] private List<UpgradeCardSO> cardLib;

	[Space(6)]

	[SerializeField] private UpgradeCardSO[] baseCardLib;

	private void Start()
	{
		SetBaseCards();
		ResetCards();
	}
	private void Update()
	{
		if (PlayerManager.instance.playerExp.IsSkillPointAvailable())
			anim.SetBool("SkillAvailable", true);
		else
		{
			anim.SetBool("SkillAvailable", false);

			if (anim.GetBool("Enter"))
				Close();

			return;
		}

		if (Input.GetKeyDown(openKey))
			Open();
		else if (Input.GetKeyUp(openKey))
			Close();
	}
	private void Open()
	{
		if (EventManager.instance != null && EventManager.instance.IsCameraPlayerMode())
			EventManager.instance.CameraModeMenu();

		anim.SetBool("Enter", true);
	}
	private void Close()
	{
		if (EventManager.instance != null && !EventManager.instance.IsCameraPlayerMode())
			EventManager.instance.CameraModePlayer();

		anim.SetBool("Enter", false);
	}
	public void ResetCards()
	{
		ResetActiveCardList();

		// Build a valid, unique pool (no nulls / no <=0 weight)
		var pool = new List<UpgradeCardSO>();
		var seen = new HashSet<UpgradeCardSO>();

		for (int i = 0; i < cardLib.Count; i++)
		{
			var c = cardLib[i];
			if (c == null) continue;
			if (c.baseWeight <= 0f) continue;

			if (seen.Add(c))
				pool.Add(c);
		}

		bool allowDuplicates = pool.Count < upgCards.Count;

		foreach (UpgradeCard card in upgCards)
		{
			UpgradeCardSO picked = GetWeightedRandomCard(pool);
			if (picked != null)
			{
				card.SetScriptableObject(picked);

				if (!allowDuplicates)
					pool.Remove(picked); // prevents duplicates in this roll
			}
		}
	}
	private void SetBaseCards()
	{
		AddCardToLibrary(baseCardLib);
	}
	public void AddCardToLibrary(UpgradeCardSO uCardSO)
	{
		cardLib.Add(uCardSO);
	}
	public void AddCardToLibrary(UpgradeCardSO[] uCardSO)
	{
		foreach (var c in uCardSO)
			cardLib.Add(c);
	}
	public void RemoveCardFromLibrary(UpgradeCardSO uCardSO)
	{
		cardLib.Remove(uCardSO);
	}
	private UpgradeCardSO GetWeightedRandomCard(List<UpgradeCardSO> source)
	{
		if (source == null || source.Count == 0) return null;

		float totalWeight = 0f;

		for (int i = 0; i < source.Count; i++)
			totalWeight += GetLuckAdjustedWeight(source[i]);

		// Fallback: if all weights are 0/invalid, just pick any non-null card
		if (totalWeight <= 0f)
		{
			for (int tries = 0; tries < source.Count; tries++)
			{
				int r = UnityEngine.Random.Range(0, source.Count);
				if (source[r] != null)
					return source[r];
			}
			return null;
		}

		float roll = UnityEngine.Random.value * totalWeight;
		float sum = 0f;

		for (int i = 0; i < source.Count; i++)
		{
			float w = GetLuckAdjustedWeight(source[i]);
			if (w <= 0f) continue;

			sum += w;
			if (roll <= sum)
				return source[i];
		}

		return null;
	}
	private float GetLuckAdjustedWeight(UpgradeCardSO c)
	{
		if (c == null) return 0f;
		if (c.baseWeight <= 0f) return 0f;

		// 100 => 1.0 (no change), 200 => 2.0 (luckier)
		float luck = 1;
		if (PlayerManager.m_pRef != null)
			luck = PlayerManager.m_pRef.playerMultipliers.GetMulti(1, Multiplier.Luck);
		luck = Mathf.Max(0.05f, luck);

		// luck=1 => exponent=1 (normal)
		// luck>1 => exponent<1 (rare weights become relatively bigger)
		// luck<1 => exponent>1 (more common-heavy)
		float exponent = 1f / luck;

		return Mathf.Pow(c.baseWeight, exponent);
	}
	public void UseCard(UpgradeCardSO c)
	{
		if (!PlayerManager.instance.playerExp.TrySpendSkillPoint())
			return;

		foreach (var mUpg in c.upgradeMultiplier)
			PlayerManager.m_pRef.playerMultipliers.ApplyPermanentMultiplier(mUpg.multiplier, mUpg.percentageUpgrade);
		foreach (var pUpg in c.upgradePermanents)
			PlayerManager.m_pRef.playerPermanents.SetPermanentUpgrade(pUpg.permanent);
		foreach (var fUpg in c.upgradeFlat)
			PlayerManager.m_pRef.playerPermanents.ApplyPermanentFlatMultiplier(fUpg.multiplier, fUpg.flatMultiplier);
		foreach (var sComp in c.setSynergiesOnUpgrade)
			PlayerManager.m_pRef.playerPermanents.SetSynergyComponent(sComp.synergyComponent);

		if (!c.invincible)
			RemoveCardFromLibrary(c);
		AddCardToLibrary(c.cardsToAddToLib);

		ResetCards();
	}

	private void ResetActiveCardList()
	{
		upgCards.Clear();

		foreach (UpgradeCard c in cardParent.GetComponentsInChildren<UpgradeCard>(true))
			upgCards.Add(c);

		while (upgCards.Count > cardUpgradeAmounts)
		{
			int lastIndex = upgCards.Count - 1;
			var toRemove = upgCards[lastIndex];
			upgCards.RemoveAt(lastIndex);
			Destroy(toRemove.gameObject);
		}

		while (upgCards.Count < cardUpgradeAmounts)
		{
			var go = Instantiate(upgradeCard, cardParent);
			go.transform.SetParent(cardParent, false);

			var card = go.GetComponent<UpgradeCard>();
			if (card != null)
				upgCards.Add(card);
			else
				Debug.LogError("upgradeCard prefab is missing the UpgradeCard component!");
		}
	}

	public void SetCardAmount(int newAmount) => cardUpgradeAmounts = Mathf.Max(1, newAmount);
	public int GetCardAmount() => cardUpgradeAmounts;
}
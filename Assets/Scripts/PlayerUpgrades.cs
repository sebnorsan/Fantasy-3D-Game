using System.Collections.Generic;
using UnityEngine;
using System;
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
		{
			if (source[i] == null) continue;
			if (source[i].baseWeight <= 0f) continue;

			totalWeight += source[i].baseWeight;
		}

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
			if (source[i] == null) continue;
			if (source[i].baseWeight <= 0f) continue;

			sum += source[i].baseWeight;
			if (roll <= sum)
				return source[i];
		}

		return null;
	}
	public void UseCard(UpgradeCardSO c)
	{
		if (!PlayerManager.instance.playerExp.TrySpendSkillPoint())
			return;

		foreach (var mUpg in c.multiUpgrades)
			PlayerManager.m_pRef.playerMultipliers.ApplyPermanentMultiplier(mUpg.multiplier, mUpg.percentageUpgrade);

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
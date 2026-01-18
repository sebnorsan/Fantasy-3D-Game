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

	[SerializeField] private UpgCardField[] cardLib;

	[Serializable]
	public class UpgCardField
	{
		public UpgradeCardSO card;
		/// <summary>
		/// Higher weight = that card gets picked more often
		/// Lower weight = that card gets picked less often
		/// Weight 0 (or negative) = it basically never gets picked
		/// 
		/// e.g.
		/// Common 60
		/// Rare 30
		/// Epic 10
		/// 
		/// </summary>
		public float weight = 1f;
	}
	private void Start()
	{
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

		foreach (UpgradeCard c in upgCards)
		{
			var picked = GetWeightedRandomCard();
			if (picked != null)
				c.SetScriptableObject(picked);
		}
	}
	private UpgradeCardSO GetWeightedRandomCard()
	{
		float totalWeight = 0f;

		for (int i = 0; i < cardLib.Length; i++)
		{
			if (cardLib[i].card == null) continue;
			if (cardLib[i].weight <= 0f) continue;

			totalWeight += cardLib[i].weight;
		}

		// Fallback: if all weights are 0/invalid, just pick any valid card
		if (totalWeight <= 0f)
		{
			for (int tries = 0; tries < cardLib.Length; tries++)
			{
				int r = UnityEngine.Random.Range(0, cardLib.Length);
				if (cardLib[r].card != null)
					return cardLib[r].card;
			}
			return null;
		}

		float roll = UnityEngine.Random.value * totalWeight;
		float sum = 0f;

		for (int i = 0; i < cardLib.Length; i++)
		{
			if (cardLib[i].card == null) continue;
			if (cardLib[i].weight <= 0f) continue;

			sum += cardLib[i].weight;
			if (roll <= sum)
				return cardLib[i].card;
		}

		return null;
	}
	public void UseCard(UpgradeCardSO c)
	{
		if (!PlayerManager.instance.playerExp.TrySpendSkillPoint())
			return;

		foreach (var mUpg in c.multiUpgrades)
			PlayerManager.m_pRef.playerMultipliers.ApplyPermanentMultiplier(mUpg.multiplier, mUpg.percentageUpgrade);

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
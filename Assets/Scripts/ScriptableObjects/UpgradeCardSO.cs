using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "ScriptableObjects/New Upgrade Card")]
public class UpgradeCardSO : ScriptableObject
{
	public Texture SO_logo;
	public string SO_title = "Title";
	public string SO_secondary = "20% / Description";

	public UpgradeMulti[] multiUpgrades;

	[Space(5)]

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

	[Header("Higher = more often; Lower = less often; Common 100 - 60, Uncommon 60 - 35, and so on.")]

	public float baseWeight = 60f;

	[Header("If on, makes so it doesent get removed from card library when picked.")]

	public bool invincible = false;

	[Space(5)]

	public UpgradeCardSO[] cardsToAddToLib;

	[Serializable]
	public class UpgradeMulti
	{
		public Multiplier multiplier;
		public float percentageUpgrade = 20;
	}
}
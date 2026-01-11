using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "ScriptableObjects/New Upgrade Card")]
public class UpgradeCardSO : ScriptableObject
{
	public Texture SO_logo;
	public string SO_title = "Title";
	public string SO_secondary = "20% / Description";

	public UpgradeMulti[] multiUpgrades;

	[Serializable]
	public class UpgradeMulti
	{
		public Multiplier multiplier;
		public float percentageUpgrade = 20;
	}
}
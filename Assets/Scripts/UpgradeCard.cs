using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UpgradeCard : MonoBehaviour
{
    [SerializeField] private UpgradeCardSO upgCardSO;

    [Space(5)]

    [SerializeField] private RawImage logo;
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI secondary;

#if UNITY_EDITOR
    private void OnValidate()
	{
        if (Application.isPlaying) return;

        SetScriptableObject();
	}
#endif
	
	private void SetScriptableObject()
    {
        if (upgCardSO == null) return;

        logo.texture = upgCardSO.SO_logo;
        title.text = upgCardSO.SO_title;
        secondary.text = upgCardSO.SO_secondary;
    }

    public void ChooseCard()
    {
        foreach (var mUpg in upgCardSO.multiUpgrades)
            PlayerManager.m_pRef.playerMultipliers.ApplyPermanentMultiplier(mUpg.multiplier, mUpg.percentageUpgrade);
    }
}

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
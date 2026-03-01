using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Net.NetworkInformation;

public class UpgradeCard : MonoBehaviour
{
    [SerializeField] private UpgradeCardSO upgCardSO;

	[Space(5)]

	[SerializeField] private GameObject cardAddVisual;
	[SerializeField] private Transform cardAddVisualParent;

    [Space(5)]

    [SerializeField] private RawImage logo;
    [SerializeField] private RawImage imageRarity;
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI secondary;

    [Space(5)]

    [SerializeField] private Button cardButton;

    [Space(5)]
    
    [SerializeField] private CardRarity thisCardRarity;

	private PlayerUpgrades pUpg;


	public Color[] rarityColor;
	private static Color[] staticRarityColors;

	private static CardRarity[] _raritiesDesc;
	private static CardRarity[] RaritiesDesc
	{
		get
		{
			if (_raritiesDesc == null)
			{
				_raritiesDesc = (CardRarity[])System.Enum.GetValues(typeof(CardRarity));
				System.Array.Sort(_raritiesDesc, (a, b) => ((int)b).CompareTo((int)a)); // high -> low
			}
			return _raritiesDesc;
		}
	}
	public enum CardRarity
	{
		Common = 60,
		Uncommon = 35,
		Rare = 20,
		SuperRare = 8,
		SuperHolyFuckRare = 1
	}
	private void OnEnable()
	{
		// Make sure this instance points at the shared palette (runtime + editor)
		EnsureRarityColorsLength(false);
	}

#if UNITY_EDITOR
	private void OnValidate()
	{
		if (Application.isPlaying) return;

		// If you edited colors on THIS card, push those changes to the shared palette
		EnsureRarityColorsLength(true);
		SetScriptableObject(upgCardSO);
	}
#endif

	private void EnsureRarityColorsLength(bool pushToStatic)
	{
		int targetLen = System.Enum.GetValues(typeof(CardRarity)).Length;

		if (rarityColor == null) rarityColor = new Color[targetLen];
		if (rarityColor.Length != targetLen)
			System.Array.Resize(ref rarityColor, targetLen);

		// First card to initialize becomes the shared palette
		if (staticRarityColors == null)
		{
			staticRarityColors = rarityColor;
			return;
		}

		// Keep shared palette length in sync with enum
		if (staticRarityColors.Length != targetLen)
			System.Array.Resize(ref staticRarityColors, targetLen);

		// If this card was edited, copy its values into the shared palette
		if (pushToStatic)
		{
			for (int i = 0; i < targetLen; i++)
				staticRarityColors[i] = rarityColor[i];

#if UNITY_EDITOR
			// Update all instances in the scene to point at the shared palette
			foreach (var card in FindObjectsByType<UpgradeCard>(FindObjectsSortMode.None))
				card.rarityColor = staticRarityColors;
#endif
		}

		// Always point this instance at the shared palette reference
		rarityColor = staticRarityColors;
	}
	private void Start()
	{
        pUpg = GetComponentInParent<PlayerUpgrades>();

        Initialize();
	}
    private void Initialize()
    {
		cardButton?.onClick.AddListener(SelectThisCard);
	}
	public void SetScriptableObject(UpgradeCardSO c)
    {
        if (c == null) return;

        upgCardSO = c;

        logo.texture = c.SO_logo;
        title.text = c.SO_title;
        secondary.text = c.SO_secondary;

		imageRarity.color = GetRarityColor(c.baseWeight);
		SetCardAddVisual();
	}
	private void SetCardAddVisual()
	{
		foreach (Transform child in cardAddVisualParent.transform)
			Destroy(child.gameObject);

		foreach (var cAdd in upgCardSO.cardsToAddToLib)
		{
			var obj = Instantiate(cardAddVisual, cardAddVisualParent);
			obj.GetComponent<RawImage>().color = GetRarityColor(cAdd.baseWeight);
		}
	}
	private Color GetRarityColor(float weight)
	{
		var rarities = RaritiesDesc;

		for (int i = 0; i < rarities.Length; i++)
		{
			if (weight >= (int)rarities[i])
			{
				thisCardRarity = rarities[i];
				return GetAppliedColor();
			}
		}

		// If below the lowest threshold, pick the rarest (lowest value)
		thisCardRarity = rarities[rarities.Length - 1];

		return GetAppliedColor();
	}
	private Color GetAppliedColor()
	{
		if (imageRarity == null || rarityColor == null) return Color.white;

		var rarities = RaritiesDesc;
		int index = System.Array.IndexOf(rarities, thisCardRarity);
		if (index < 0 || index >= rarityColor.Length) return Color.white;

		return rarityColor[index];
	}
	
	private void SelectThisCard()
    {
        pUpg.UseCard(upgCardSO);
	}
}
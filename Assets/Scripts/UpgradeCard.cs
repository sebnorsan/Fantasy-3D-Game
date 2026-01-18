using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeCard : MonoBehaviour
{
    [SerializeField] private UpgradeCardSO upgCardSO;

    [Space(5)]

    [SerializeField] private RawImage logo;
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI secondary;

    [Space(5)]

    [SerializeField] private Button cardButton;

    private PlayerUpgrades pUpg;

#if UNITY_EDITOR
    private void OnValidate()
	{
        if (Application.isPlaying) return;

        SetScriptableObject(upgCardSO);
	}
#endif
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
    }

    private void SelectThisCard()
    {
        pUpg.UseCard(upgCardSO);
    }
}
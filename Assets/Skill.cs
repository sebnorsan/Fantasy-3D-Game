using Radishmouse;
using UnityEngine;
using UnityEngine.UI;

public class Skill : MonoBehaviour
{
	[SerializeField] private int price = 100;
	[SerializeField] TMPro.TextMeshProUGUI priceText;
	public GameObject boughtUI, lockedUI;
	[Space(30)]
	[SerializeField] private bool isBought;

	[SerializeField] private Skill[] connectedSkills;
	public void OnValidate()
	{
		var lineRend = GetComponentInChildren<UILineRenderer>();

		if (lineRend == null) return;
		lineRend.points.Clear();
		lineRend.transform.localPosition = -(new Vector3(0, transform.localPosition.y + (transform.localPosition.y * .5f)));
		
		foreach (var skill in connectedSkills)
		{
			if (skill == null)
				continue;

			skill.OnValidate();
			skill.lockedUI.SetActive(true);
			
			lineRend.points.Add(new Vector2(transform.localPosition.x, transform.localPosition.y));
			lineRend.points.Add(new Vector2(skill.transform.localPosition.x, skill.transform.localPosition.y));
			lineRend.points.Add(new Vector2(transform.localPosition.x, transform.localPosition.y));
		}

		boughtUI.SetActive(isBought);
		UpdateText();
	}
	private void Start()
	{
		UpdateText();
	}
	private void UpdateText()
	{
		priceText.text = isBought ? string.Empty : $"{price}$";

		if (lockedUI.activeSelf)
			priceText.text = string.Empty;
	}
	
	public void BuySkill()
	{
		isBought = true;
		boughtUI.SetActive(true);
	}
}

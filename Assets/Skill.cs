using Radishmouse;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.iOS;

public class Skill : MonoBehaviour
{
	[SerializeField] private int price = 100;
	[SerializeField] TMPro.TextMeshProUGUI priceText;
	[SerializeField] private GameObject boughtUI;
	private bool isBought;

	[SerializeField] private Skill[] connectedSkills;
	private void OnValidate()
	{
		var lineRend = GetComponentInChildren<UILineRenderer>();

		if (lineRend == null) return;
		lineRend.points.Clear();
		lineRend.transform.localPosition = -(new Vector3(0, transform.localPosition.y + (transform.localPosition.y * .5f)));
		
		foreach (var skill in connectedSkills)
		{
			if (skill == null)
				continue;	
			lineRend.points.Add(new Vector2(transform.localPosition.x, transform.localPosition.y));
			lineRend.points.Add(new Vector2(skill.transform.localPosition.x, skill.transform.localPosition.y));
			lineRend.points.Add(new Vector2(transform.localPosition.x, transform.localPosition.y));
		}
	}
	private void Start()
	{
		UpdateText();
	}
	private void UpdateText()
	{
		priceText.text = isBought ? string.Empty : $"{price}$";
	}
	public void BuySkill()
	{
		isBought = true;
		boughtUI.SetActive(true);
	}
}

using Radishmouse;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using EasyTextEffects.Editor.MyBoxCopy.Extensions;
using System.Linq;

public class Skill : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public string skillName;
	[TextArea]
	public string skillDescription;
	[SerializeField] private int skillPrice = 1;
	[Space(10)]
	[SerializeField] private Skill[] connectedSkills;
	[Space(50)]
	[SerializeField] TMPro.TextMeshProUGUI priceText;
	[SerializeField] TMPro.TextMeshProUGUI skillTitleText;
	public GameObject boughtUI, lockedUI;
	[Space(30)]
	[SerializeField] private bool isBought;
	public void OnValidate()
	{
		var lineRend = GetComponentInChildren<UILineRenderer>();
		skillTitleText.text = skillName;

		if (lineRend == null) return;
		lineRend.points.Clear();

		lineRend.transform.position = transform.parent.position;

		foreach (var skill in connectedSkills)
		{
			if (skill == null)
				continue;

			skill.lockedUI.SetActive(!isBought);
			skill.OnValidate();
			skill.DecideInteractable();

			lineRend.points.Add(new Vector2(transform.localPosition.x, transform.localPosition.y));
			lineRend.points.Add(new Vector2(skill.transform.localPosition.x, skill.transform.localPosition.y));
			lineRend.points.Add(new Vector2(transform.localPosition.x, transform.localPosition.y));
		}

		connectedSkills = connectedSkills.Where(s => s != null).ToArray();

		boughtUI.SetActive(isBought);
		UpdateText();
	}
	private void Start()
	{
		UpdateText();
	}
	private void UpdateText()
	{
		priceText.text = isBought ? string.Empty : $"{skillPrice}";

		if (lockedUI.activeSelf)
			priceText.text = string.Empty;
	}

	public void BuySkill()
	{
		isBought = true;
		boughtUI.SetActive(true);

		OnValidate();
	}

	public void DecideInteractable()
    {
		if (isBought || lockedUI.activeSelf)
			GetComponent<Button>().interactable = false;
		else
			GetComponent<Button>().interactable = true;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		SkillHoverTooltip.instance.ShowTooltip(skillName, skillDescription);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		SkillHoverTooltip.instance.HideTooltip();
	}
}

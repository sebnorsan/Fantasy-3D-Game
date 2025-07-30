using Radishmouse;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Skill : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public string skillName;
	[TextArea]
	public string skillDescription;
	[SerializeField] private int price = 100;
	[Space(10)]
	[SerializeField] private Skill[] connectedSkills;
	[Space(50)]
	[SerializeField] TMPro.TextMeshProUGUI priceText;
	public GameObject boughtUI, lockedUI;
	[Space(30)]
	[SerializeField] private bool isBought;
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

			skill.lockedUI.SetActive(!isBought);
			skill.OnValidate();
			
			lineRend.points.Add(new Vector2(transform.localPosition.x, transform.localPosition.y));
			lineRend.points.Add(new Vector2(skill.transform.localPosition.x, skill.transform.localPosition.y));
			lineRend.points.Add(new Vector2(transform.localPosition.x, transform.localPosition.y));
		}

		GetComponent<Button>().interactable = !lockedUI.activeSelf;
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

	public void OnPointerEnter(PointerEventData eventData)
	{
		SkillHoverTooltip.instance.ShowTooltip(skillName, skillDescription);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		SkillHoverTooltip.instance.HideTooltip();
	}
}

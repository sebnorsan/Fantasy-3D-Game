using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class PlayerExperience : MonoBehaviour
{
	[Header("XP")]
	[SerializeField] private float currentXp = 0;
	[SerializeField] private int xpToLevelUp = 10;

	[SerializeField] private float levelUpMultiplier = 1.5f;

	[Header("Level")]
	[SerializeField] private int currLevel = 0;
	[SerializeField] private int skillPointsAvailable = 0;

	[Header("UI")]
	[SerializeField] private Slider xpSlider;
	[SerializeField] private float xpLerpDuration = 2f;
	[SerializeField] private TextMeshProUGUI levelText;
	[SerializeField] private TextMeshProUGUI levelPointsText;

	private Coroutine xpRoutine;

	private void Awake()
	{
		UpdateXpGraphics();
	}
	public void AddXp(float xp)
	{
		currentXp += PlayerManager.m_pRef.playerMultipliers.GetExperienceMulti(xp);

		TryLevelUp();
		UpdateXpGraphics();
	}
	public bool IsSkillPointAvailable() => skillPointsAvailable >= 1;

	private void TryLevelUp()
	{
		if (currentXp < xpToLevelUp) return;

		currentXp -= xpToLevelUp;
		xpToLevelUp = Mathf.RoundToInt(xpToLevelUp * levelUpMultiplier);

		currLevel++;
		GainSkillPoint(1);
	}

	private void UpdateXpGraphics()
	{
		if (levelText) levelText.text = $"Lv. {currLevel}";

		if (levelPointsText)
			levelPointsText.text = (skillPointsAvailable > 0) ? $"Skill points: {skillPointsAvailable}" : string.Empty;

		if (!xpSlider) return;

		if (xpRoutine != null)
			StopCoroutine(xpRoutine);

		xpRoutine = StartCoroutine(LerpXpBar(xpLerpDuration));
	}

	private IEnumerator LerpXpBar(float duration)
	{
		float from = xpSlider.value;
		float to = (float)currentXp / xpToLevelUp;

		float elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			float t = Mathf.Clamp01(elapsed / duration);
			float smoothT = Mathf.SmoothStep(0f, 1f, t);

			xpSlider.value = Mathf.Lerp(from, to, smoothT);
			yield return null;
		}

		xpSlider.value = to;
		xpRoutine = null;
	}

	public bool TrySpendSkillPoint()
	{
		if (skillPointsAvailable < 1) return false;

		skillPointsAvailable--;
		UpdateXpGraphics();
		return true;
	}

	public void GainSkillPoint(int points)
	{
		skillPointsAvailable += points;
		UpdateXpGraphics();
	}
}

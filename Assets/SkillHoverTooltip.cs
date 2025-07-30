using System.Collections;
using UnityEngine;
using TMPro;

public class SkillHoverTooltip : MonoBehaviour
{
	public static SkillHoverTooltip instance { get; private set; }

	[Header("UI References")]
	[Tooltip("The CanvasGroup on the tooltip panel")]
	[SerializeField] private CanvasGroup tooltipGroup;
	[SerializeField] private TextMeshProUGUI nameText;
	[SerializeField] private TextMeshProUGUI descriptionText;

	[Header("Fade Settings")]
	[SerializeField, Tooltip("How long (sec) the fade in/out takes")]
	private float fadeDuration = 0.3f;

	[Header("Follow Settings")]
	[Tooltip("Offset from mouse in pixels")]
	[SerializeField] private Vector2 tooltipOffset = new Vector2(10f, -10f);

	private Coroutine fadeRoutine;
	private RectTransform tooltipRect;
	private Canvas parentCanvas;

	private void Awake()
	{
		if (instance != null)
			Destroy(this);
		else
			instance = this;

		// grab RectTransform & parent Canvas
		tooltipRect = tooltipGroup.GetComponent<RectTransform>();
		parentCanvas = tooltipGroup.GetComponentInParent<Canvas>();

		// ensure starting hidden
		tooltipGroup.alpha = 0f;
		tooltipGroup.interactable = false;
		tooltipGroup.blocksRaycasts = false;
	}

	private void LateUpdate()
	{
		if (tooltipGroup.alpha > 0f && tooltipRect != null)
		{
			Vector2 anchoredPos;
			RectTransform canvasRect = parentCanvas.transform as RectTransform;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(
				canvasRect,
				Input.mousePosition,
				parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera,
				out anchoredPos
			);
			tooltipRect.localPosition = anchoredPos + tooltipOffset;
		}
	}

	public void ShowTooltip(string skillName, string skillDescription)
	{
		nameText.text = skillName;
		descriptionText.text = skillDescription;
		StartFade(1f);
	}

	public void HideTooltip()
	{
		StartFade(0f);
	}

	private void StartFade(float targetAlpha)
	{
		if (fadeRoutine != null)
			StopCoroutine(fadeRoutine);
		fadeRoutine = StartCoroutine(FadeCoroutine(targetAlpha));
	}

	private IEnumerator FadeCoroutine(float targetAlpha)
	{
		float startAlpha = tooltipGroup.alpha;
		float elapsed = 0f;

		if (targetAlpha == 0f)
		{
			tooltipGroup.interactable = false;
			tooltipGroup.blocksRaycasts = false;
		}

		while (elapsed < fadeDuration)
		{
			elapsed += Time.deltaTime;
			float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeDuration));
			tooltipGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
			yield return null;
		}

		tooltipGroup.alpha = targetAlpha;
		fadeRoutine = null;

		if (targetAlpha == 1f)
		{
			tooltipGroup.interactable = true;
			tooltipGroup.blocksRaycasts = true;
		}
	}
}

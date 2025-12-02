using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class ChangePostprocessingEvent : AbstractEvent
{
	[Space(15)]
	public Volume volume; // Assign your Volume component in the Inspector
	public AnimationCurve curve;
	public float duration = 2f;
	public float startValue = 0f;
	public float endValue = 1f;

	public ProcessingToChange processingToChange;

	public enum ProcessingToChange
	{
		Bloom,
		Vignette
		// Add more effects as needed
	}

	private void OnValidate()
	{
		switch (processingToChange)
		{
			case ProcessingToChange.Bloom:
				volume.profile.TryGet<Bloom>(out var bloom);
				startValue = bloom.intensity.value;
				break;
			case ProcessingToChange.Vignette:
				break;
			default:
				break;
		}
	}

	public override void CallEvent()
	{
		StartCoroutine(ChangeEffectOverTime());
	}

	private IEnumerator ChangeEffectOverTime()
	{
		float elapsed = 0f;

		switch (processingToChange)
		{
			case ProcessingToChange.Bloom:
				if (!volume.profile.TryGet<Bloom>(out var bloom))
				{
					Debug.LogWarning("Bloom effect not found in the Volume profile.");
					yield break;
				}

				while (elapsed < duration)
				{
					elapsed += Time.deltaTime;
					float t = Mathf.Clamp01(elapsed / duration);
					float curveValue = curve.Evaluate(t);
					bloom.intensity.value = Mathf.Lerp(startValue, endValue, curveValue);
					yield return null;
				}

				bloom.intensity.value = Mathf.Lerp(startValue, endValue, curve.Evaluate(1f));
				break;
			case ProcessingToChange.Vignette:
				if (!volume.profile.TryGet<Vignette>(out var vignette))
				{
					Debug.LogWarning("Grain effect not found in the Volume profile.");
					yield break;
				}

				while (elapsed < duration)
				{
					elapsed += Time.deltaTime;
					float t = Mathf.Clamp01(elapsed / duration);
					float curveValue = curve.Evaluate(t);
					vignette.intensity.value = Mathf.Lerp(startValue, endValue, curveValue);
					yield return null;
				}

				vignette.intensity.value = Mathf.Lerp(startValue, endValue, curve.Evaluate(1f));
				break;

			// Add additional cases for other effects as needed

			default:
				Debug.LogWarning("Selected processing effect is not supported.");
				break;
		}
	}
}

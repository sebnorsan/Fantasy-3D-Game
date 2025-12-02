using UnityEngine;
using UnityEngine.UI;
using System.Collections;
public class InteractionProgressBar : MonoBehaviour
{
	public Image fillImage;
	public Image innerFillImage;
	public Animator anim;

	[SerializeField] private float stopEaseDuration = 0.5f;

	public static InteractionProgressBar instance;
	private void Awake()
	{
		if (instance != null)
			Destroy(this);
		else
			instance = this;
	}

	Coroutine animationRunning;
	Coroutine stopAnimationRunning;

	public void StartAnimation(float timeUntil)
	{
		if (timeUntil <= 0f) { SetFill(1f); return; }

		if (stopAnimationRunning != null)
		{
			StopCoroutine(stopAnimationRunning);
			stopAnimationRunning = null;
		}

		if (animationRunning != null)
		{
			StopCoroutine(animationRunning); // restart with new duration
			animationRunning = null;
		}

		animationRunning = StartCoroutine(FillOverTime(timeUntil));

		if (anim) anim.SetBool("Shake", true);
		// EZCameraShake if you want: CameraShaker.Instance?.StartShake(2f, 1f, .1f);
	}

	public void StopAnimation()
	{
		if (animationRunning != null)
		{
			StopCoroutine(animationRunning);
			animationRunning = null;
		}

		if (stopAnimationRunning != null)
			StopCoroutine(stopAnimationRunning);

		stopAnimationRunning = StartCoroutine(EaseFillToZero(stopEaseDuration));

		if (anim) anim.SetBool("Shake", false);
	}

	IEnumerator FillOverTime(float duration)
	{
		SetFill(0f);

		float t = 0f;
		while (t < duration)
		{
			float dt = Time.deltaTime;
			t += dt;
			float currFill = Mathf.Clamp01(t / duration);
			SetFill(currFill);
			yield return null;
		}

		SetFill(1f);
		animationRunning = null;
	}

	IEnumerator EaseFillToZero(float duration)
	{
		float start = GetCurrentFill();
		float t = 0f;

		while (t < duration)
		{
			float dt = Time.deltaTime;
			t += dt;
			float f = Mathf.SmoothStep(start, 0f, Mathf.Clamp01(t / duration));
			SetFill(f);
			yield return null;
		}

		SetFill(0f);
		stopAnimationRunning = null;
	}

	void SetFill(float v)
	{
		if (fillImage) fillImage.fillAmount = v;
		if (innerFillImage) innerFillImage.fillAmount = v;
	}

	float GetCurrentFill()
	{
		if (fillImage) return fillImage.fillAmount;
		if (innerFillImage) return innerFillImage.fillAmount;
		return 0f;
	}
}

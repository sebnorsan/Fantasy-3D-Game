using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
public static class ScreenSummoner
{
	// persistent refs
	static GameObject screenGO;
	static Image screenImg;
	static Coroutine currentFade;

	// settings (optional: tune your prefab path / sorting order)
	const string PrefabPath = "Prefabs/Screen";
	const int SortingOrderTop = 32767; // stay on top of any UI

	public static void SummonScreen(Color screenColor, float lerpTime, bool transToFilled)
	{
		// ensure persistent screen exists
		EnsureScreen();

		// run (or re-run) fade
		if (currentFade != null) CoroutineRunner.instance.StopCoroutine(currentFade);
		currentFade = CoroutineRunner.instance.StartCoroutine(Fade(screenColor, lerpTime, transToFilled));
	}

	static void EnsureScreen()
	{
		if (screenGO != null) return;

		var prefab = Resources.Load<GameObject>(PrefabPath);
		screenGO = Object.Instantiate(prefab);
		Object.DontDestroyOnLoad(screenGO);

		// grab the image we’re fading
		screenImg = screenGO.GetComponentInChildren<Image>(true);

		// make sure it’s truly on top across scenes
		var canvas = screenGO.GetComponentInChildren<Canvas>(true);
		if (canvas)
		{
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = SortingOrderTop;
		}

		// start hidden
		var c0 = screenImg.color;
		screenImg.color = new Color(c0.r, c0.g, c0.b, 0f);
	}

	static IEnumerator Fade(Color color, float lerpTime, bool toFilled)
	{
		// optional small delay before fading OUT so the next scene has a frame to settle
		//if (!toFilled) yield return new WaitForSeconds(0.25f);

		// set base RGB, we’ll only animate alpha
		screenImg.color = new Color(color.r, color.g, color.b, screenImg.color.a);

		float startA = toFilled ? 0f : 1f;
		float endA = toFilled ? 1f : 0f;

		// if instant skip, just snap
		if (lerpTime <= 0f)
		{
			screenImg.color = new Color(color.r, color.g, color.b, endA);
			currentFade = null;
			yield break;
		}

		float t = 0f;
		while (t < 1f)
		{
			t += Time.deltaTime / lerpTime;
			float k = Mathf.SmoothStep(0f, 1f, t);
			float a = Mathf.Lerp(startA, endA, k);
			screenImg.color = new Color(color.r, color.g, color.b, a);
			yield return null;
		}

		// snap exact
		screenImg.color = new Color(color.r, color.g, color.b, endA);

		currentFade = null;
	}
}
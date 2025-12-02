using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
public static class SceneManagerTransitions
{
	public static event System.Action OnBeforeSceneLoad;
	public static event System.Action OnAfterSceneLoad;

	public static Skip whatToSkip = Skip.None;

	static bool busy;
	static Coroutine current;
	static bool hasPending;
	static Color pColor;
	static float pLerp;
	static string pScene;

	public static bool IsBusy => busy;

	public static void LoadSceneWithTransition(Color screenColor, float lerpTime, string sceneName)
	{
		// queue latest if already busy
		if (busy)
		{
			hasPending = true;
			pColor = screenColor; pLerp = lerpTime; pScene = sceneName;
			return;
		}

		busy = true;
		current = CoroutineRunner.instance.StartCoroutine(SceneLoader(screenColor, lerpTime, sceneName));
	}

	public static void LoadSceneWithTransition(Color screenColor, float lerpTime, Scene scene) => LoadSceneWithTransition(screenColor, lerpTime, scene.name);

	static IEnumerator SceneLoader(Color screenColor, float lerpTime, string sceneName)
	{
		// fade IN (to filled)
		if (whatToSkip != Skip.SkipFadeInScreen)
		{
			ScreenSummoner.SummonScreen(screenColor, lerpTime, true);
			yield return new WaitForSeconds(lerpTime);
		}
		else
		{
			ScreenSummoner.SummonScreen(screenColor, 0f, true);
		}

		OnBeforeSceneLoad?.Invoke();
		yield return new WaitForSeconds(0.01f);

		SceneManager.LoadScene(sceneName);
		yield return null; // let scene initialize a frame

		OnAfterSceneLoad?.Invoke();

		// fade OUT (to clear)
		if (whatToSkip != Skip.SkipFadeOutScreen)
			ScreenSummoner.SummonScreen(screenColor, lerpTime, false);
		else
			ScreenSummoner.SummonScreen(screenColor, 0f, false);

		whatToSkip = Skip.None;

		// wait for fade-out duration if not skipped (so back-to-back feels clean)
		if (lerpTime > 0f && whatToSkip != Skip.SkipFadeOutScreen)
			yield return new WaitForSeconds(lerpTime);

		// done — run pending if any (latest wins)
		busy = false;
		current = null;

		if (hasPending)
		{
			hasPending = false;
			LoadSceneWithTransition(pColor, pLerp, pScene);
		}
	}
}

public enum Skip
{
	None,
	SkipFadeInScreen,
	SkipFadeOutScreen
}

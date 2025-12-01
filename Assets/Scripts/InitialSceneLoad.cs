using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InitialSceneLoad : MonoBehaviour
{
	public string sceneToLoad = "Scene";
	public bool directlyToGame;

	private void Start()
	{
		if (directlyToGame)
		{
			StartCoroutine(LoadGameScene());
			return;
		}

		StartCoroutine(LoadInitialScene());
	}
	private IEnumerator LoadInitialScene()
	{
		yield return new WaitUntil(() => NetworkManager.Singleton != null);
		SceneManager.LoadScene(sceneToLoad);
	}
	private IEnumerator LoadGameScene()
	{
		yield return new WaitUntil(() => NetworkManager.Singleton != null);
		NetworkManager.Singleton.StartHost();
		NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
	}
}

using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InitialSceneLoad : MonoBehaviour
{
	public string sceneToLoad = "Scene";

	[Space(10)]

	public bool directlyToGame;
	public string directSceneToLoad = "PvP-Scene";
	
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
		NetworkManager.Singleton.SceneManager.LoadScene(directSceneToLoad, UnityEngine.SceneManagement.LoadSceneMode.Single);
	}
}

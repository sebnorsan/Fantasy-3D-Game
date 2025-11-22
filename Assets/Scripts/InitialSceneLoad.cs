using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InitialSceneLoad : MonoBehaviour
{
	public string sceneToLoad = "Scene";

	private void Start()
	{
		StartCoroutine(LoadInitialScene());
	}
	private IEnumerator LoadInitialScene()
	{
		yield return new WaitUntil(() => NetworkManager.Singleton != null);
		SceneManager.LoadScene(sceneToLoad);
	}
}

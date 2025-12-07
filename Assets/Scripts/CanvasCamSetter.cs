using Unity.Netcode;
using UnityEngine;

public class CanvasCamSetter : MonoBehaviour
{
	private void Start()
	{
		InitializeAfterDelay();
	}
	private void InitializeAfterDelay()
	{
		if (!NetworkManager.Singleton)
			if (!NetworkManager.Singleton.LocalClient.PlayerObject)
			{
				Invoke(nameof(InitializeAfterDelay), .5f);
				return;
			}
			else
				GetComponent<Canvas>().worldCamera = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerReferences>().playerCam;
	}
}

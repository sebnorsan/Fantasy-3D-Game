using Unity.Netcode;
using UnityEngine;

public class EventAudioPlayer : MonoBehaviour
{
	[SerializeField] private AudioToPlay[] audios;
	[SerializeField] private AudioToPlay[] audiosServer;

	private NetworkObject netObj;

	void Awake()
	{
		netObj = GetComponentInParent<NetworkObject>();
	}

	public void PlayAudio(int i)
	{
		bool isOwner = netObj == null || netObj.IsOwner;

		AudioToPlay toPlay = null;

		// Non-owner: try server array first
		if (!isOwner &&
			audiosServer != null &&
			i >= 0 && i < audiosServer.Length)
		{
			toPlay = audiosServer[i];
		}

		// Fallback to normal audios
		if (toPlay == null &&
			audios != null &&
			i >= 0 && i < audios.Length)
		{
			toPlay = audios[i];
		}

		if (toPlay != null)
			AudioManagement.instance.PlayThisSound(toPlay, posToGo: transform.position, parentToGameObject: gameObject);
		else
			Debug.LogWarning($"EventAudioPlayer {name}: no clip at index {i}");
	}

	public void StopAudio(int i)
	{
		bool isOwner = netObj == null || netObj.IsOwner;

		AudioClip clip = null;

		if (!isOwner &&
			audiosServer != null &&
			i >= 0 && i < audiosServer.Length &&
			audiosServer[i] != null)
		{
			clip = audiosServer[i].audioToPlay;
		}

		if (clip == null &&
			audios != null &&
			i >= 0 && i < audios.Length &&
			audios[i] != null)
		{
			clip = audios[i].audioToPlay;
		}

		if (clip != null)
			AudioManagement.instance.StopThisSound(clip);
		else
			Debug.LogWarning($"EventAudioPlayer {name}: no clip to stop at index {i}");
	}
}

using Unity.Netcode;
using UnityEngine;

public class EventAudioPlayer : MonoBehaviour
{
	[SerializeField] private AudioToPlay[] audios;
	[SerializeField] private AudioToPlay[] audiosServer;

	public void PlayAudio(int i)
	{
		bool isOwner = true;

		if (GetComponentInParent<NetworkObject>())
			if (!GetComponentInParent<NetworkObject>().IsOwner)
				isOwner = false;

		if (!isOwner)
			if (audiosServer.Length > 0)
				if (audiosServer[i] != null)
				{
					AudioManagement.instance.PlayThisSound(audiosServer[i]);
					return;
				}

		if (isOwner)
			AudioManagement.instance.PlayThisSound(audios[i]);
	}
	public void StopAudio(int i)
	{
		bool isOwner = true;

		if (GetComponentInParent<NetworkObject>())
			if (!GetComponentInParent<NetworkObject>().IsOwner)
				isOwner = false;

		if (!isOwner)
			if (audiosServer.Length > 0)
				if (audiosServer[i] != null)
				{
					AudioManagement.instance.StopThisSound(audiosServer[i].audioToPlay);
					return;
				}

		if (isOwner)
			AudioManagement.instance.StopThisSound(audios[i].audioToPlay);
	}
}

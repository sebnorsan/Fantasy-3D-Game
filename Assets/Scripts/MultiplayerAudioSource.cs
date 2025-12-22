using System;
using UnityEngine;

public class MultiplayerAudioSource : MonoBehaviour
{
    public AudioToPlay ownerAudio;
    public AudioToPlay nonownerAudio;

	public void Play(bool isOwner)
	{
		if (isOwner)
			AudioManagement.instance.PlayThisSound(ownerAudio, posToGo: transform.position);
		else
			AudioManagement.instance.PlayThisSound(nonownerAudio, posToGo: transform.position);
	}
	public void Stop(bool isOwner)
	{
		if (isOwner)
			AudioManagement.instance.StopThisSound(ownerAudio.audioToPlay);
		else
			AudioManagement.instance.StopThisSound(nonownerAudio.audioToPlay);
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(transform.position, nonownerAudio.minDistance);
		Gizmos.color = Color.blue;
		Gizmos.DrawWireSphere(transform.position, nonownerAudio.maxDistance);
	}
}

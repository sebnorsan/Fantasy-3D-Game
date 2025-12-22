using System;
using UnityEngine;

public class MultiplayerAudioSource : MonoBehaviour
{
    public AudioToPlay ownerAudio;
    public AudioToPlay nonownerAudio;

	public void Play(bool isOwner)
	{
		if (isOwner)
			AudioManagement.instance.PlayThisSound(ownerAudio);
		else
			AudioManagement.instance.PlayThisSound(nonownerAudio);
	}
	public void Stop(bool isOwner)
	{
		if (isOwner)
			AudioManagement.instance.PlayThisSound(ownerAudio);
		else
			AudioManagement.instance.PlayThisSound(nonownerAudio);
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(transform.position, nonownerAudio.minDistance);
		Gizmos.color = Color.blue;
		Gizmos.DrawWireSphere(transform.position, nonownerAudio.maxDistance);
	}
}

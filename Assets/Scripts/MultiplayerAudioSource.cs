using System;
using UnityEngine;

public class MultiplayerAudioSource : MonoBehaviour
{
	[SerializeField] private bool playOnAwake = true;

    public AudioSource ownerAudioSource;
    public AudioSource nonownerAudioSource;

	private void OnValidate()
	{
		if (!Application.isEditor) return;

		ownerAudioSource.playOnAwake = false;
		nonownerAudioSource.playOnAwake = false;
 	}
	private void Start()
	{
		if (playOnAwake)
			Play();
	}
	public void Play()
	{

	}
	public void Stop()
	{

	}
}

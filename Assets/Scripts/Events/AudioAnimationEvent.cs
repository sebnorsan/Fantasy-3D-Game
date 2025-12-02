using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class AudioAnimationEvent : MonoBehaviour
{
	public AudioEvent[] audioEvent;

    [System.Serializable]
	public class AudioEvent
    {
		[Tooltip("Only plays, if not already playing")]
		public bool consistent;

		public AudioChild[] audiosToPlay;
	}
	[System.Serializable]
	public class AudioChild
	{
		public string libName = "Library";
		public string audioName = "Audio";

		public bool randomizePitch = false;
		public float min = .9f;
		public float max = 1.1f;

		public bool setPosCurrentPosition;
		public Vector3 overridePos = Vector3.zero;

		public bool parentAudioSource = false;
	}

	public void AE_PlaySound(int i)
	{
		int audioIndex = 0;

		if (audioEvent[i].audiosToPlay.Length > 1)
			audioIndex = Random.Range(0, audioEvent[i].audiosToPlay.Length);

		var eventToUse = audioEvent[i].audiosToPlay[audioIndex];

		if (audioEvent[i].consistent)
			if (AudioManagement.instance.GetThisSoundInScene(eventToUse.libName, eventToUse.audioName) != null)
				return;

		Vector3 v = Vector3.zero;

		if (eventToUse.setPosCurrentPosition)
			v = transform.position;
		else if (eventToUse.overridePos != Vector3.zero)
			v = eventToUse.overridePos;

		GameObject parentGo = null;

		if (eventToUse.parentAudioSource)
			parentGo = gameObject;

		AudioManagement.instance.PlayThisSound(eventToUse.libName, eventToUse.audioName, v, eventToUse.randomizePitch, eventToUse.min, eventToUse.max, parentGo);
	}
	public void AE_StopSound(int i)
	{
		var eventToUse = audioEvent[i];

		foreach (var e in eventToUse.audiosToPlay)
			AudioManagement.instance.StopThisSound(e.libName, e.audioName);
	}
}

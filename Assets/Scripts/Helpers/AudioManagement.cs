using TMPro;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices.WindowsRuntime;

public class AudioManagement : MonoBehaviour
{
	public static AudioManagement instance;

	[Header("Audio / Subtitles")]
	[SerializeField] private GameObject audioSourceTemplate;
	[SerializeField] private GameObject subtitleTemplate;
	[SerializeField] private AudioLibrary[] audioLibraries;

	[System.Serializable]
	public class AudioLibrary
	{
		public string libName = "New Audio Library";
		[Space(30)] public AudioToPlay[] audios;
	}

	private void Awake()
	{
		if (instance != null)
			Destroy(this);
		else
			instance = this;
	}

	public void RandomizePitchOnSound(string libId, string audioId, float min, float max)
	{
		var src = GetThisSoundInScene(libId, audioId);
		RandomizePitchOnSound(src, min, max);
	}
	public void RandomizePitchOnSound(AudioSource src, float min, float max)
	{
		if (src) src.pitch = Random.Range(min, max);

		if (src.isPlaying)
		{
			src.Stop();
			src.Play();
		}
	}

	public void PlayThisSound(string libId, string audioId, bool randomizePitch = false, float min = .9f, float max = 1.1f, GameObject parentToGameObject = null)
	{
		var data = GetThisSound(libId, audioId);
		PlayThisSound(data, Vector3.zero, parentToGameObject);
	}
	public void PlayThisSound(AudioToPlay data, bool randomizePitch = false, float min = .9f, float max = 1.1f, GameObject parentToGameObject = null)
	{
		PlayThisSound(data, Vector3.zero, parentToGameObject);
	}
	public void PlayThisSound(string libId, string audioId, Vector3 posToGo, bool randomizePitch = false, float min = .9f, float max = 1.1f, GameObject parentToGameObject = null)
	{
		var data = GetThisSound(libId, audioId);
		PlayThisSound(data, posToGo, parentToGameObject);
	}
	public void PlayThisSound(AudioToPlay data, Vector3 posToGo, bool randomizePitch = false, float min = .9f, float max = 1.1f, GameObject parentToGameObject = null)
	{
		if (data == null) return;


		var go = SpawnAndPlay(data);
		var src = go.GetComponent<AudioSource>();

		if (randomizePitch)
			RandomizePitchOnSound(src, min, max);
		if (parentToGameObject != null)
			go.transform.SetParent(parentToGameObject.transform);

		go.transform.position = posToGo;
	}

	public void StopThisSound(string libId, string audioId)
	{
		var src = GetThisSoundInScene(libId, audioId);
		if (src) Destroy(src.gameObject);
	}
	public void StopThisSound(AudioClip clip)
	{
		var audioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
		foreach (var a in audioSources)
			if (a.clip == clip)
				Destroy(a.gameObject);
	}

	public AudioToPlay GetThisSound(string libId, string audioId)
	{
		AudioLibrary curLib = null;
		foreach (var lib in audioLibraries)
			if (lib.libName == libId) { curLib = lib; break; }

		if (curLib != null)
			foreach (var audio in curLib.audios)
				if (audio.audioName == audioId)
					return audio;

		Debug.LogWarning("No audio library / audio found");
		return null;
	}
	public void SetSoundToThisPosition(string libId, string audioId, Vector3 posToGo)
	{
		var src = GetThisSoundInScene(libId, audioId);

		src.transform.position = posToGo;
	}

	public AudioSource GetThisSoundInScene(string libId, string audioId)
	{
		var data = GetThisSound(libId, audioId);
		if (data == null) return null;

		var audioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
		foreach (var a in audioSources)
			if (a.clip == data.audioToPlay)
				return a;

		return null;
	}

	private GameObject SpawnAndPlay(AudioToPlay data)
	{
		var go = Instantiate(audioSourceTemplate, transform.position, Quaternion.identity);
		var src = go.GetComponent<AudioSource>();

		src.loop = data.loop;
		src.clip = data.audioToPlay;
		src.volume = data.audioVolume;
		src.spatialBlend = data.spatialBlend;
		src.maxDistance = data.maxDistance;
		src.minDistance = data.minDistance;
		src.Play();

		if (!data.loop)
			Destroy(go, data.audioToPlay.length + 1f);

		if (data.subtitlesEnabled && data.subtitles != null)
			foreach (var sub in data.subtitles)
				StartCoroutine(CreateSubtitle(sub.timeStarted, sub.timeDestroyed, sub.subtitleText));

		return go;
	}

	private IEnumerator CreateSubtitle(float timeUntilStarted, float timeUntilDestroyed, string text)
	{
		yield return new WaitForSeconds(timeUntilStarted);

		var inst = Instantiate(subtitleTemplate, subtitleTemplate.transform.position, Quaternion.identity);
		var tmp = inst.GetComponentInChildren<TextMeshProUGUI>();
		tmp.text = text;

		Destroy(inst, timeUntilDestroyed - timeUntilStarted);
	}
}

[System.Serializable]
public class AudioToPlay
{
	public string audioName = "New Audio";

	[Space(30)] public bool loop = false;

	[Space(15)] public AudioClip audioToPlay;
	[Range(0, 1)] public float audioVolume = 1f;

	[Space(15)]
	[Range(0, 1)] public int spatialBlend = 0;
	[Range(0, 500)] public float minDistance = 1f;
	[Range(0, 500)] public float maxDistance = 500f;

	[Space(15)]
	public bool randomPitch = false;
	[Range(-3, 3)] public float minPitch = .9f;
	[Range(-3, 3)] public float maxPitch = 1.1f;

	[Space(15)] public bool subtitlesEnabled = false;
	public Subtitle[] subtitles;

	[System.Serializable]
	public class Subtitle
	{
		[TextArea] public string subtitleText;
		public float timeStarted = 0f;
		public float timeDestroyed = 1f;
	}
}
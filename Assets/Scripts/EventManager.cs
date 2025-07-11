using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EventManager : MonoBehaviour
{
	public static EventManager instance;
	public void Awake()
	{
		if (instance != null)
			Destroy(this);
		else
			instance = this;
	}

	public bool playIntro = true;

	[SerializeField] private GameObject tower;
	[SerializeField] private GameObject lights;

	PlayerController player;
	private void Start()
	{
		player = FindAnyObjectByType<PlayerController>();
		if (playIntro)
			StartCoroutine(StartEvent());
	}
	public IEnumerator StartEvent()
	{
		SummonScreen(Color.black, 1f, false);

		player.canMove = false;

		tower.SetActive(false);
		lights.SetActive(false);

		yield return new WaitForSeconds(1.5f);

		PlayThisSound("Voicelines", "Tutorial");
		yield return new WaitForSeconds(GetThisSound("Voicelines", "Tutorial").audioToPlay.length - 1.2f);
			
		tower.SetActive(true);
		lights.SetActive(true);

		player.canMove = true;
	}

	#region PlaySound-Subtitles
	[SerializeField] private GameObject audioSourceTemplate;
	[SerializeField] private GameObject subtitleTemplate;
	[SerializeField] private AudioLibrary[] audioLibraries;

	[System.Serializable] 
	public class AudioLibrary
	{
		public string libName = "New Audio Library";
		[Space(30)]
		public AudioToPlay[] audios;
	}
	
	public void RandomizePitchOnSound(string libId, string audioId, float min, float max)
	{
		var temp = GetThisSoundInScene(libId, audioId);

		temp.pitch = Random.Range(min, max);
	}
	public void RandomizePitchOnSound(AudioClip a, float min, float max)
	{
		AudioSource[] audioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);

		foreach (var audio in audioSources)
			if (audio.clip == a)
				audio.pitch = Random.Range(min, max);
	}
	public void PlayThisSound(string libId, string audioId)
	{
		var tempAudio = GetThisSound(libId, audioId);

		GameObject template = Instantiate(audioSourceTemplate, transform.position, Quaternion.identity);

		AudioSource templateSource = template.GetComponent<AudioSource>();

		templateSource.loop = tempAudio.loop;
		templateSource.clip = tempAudio.audioToPlay;
		templateSource.volume = tempAudio.audioVolume;
		templateSource.spatialBlend = tempAudio.spatialBlend;
		templateSource.maxDistance = tempAudio.maxDistance;

		templateSource.Play();

		if (!tempAudio.loop)
			Destroy(template, tempAudio.audioToPlay.length + 1f);

		if (tempAudio.subtitlesEnabled)
			foreach (var sub in tempAudio.subtitles)
				StartCoroutine(CreateSubtitle(sub.timeStarted, sub.timeDestroyed, sub.subtitleText));
	}
	public void PlayThisSound(AudioToPlay a)
	{
		var tempAudio = a;

		GameObject template = Instantiate(audioSourceTemplate, transform.position, Quaternion.identity);

		AudioSource templateSource = template.GetComponent<AudioSource>();

		templateSource.loop = tempAudio.loop;
		templateSource.clip = tempAudio.audioToPlay;
		templateSource.volume = tempAudio.audioVolume;
		templateSource.spatialBlend = tempAudio.spatialBlend;
		templateSource.maxDistance = tempAudio.maxDistance;

		templateSource.Play();

		if (!tempAudio.loop)
			Destroy(template, tempAudio.audioToPlay.length + 1f);

		if (tempAudio.subtitlesEnabled)
			foreach (var sub in tempAudio.subtitles)
				StartCoroutine(CreateSubtitle(sub.timeStarted, sub.timeDestroyed, sub.subtitleText));
	}

	public void StopThisSound(string libId, string audioId)
	{
		var temp = GetThisSoundInScene(libId, audioId);

		if (temp != null)
			Destroy(temp);
	}
	public void StopThisSound(AudioClip a)
	{
		AudioSource[] audioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);

		foreach (var audio in audioSources)
			if (audio.clip == a)
				Destroy(audio.gameObject);
	}
	public AudioToPlay GetThisSound(string libId, string audioId)
	{
		AudioLibrary curLib = null;

		foreach (AudioLibrary lib in audioLibraries)
			if (lib.libName == libId)
				curLib = lib;

		if (curLib != null)
			foreach (AudioToPlay audio in curLib.audios)
				if (audio.audioName == audioId)
					return audio;
		else
			Debug.LogWarning("No audio library found");

		return null;
	}
	private AudioSource GetThisSoundInScene(string libId, string audioId)
	{
		var tempAudio = GetThisSound(libId, audioId);

		AudioSource[] audioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);

		foreach (var audio in audioSources)
			if (audio.clip == tempAudio.audioToPlay)
				return audio;

		return null;
	}
	private IEnumerator CreateSubtitle(float timeUntilStarted, float timeUntilDestroyed, string textToBeDisplayed)
	{
		yield return new WaitForSeconds(timeUntilStarted);

		var _subtitleTemplate = Instantiate(subtitleTemplate, subtitleTemplate.transform.position, Quaternion.identity);
		var _subtitle = _subtitleTemplate.GetComponentInChildren<TextMeshProUGUI>();

		_subtitle.text = textToBeDisplayed;

		Destroy(_subtitleTemplate, timeUntilDestroyed - timeUntilStarted);
	}
	#endregion
	#region Activate-Deactivate-Gameobject
	public void ActivateObjects(GameObject[] objToActivate)
	{
		foreach (var obj in objToActivate)
			if (obj != null)
				obj.SetActive(true);
	}
	public void DeactivateObjects(GameObject[] objToDeactivate)
	{
		foreach (var obj in objToDeactivate)
			if (obj != null)
				obj.SetActive(false);
	}
	public void ActivateColliders(Collider[] objToActivate)
	{
		foreach (var obj in objToActivate)
			if (obj != null)
				obj.enabled = true;
	}
	public void DeactivateColliders(Collider[] objToDeactivate)
	{
		foreach (var obj in objToDeactivate)
			if (obj != null)
				obj.enabled = false;
	}
	#endregion

	public void CallDensityChange(float changedDensity)
	{
		StopAllCoroutines();
		StartCoroutine(DensityChangeLerp(changedDensity));
	}

	private IEnumerator DensityChangeLerp(float changedDensity)
	{
		while (Mathf.Abs(RenderSettings.fogDensity - changedDensity) > 0.001f) // Stop when close enough
		{
			RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, changedDensity, 5f * Time.deltaTime);
			yield return null; // Use `null` instead of `WaitForSeconds(.01f)` for smoother animation
		}

		RenderSettings.fogDensity = changedDensity; // Ensure it reaches the exact target
	}
	public void TeleportPlayer(Vector3 tpPos)
	{
		player.characterController.enabled = false;
		player.transform.position = tpPos;
		player.characterController.enabled = true;

		player.moveDirection = Vector3.zero;
	}
	public Transform bonusRoomSpawn;
	public void BonusRoomTeleport()
	{
		TeleportPlayer(bonusRoomSpawn.position);
		PlayThisSound("Voicelines", "BonusRoom");
	}

	#region UI-Events

	[SerializeField] private GameObject screenSummon;

	public void SummonScreen(Color screenColor, float lerpTime, bool transToFilled)
	{
		StartCoroutine(SummonScreenNumerator(screenColor, lerpTime, transToFilled));
	}

	private IEnumerator SummonScreenNumerator(Color screenColor, float lerpTime, bool transToFilled)
	{
		GameObject temp = Instantiate(screenSummon);
		Image img = temp.GetComponentInChildren<Image>();

		float startAlpha = transToFilled ? 0f : 1f;
		float endAlpha = transToFilled ? 1f : 0f;

		float timeElapsed = 0f;

		while (timeElapsed < lerpTime)
		{
			timeElapsed += Time.deltaTime;
			float t = timeElapsed / lerpTime;

			float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, t);
			img.color = new Color(screenColor.r, screenColor.g, screenColor.b, currentAlpha);

			yield return null;
		}

		img.color = new Color(screenColor.r, screenColor.g, screenColor.b, endAlpha);

		if (!transToFilled)
			Destroy(temp);
	}


	#endregion
}




[System.Serializable]
public class AudioToPlay
{
	public string audioName = "New Audio";

	[Space(30)]

	public bool loop = false;

	[Space(15)]

	public AudioClip audioToPlay;
	[Range(0, 1)]
	public float audioVolume = 1f;

	[Space(15)]

	[Range(0, 1)]
	public int spatialBlend = 0;
	[Range(0, 500)]
	public float maxDistance = 500f;

	[Space(15)]


	public bool subtitlesEnabled = false;

	public Subtitle[] subtitles;

	[System.Serializable]
	public class Subtitle
	{
		[TextArea]
		public string subtitleText;

		public float timeStarted = 0f;
		public float timeDestroyed = 1f;
	}
}


public abstract class Event : MonoBehaviour
{
	public bool showGizmo = true;
	public Color gizmoColor = Color.white;
	protected virtual void OnDrawGizmos()
	{
		if (!showGizmo)
			return;

		Gizmos.color = gizmoColor;

		if (GetComponent<Collider>() && GetComponent<Collider>().isTrigger)
			Gizmos.DrawCube(transform.position, transform.localScale);
	}

	protected abstract void CallEvent();

	private void OnTriggerEnter(Collider other)
	{
		if (!GetComponent<Collider>())
			return;

		if (other.gameObject.CompareTag("GameController"))
			CallEvent();
	}
}
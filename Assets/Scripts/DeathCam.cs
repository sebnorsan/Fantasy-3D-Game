using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using VolFx;
using static UnityEngine.GraphicsBuffer;

public class DeathCam : MonoBehaviour
{
	public PlayerController playerWhoKilled = null;

	[SerializeField] private Volume deathPostProcessing;

	[SerializeField] private float duration = 4f;

	private ComicsVol comicsRendering;

	private void Start()
	{
		if (deathPostProcessing.profile.TryGet(out ComicsVol c))
			comicsRendering = c;

		StartCoroutine(DieEffects());
	}
	private void Update()
	{
		if (playerWhoKilled == null) return;

		transform.LookAt(playerWhoKilled.transform);


	}

	private IEnumerator DieEffects()
	{
		if (comicsRendering == null)
			yield break;

		float start = comicsRendering.m_ColorWeight.value;
		float t = 0f;

		while (t < duration)
		{
			t += Time.deltaTime;
			float lerp = t / duration;
			comicsRendering.m_ColorWeight.value = Mathf.Lerp(start, 1, lerp);
			yield return null;
		}

		comicsRendering.m_ColorWeight.value = 1;
	}
}

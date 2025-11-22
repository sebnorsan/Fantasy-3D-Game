using UnityEngine;
using System.Collections;

public class MusicManager : MonoBehaviour
{
	public enum MusicState { Intro, Battle, Pause }

	[Header("Audio Sources")]
	public AudioSource intro;          // Calm intro music
	public AudioSource battle;         // Full battle music
	public AudioSource battleCalm;     // Calm variant of battle music

	[Header("Fade Settings")]
	public float introToBattleFadeTime = 2f;
	public float pauseFadeTime = 1f;
	public float battleFullVolume = 1f;
	public float battlePauseVolume = 0f;  // Non-zero default for audible pause fade
	public float muteFadeTime = 1f;    // Fade time when muting/unmuting

	[HideInInspector]
	public MusicState currentState = MusicState.Intro;

	private void Start()
	{
		// Initialize volumes and start all clips so fades work
		intro.volume = 0f;
		battle.volume = 0f;
		battleCalm.volume = 0f;
		intro.loop = battle.loop = battleCalm.loop = true;

		intro.Play();
		battle.Play();
		battleCalm.Play();
	}

	private IEnumerator FadeVolume(AudioSource src, float targetVolume, float duration)
	{
		float startVolume = src.volume;
		float elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			src.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
			yield return null;
		}
		src.volume = targetVolume;
	}

	/// <summary>
	/// Fade out all audio sources to silence.
	/// </summary>
	public void MuteAllFade()
	{
		StartCoroutine(FadeVolume(intro, 0f, muteFadeTime));
		StartCoroutine(FadeVolume(battle, 0f, muteFadeTime));
		StartCoroutine(FadeVolume(battleCalm, 0f, muteFadeTime));
	}

	/// <summary>
	/// Fade in audio sources based on the current music state.
	/// </summary>
	public void UnmuteAllFade()
	{
		switch (currentState)
		{
			case MusicState.Intro:
				StartCoroutine(FadeVolume(intro, 1f, muteFadeTime));
				StartCoroutine(FadeVolume(battle, 0f, muteFadeTime));
				StartCoroutine(FadeVolume(battleCalm, 0f, muteFadeTime));
				break;
			case MusicState.Battle:
				StartCoroutine(FadeVolume(intro, 0f, muteFadeTime));
				StartCoroutine(FadeVolume(battle, battleFullVolume, muteFadeTime));
				StartCoroutine(FadeVolume(battleCalm, 0f, muteFadeTime));
				break;
			case MusicState.Pause:
				StartCoroutine(FadeVolume(intro, 0f, muteFadeTime));
				StartCoroutine(FadeVolume(battle, battlePauseVolume, muteFadeTime));
				StartCoroutine(FadeVolume(battleCalm, 1f, muteFadeTime));
				break;
		}
	}

	public void ToBattle()
	{
		Debug.Log("[MusicManager] Transition to Battle");
		currentState = MusicState.Battle;
		StartCoroutine(FadeVolume(intro, 0f, introToBattleFadeTime));
		StartCoroutine(FadeVolume(battle, battleFullVolume, introToBattleFadeTime));
		StartCoroutine(FadeVolume(battleCalm, 0f, introToBattleFadeTime));
	}

	public void BattlePause()
	{
		Debug.Log("[MusicManager] BattlePause called, fading to calm battle");
		currentState = MusicState.Pause;
		StartCoroutine(FadeVolume(battle, battlePauseVolume, pauseFadeTime));
		StartCoroutine(FadeVolume(battleCalm, 1f, pauseFadeTime));
	}

	public void BattleResume()
	{
		Debug.Log("[MusicManager] BattleResume called, fading to full battle");
		currentState = MusicState.Battle;
		StartCoroutine(FadeVolume(battle, battleFullVolume, pauseFadeTime));
		StartCoroutine(FadeVolume(battleCalm, 0f, pauseFadeTime));
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.GetComponent<PlayerController>())
		{
			MuteAllFade();
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.gameObject.GetComponent<PlayerController>())
		{
			UnmuteAllFade();
		}
	}
}

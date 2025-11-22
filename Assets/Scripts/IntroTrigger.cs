using UnityEngine;

public class IntroTrigger : MonoBehaviour
{
	[Tooltip("Assign your MusicManager instance here.")]
	public MusicManager musicManager;
	private bool hasTriggered = false;

	private void OnTriggerEnter(Collider other)
	{
		if (!hasTriggered && other.CompareTag("GameController"))
		{
			musicManager.intro.Play();

			hasTriggered = true;
			musicManager.currentState = MusicManager.MusicState.Intro;
			// Fade in intro music
			musicManager.UnmuteAllFade();
		}
	}
}
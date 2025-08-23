using UnityEngine;

public class BattleTrigger : MonoBehaviour
{
	[Tooltip("Assign your MusicManager instance here.")]
	public MusicManager musicManager;
	private bool hasTriggered = false;

	private void OnTriggerEnter(Collider other)
	{
		if (!hasTriggered && other.CompareTag("GameController"))
		{
			musicManager.battle.Play();
			musicManager.battleCalm.Play();

			hasTriggered = true;
			musicManager.ToBattle();
		}
	}
}
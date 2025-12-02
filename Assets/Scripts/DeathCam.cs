using UnityEngine;

public class DeathCam : MonoBehaviour
{
	public PlayerController playerWhoKilled = null;

	private void Update()
	{
		if (playerWhoKilled == null) return;

		transform.LookAt(playerWhoKilled.transform);
	}
}

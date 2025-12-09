using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PvPPowerupSpawner : NetworkBehaviour
{
	[Header("Powerups to spawn (prefabs with NetworkObject + PvPPowerup)")]
	[SerializeField] private GameObject[] powerupPrefabs;

	[Header("Random spawn interval (seconds)")]
	[SerializeField] private float minSpawnDelay = 10f;
	[SerializeField] private float maxSpawnDelay = 20f;

	private PvPPowerup currentPowerup;

	public override void OnNetworkSpawn()
	{
		if (!IsServer) return;
		if (powerupPrefabs == null || powerupPrefabs.Length == 0) return;

		StartCoroutine(SpawnLoop());
	}

	private IEnumerator SpawnLoop()
	{
		while (true)
		{
			// Wait until there is no active powerup from this spawner
			while (currentPowerup != null &&
				   currentPowerup.NetworkObject != null &&
				   currentPowerup.NetworkObject.IsSpawned)
			{
				yield return null;
			}

			// Random delay before next spawn
			float delay = Random.Range(minSpawnDelay, maxSpawnDelay);
			yield return new WaitForSeconds(delay);

			// Double-check nothing spawned in the meantime
			if (currentPowerup == null ||
				currentPowerup.NetworkObject == null ||
				!currentPowerup.NetworkObject.IsSpawned)
			{
				SpawnRandomPowerup();
			}
		}
	}

	private void SpawnRandomPowerup()
	{
		if (powerupPrefabs.Length == 0) return;

		int index = Random.Range(0, powerupPrefabs.Length);
		var prefab = powerupPrefabs[index];
		if (prefab == null) return;

		var go = Instantiate(prefab, transform.position, Quaternion.identity);
		var netObj = go.GetComponent<NetworkObject>();
		if (netObj == null)
		{
			Debug.LogWarning("Powerup prefab is missing NetworkObject.", this);
			Destroy(go);
			return;
		}

		netObj.Spawn(); // server-side spawn

		currentPowerup = go.GetComponent<PvPPowerup>();
		if (currentPowerup != null)
			currentPowerup.Init(this);
	}

	// called by PvPPowerup when picked up / consumed
	public void NotifyPowerupConsumed(PvPPowerup powerup)
	{
		if (powerup == currentPowerup)
			currentPowerup = null;
	}
}

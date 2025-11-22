using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameSceneSpawnManager : NetworkBehaviour
{
	[Header("References")]
	[SerializeField] private NetworkObject playerPrefab;
	[SerializeField] private BoxCollider spawnArea; // should be a trigger collider

	[Header("Spawn Tuning")]
	[SerializeField] private float minDistanceBetweenPlayers = 2.0f;
	[SerializeField] private int maxAttemptsPerPlayer = 30;
	[SerializeField] private bool spawnOnLateJoin = true;

	// Keep track of where we've spawned players this round
	private readonly List<Vector3> usedSpawnPoints = new();

	public override void OnNetworkSpawn()
	{
		if (!IsServer) return;

		// Spawn everyone already connected (lobby -> game transition)
		SpawnAllCurrentlyConnectedPlayers();

		// Hook late join
		NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
		NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
	}

	public override void OnNetworkDespawn()
	{
		if (!IsServer || NetworkManager.Singleton == null) return;

		NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
		NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
	}

	private void SpawnAllCurrentlyConnectedPlayers()
	{
		foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
		{
			// If they already have a player object, skip
			if (NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject != null)
				continue;

			SpawnPlayerForClient(clientId);
		}
	}

	private void OnClientConnected(ulong clientId)
	{
		if (!spawnOnLateJoin) return;

		// If already has a player (rare but safe check)
		if (NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject != null)
			return;

		SpawnPlayerForClient(clientId);
	}

	private void OnClientDisconnected(ulong clientId)
	{
		// Optional: remove that player's spawn point so new late joiners can reuse space
		// If you want that behavior, uncomment below:

		var playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
		if (playerObj != null)
			usedSpawnPoints.Remove(playerObj.transform.position);
	}

	private void SpawnPlayerForClient(ulong clientId)
	{
		Vector3 spawnPos = FindValidSpawnPoint();
		Quaternion spawnRot = Quaternion.identity;

		var player = Instantiate(playerPrefab, spawnPos, spawnRot);
		player.SpawnAsPlayerObject(clientId, true);

		usedSpawnPoints.Add(spawnPos);
	}

	private Vector3 FindValidSpawnPoint()
	{
		if (spawnArea == null)
		{
			Debug.LogError("[GameSceneSpawnManager] No spawnArea assigned!");
			return Vector3.zero;
		}

		Bounds b = spawnArea.bounds;

		for (int attempt = 0; attempt < maxAttemptsPerPlayer; attempt++)
		{
			// Random point inside box bounds
			Vector3 candidate = new Vector3(
				Random.Range(b.min.x, b.max.x),
				Random.Range(b.min.y, b.max.y),
				Random.Range(b.min.z, b.max.z)
			);

			// If you want to always spawn on floor, raycast down:
			candidate = ProjectToGround(candidate);

			if (IsFarEnough(candidate))
				return candidate;
		}

		// Fallback: if we can't find a spaced point, just pick any ground point
		Vector3 fallback = ProjectToGround(new Vector3(
			Random.Range(b.min.x, b.max.x),
			Random.Range(b.min.y, b.max.y),
			Random.Range(b.min.z, b.max.z)
		));

		Debug.LogWarning("[GameSceneSpawnManager] Using fallback spawn point.");
		return fallback;
	}

	private bool IsFarEnough(Vector3 candidate)
	{
		float minDistSqr = minDistanceBetweenPlayers * minDistanceBetweenPlayers;

		for (int i = 0; i < usedSpawnPoints.Count; i++)
		{
			if ((usedSpawnPoints[i] - candidate).sqrMagnitude < minDistSqr)
				return false;
		}

		return true;
	}

	private Vector3 ProjectToGround(Vector3 point)
	{
		// Cast downward from above the point to find ground.
		// Adjust height/layermask as needed.
		Ray ray = new Ray(point + Vector3.up * 10f, Vector3.down);

		if (Physics.Raycast(ray, out RaycastHit hit, 50f, ~0, QueryTriggerInteraction.Ignore))
		{
			return hit.point + new Vector3(0, 2, 0);
		}

		return point; // if no ground hit, keep original
	}
}

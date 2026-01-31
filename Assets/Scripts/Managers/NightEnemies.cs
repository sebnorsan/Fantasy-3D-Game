using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class NightEnemies : NetworkBehaviour
{
	[Header("Enemy Prefabs (must have NetworkObject)")]
	[SerializeField] private GameObject[] enemyPrefabs;

	[Header("Spawn Around Player")]
	[SerializeField] private float spawnRadius = 20f;
	[SerializeField] private float minSpawnRadius = 8f; // so they don't pop on top of player
	[SerializeField] private float spawnInterval = 3f;

	[Header("Validation")]
	[SerializeField] private float maxNavSampleDistance = 2f;
	[SerializeField] private LayerMask obstacleMask;
	[SerializeField] private float obstacleCheckRadius = 0.5f;

	[Header("Limits")]
	[SerializeField] private int maxAlivePerPlayer = 6;

	private readonly Dictionary<ulong, Coroutine> _spawnRoutineByClient = new();
	private readonly Dictionary<ulong, List<NetworkObject>> _aliveByClient = new();

	private bool IsNight() => GameStateManager.GetCurrentGameState() == GameStateType.Night;

	private void OnTriggerExit(Collider other)
	{
		if (!IsServer) return;

		var playerNO = other.GetComponentInParent<NetworkObject>();
		if (playerNO == null || !playerNO.IsPlayerObject) return;

		StartSpawningFor(playerNO.OwnerClientId, playerNO.transform);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!IsServer) return;

		var playerNO = other.GetComponentInParent<NetworkObject>();
		if (playerNO == null || !playerNO.IsPlayerObject) return;

		StopSpawningFor(playerNO.OwnerClientId);
	}

	private void StartSpawningFor(ulong clientId, Transform player)
	{
		if (_spawnRoutineByClient.ContainsKey(clientId)) return;

		if (!_aliveByClient.ContainsKey(clientId))
			_aliveByClient[clientId] = new List<NetworkObject>();

		_spawnRoutineByClient[clientId] = StartCoroutine(SpawnLoop(clientId, player));
	}

	private void StopSpawningFor(ulong clientId)
	{
		if (_spawnRoutineByClient.TryGetValue(clientId, out var co))
		{
			StopCoroutine(co);
			_spawnRoutineByClient.Remove(clientId);
		}
	}

	private IEnumerator SpawnLoop(ulong clientId, Transform player)
	{
		while (true)
		{
			if (player == null)
			{
				_spawnRoutineByClient.Remove(clientId);
				yield break;
			}

			// only spawn at night
			if (!IsNight())
			{
				yield return new WaitForSeconds(1f);
				continue;
			}

			CleanupDead(clientId);

			if (_aliveByClient[clientId].Count < maxAlivePerPlayer)
				TrySpawnOne(player, clientId);

			yield return new WaitForSeconds(spawnInterval);
		}
	}

	private void CleanupDead(ulong clientId)
	{
		var list = _aliveByClient[clientId];
		for (int i = list.Count - 1; i >= 0; i--)
		{
			if (list[i] == null || !list[i].IsSpawned)
				list.RemoveAt(i);
		}
	}

	private bool TrySpawnOne(Transform player, ulong clientId)
	{
		if (enemyPrefabs == null || enemyPrefabs.Length == 0) return false;

		const int attempts = 8;

		for (int i = 0; i < attempts; i++)
		{
			Vector3 dir = Random.insideUnitSphere;
			dir.y = 0f;
			if (dir.sqrMagnitude < 0.0001f) continue;
			dir.Normalize();

			float dist = Random.Range(minSpawnRadius, spawnRadius);
			Vector3 candidate = player.position + dir * dist;

			if (obstacleMask.value != 0 && Physics.CheckSphere(candidate, obstacleCheckRadius, obstacleMask))
				continue;

			if (!NavMesh.SamplePosition(candidate, out var hit, maxNavSampleDistance, NavMesh.AllAreas))
				continue;

			var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
			var inst = Instantiate(prefab, hit.position, Quaternion.identity);

			var nwo = inst.GetComponent<NetworkObject>();
			if (nwo == null)
			{
				Destroy(inst);
				continue;
			}

			nwo.Spawn();

			var lvl = inst.GetComponent<EnemyLevelling>();
			if (lvl != null)
				lvl.CheckAndAssignLevel(EnemyWaveController.instance.GetCurrentWave(), 0);

			_aliveByClient[clientId].Add(nwo);
			return true;
		}

		return false;
	}

	public override void OnNetworkDespawn()
	{
		if (!IsServer) return;

		foreach (var kv in _spawnRoutineByClient)
			if (kv.Value != null) StopCoroutine(kv.Value);

		_spawnRoutineByClient.Clear();
		_aliveByClient.Clear();
	}
}

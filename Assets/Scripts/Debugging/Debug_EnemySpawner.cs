using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.AI;

public class Debug_EnemySpawner : NetworkBehaviour
{

	[SerializeField] private LayerMask obstacleMask;
	[SerializeField] private float maxNavSampleDistance = 1f;

	[SerializeField] private float spawnRadius;
    [SerializeField] private GameObject enemyToSpawn;
    [SerializeField] private float spawnDelay = .2f;
    [SerializeField] private KeyCode spawnToggler = KeyCode.O;
    private bool isSpawning = false;
	[Space(15)]
	public int amountSpawned;

	private Coroutine _spawnRoutine;
	private void Update()
	{
		if (Input.GetKeyDown(spawnToggler)) DoToggleRpc();
	}
	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void DoToggleRpc()
	{
		if (!NetworkManager.Singleton.IsServer) return;

		isSpawning = !isSpawning;

		if (_spawnRoutine == null)
			_spawnRoutine = StartCoroutine(SpawnLoop());
	}
	private IEnumerator SpawnLoop()
	{
		while (isSpawning)
		{
			if (!NetworkManager.Singleton.IsServer)
				yield break; // safety

			yield return new WaitForSeconds(spawnDelay);

			TrySpawnOne();
		}

		_spawnRoutine = null;
	}
	private void TrySpawnOne()
	{
		if (!NetworkManager.Singleton.IsServer)
			return;

		// pick random prefab in tier
		var prefab = enemyToSpawn;

		Vector3 offset = Random.insideUnitSphere * spawnRadius;
		offset.y = 0;
		Vector3 candidate = transform.position + offset;

		// obstacle collision check
		if (Physics.CheckSphere(candidate, 0.5f, obstacleMask))
			return;

		// NavMesh sample
		if (NavMesh.SamplePosition(candidate, out var hit, maxNavSampleDistance, NavMesh.AllAreas))
		{
			// SERVER spawns networked enemy
			var inst = Instantiate(prefab, hit.position, Quaternion.identity);
			var nwo = inst.GetComponent<NetworkObject>();
			if (nwo != null)
				nwo.Spawn();
			else
				Debug.LogWarning($"Spawned enemy '{prefab.name}' has no NetworkObject!");

			amountSpawned++;
		}
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
		Gizmos.DrawSphere(transform.position, spawnRadius);
	}
}

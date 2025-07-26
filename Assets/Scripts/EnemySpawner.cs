using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(SphereCollider))]
public class EnemySpawner : MonoBehaviour
{
	[Header("Spawn Settings")]
	[Tooltip("How far from the spawner's center to try and place enemies")]
	public float spawnRadius = 10f;
	[Tooltip("Minimum time between spawns")]
	public float minSpawnInterval = 2f;
	[Tooltip("Maximum time between spawns")]
	public float maxSpawnInterval = 5f;
	[Tooltip("Enemy prefab must have a NavMeshAgent")]
	public GameObject enemyPrefab;

	[Header("Validation")]
	[Tooltip("Layers to consider when checking for obstacles (optional)")]
	public LayerMask obstacleMask;
	[Tooltip("Max distance for NavMesh sampling")]
	public float maxNavSampleDistance = 1.0f;

	private Coroutine _spawnRoutine;

	void OnEnable()
	{
		// ensure our collider is a trigger so it won't collide
		SphereCollider col = GetComponent<SphereCollider>();
		col.isTrigger = true;
		col.radius = spawnRadius;

		_spawnRoutine = StartCoroutine(SpawnLoop());
	}

	void OnDisable()
	{
		if (_spawnRoutine != null)
			StopCoroutine(_spawnRoutine);
	}

	private IEnumerator SpawnLoop()
	{
		while (true)
		{
			float wait = Random.Range(minSpawnInterval, maxSpawnInterval);
			yield return new WaitForSeconds(wait);
			TrySpawnEnemy();
		}
	}

	private void TrySpawnEnemy()
	{
		// pick a random point in sphere (uniform distribution)
		Vector3 randomOffset = Random.insideUnitSphere * spawnRadius;
		Vector3 candidatePoint = transform.position + randomOffset;

		// optional: reject if overlapping some obstacle
		if (Physics.CheckSphere(candidatePoint, 0.5f, obstacleMask))
			return;

		// find nearest point on NavMesh
		NavMeshHit hit;
		if (NavMesh.SamplePosition(candidatePoint, out hit, maxNavSampleDistance, NavMesh.AllAreas))
		{
			Instantiate(enemyPrefab, hit.position, Quaternion.identity);
		}
	}

	// draw a red wireframe sphere in the Editor
	void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, spawnRadius);
	}
}

using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawnerManager : MonoBehaviour
{
	public static EnemySpawnerManager instance;

	[Header("Global Spawn Settings")]
	[Tooltip("Minimum interval between spawns.")]
	public float minSpawnInterval = 2f;
	[Tooltip("Maximum interval between spawns.")]
	public float maxSpawnInterval = 5f;
	[Tooltip("Radius around each spawn point to place enemies.")]
	public float spawnRadius = 5f;

	[Header("Tiers (in order)")]
	[Tooltip("List of all tiers; index corresponds to countsPerTier in WaveDefinition.")]
	public EnemyTier[] tiers;

	[Header("Waves")]
	[Tooltip("Sequence of waves; each defines counts per tier.")]
	public WaveDefinition[] waves;

	[Header("Spawn Points")]
	[Tooltip("Assign your spawn location transforms here.")]
	public Transform[] spawnPoints;

	[Header("NavMesh & Obstacle")]
	public LayerMask obstacleMask;
	public float maxNavSampleDistance = 1f;

	[HideInInspector] public int _currentWave = 0;
	private int[] _remainingThisWave;
	private Coroutine _spawnRoutine;

	private void Awake()
	{
		_currentWave = 0;

		if (instance != null)
			Destroy(gameObject);
		else
			instance = this;
	}

	void OnEnable()
	{
		StartWave(_currentWave);
		_spawnRoutine = StartCoroutine(SpawnLoop());
	}

	void OnDisable()
	{
		if (_spawnRoutine != null)
			StopCoroutine(_spawnRoutine);
	}

	void StartWave(int waveIndex)
	{
		if (waveIndex < 0 || waveIndex >= waves.Length)
		{
			Debug.Log("No more waves to run.");
			return;
		}
		var wave = waves[waveIndex];

		_remainingThisWave = wave.countsPerTier.ToArray();
		Debug.Log($"Wave {waveIndex + 1} started: total enemies = {_remainingThisWave.Sum()}");
	}

	IEnumerator SpawnLoop()
	{
		while (true)
		{
			yield return new WaitForSeconds(Random.Range(minSpawnInterval, maxSpawnInterval));

			// If no counts left, end this wave
			if (_remainingThisWave == null || _remainingThisWave.Sum() == 0)
			{
				Debug.Log($"Wave {_currentWave + 1} complete.");
				if (_spawnRoutine != null)
					StopCoroutine(_spawnRoutine);
				yield break;
			}

			TrySpawnOne();
		}
	}

	void TrySpawnOne()
	{
		// pick a random tier with remaining > 0
		var available = _remainingThisWave
			.Select((count, idx) => new { count, idx })
			.Where(x => x.count > 0)
			.ToArray();
		if (available.Length == 0)
			return;

		int tierIdx = available[Random.Range(0, available.Length)].idx;
		var tier = tiers[tierIdx];
		if (tier.enemyPrefabs == null || tier.enemyPrefabs.Length == 0)
			return;

		// pick random prefab in tier
		var prefab = tier.enemyPrefabs[Random.Range(0, tier.enemyPrefabs.Length)];

		// pick random spawn point
		var sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
		Vector3 offset = Random.insideUnitSphere * spawnRadius;
		offset.y = 0;
		Vector3 candidate = sp.position + offset;

		// obstacle collision check
		if (Physics.CheckSphere(candidate, 0.5f, obstacleMask))
			return;

		// NavMesh sample
		if (NavMesh.SamplePosition(candidate, out var hit, maxNavSampleDistance, NavMesh.AllAreas))
		{
			Instantiate(prefab, hit.position, Quaternion.identity);
			_remainingThisWave[tierIdx]--;
		}
	}

	void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		if (spawnPoints != null)
		{
			foreach (var sp in spawnPoints)
				if (sp != null)
					Gizmos.DrawWireSphere(sp.position, spawnRadius);
		}
	}
}

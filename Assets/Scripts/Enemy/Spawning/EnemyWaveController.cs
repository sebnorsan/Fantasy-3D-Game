using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Linq;

public class EnemyWaveController : NetworkBehaviour
{
    public static EnemyWaveController instance;

	[SerializeField] private EnemyWaveTimer enemyWaveTimer;
	[SerializeField] private EnemyWaveVisuals enemyWaveVisuals;

    [Header("NavMesh & Obstacles")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float maxNavSampleDistance = 1f;

    [Space(8)]
	[Header("Tiers")]
	[SerializeField] private EnemyTiers[] enemyTiers;

    [Space(2)]
    [Header("Waves")]
    [SerializeField] private EnemyWaves[] enemyWaves;

    [Space(2)]
    [Header("Spawnpoints")]
    [SerializeField] private EnemySpawnPoint[] enemySpawnPoints;

	#region Serializables
	[Serializable]
    public class EnemyTiers
    {
        public GameObject[] enemyPrefabs;
    }
    [Serializable]
    public class EnemyWaves
    {
		[Space(5)]

        public float minSpawnInterval = .1f;
        public float maxSpawnInterval = 3f;

        public int[] enemyTierSpawnAmount;

		public int enemyDownTime = 60;

		public IslandSpawnPoints[] spawnPointsAvailable =
		{
			IslandSpawnPoints.Marsh,
			IslandSpawnPoints.Desert,
			IslandSpawnPoints.Ice,
			IslandSpawnPoints.Fire,
			IslandSpawnPoints.Forest
		};
	}
    [Serializable]
    public class EnemySpawnPoint
    {
		public IslandSpawnPoints spawnPointType;
        public Transform spawnPoint;
        public float spawnRadius = 3f;
    }
	#endregion

	[Header("DEFAULT VALUE IS -1")]
	[SerializeField] private int _currentWave = -1;
	private int[] _remainingThisWave;
	private Coroutine _spawnRoutine;

	private EnemySpawnPoint[] _spawnPointsThisWave;

	public enum IslandSpawnPoints
	{
		Marsh,
		Desert,
		Ice,
		Fire,
		Forest
	}

	private bool IsSpawnPointAllowed(IslandSpawnPoints type, IslandSpawnPoints[] allowed)
	{
		if (allowed == null || allowed.Length == 0) return true; // fallback = allow all

		for (int i = 0; i < allowed.Length; i++)
			if (allowed[i] == type)
				return true;

		return false;
	}

	private int GetWaveTierIsFirstSpawned(int tier)
	{
		for (int i = 0; i < enemyWaves.Length; i++)
		{
			if (enemyWaves[i].enemyTierSpawnAmount.Length > tier)
				return i;
		}
		return 0;
	}

	private void OnEnable()
	{
		EnemyWaveTimer.downTimeTimerFinished += StartNextWave;
		AbstractEnemy.enemyHasDied += CheckForFinish;
	}
	private void OnDisable()
	{
		EnemyWaveTimer.downTimeTimerFinished -= StartNextWave;
		AbstractEnemy.enemyHasDied -= CheckForFinish;
	}

	public int GetNextWave() => _currentWave + 1;
	private void Awake()
	{
		if (instance != null)
			Destroy(gameObject);
		else
			instance = this;
	}
	private void Update()
	{
		if (_spawnRoutine == null)
			if (Input.GetKeyDown(KeyCode.O))
				StartNextWave();
	}
	private void StartNextWave()
	{
		if (!NetworkManager.Singleton.IsServer)
			return;

		StartWave(GetNextWave());
	}
	public void StartWave(int waveIndex)
	{
		if (!NetworkManager.Singleton.IsServer) return;

		if (GameStateManager.GetCurrentGameState() == GameStateType.Night)
			enemyWaveVisuals.SetVisualType(GameStateType.Morning, 3f);

		CancelInvoke();

		if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
		{
			Debug.LogError("No spawn points set!");
			return;
		}

		if (waveIndex < 0 || waveIndex >= enemyWaves.Length)
		{
			Debug.Log("No more waves to run.");
			return;
		}

		_currentWave = waveIndex;
		var wave = enemyWaves[waveIndex];

		// spawnpoints for this wave
		var tmp = new System.Collections.Generic.List<EnemySpawnPoint>(enemySpawnPoints.Length);
		for (int i = 0; i < enemySpawnPoints.Length; i++)
		{
			var sp = enemySpawnPoints[i];
			if (sp == null || sp.spawnPoint == null) continue;
			if (IsSpawnPointAllowed(sp.spawnPointType, wave.spawnPointsAvailable))
				tmp.Add(sp);
		}
		_spawnPointsThisWave = tmp.ToArray();

		if (_spawnPointsThisWave.Length == 0)
			Debug.LogWarning($"Wave {waveIndex + 1}: No spawn points match spawnPointsAvailable!");

		_remainingThisWave = wave.enemyTierSpawnAmount.ToArray();

		Debug.Log($"Wave {waveIndex + 1} started: total enemies = {_remainingThisWave.Sum()}");

		if (_spawnRoutine != null) StopCoroutine(_spawnRoutine);
		_spawnRoutine = StartCoroutine(SpawnLoop());
	}
	private void WaveFinish()
	{
		if (!NetworkManager.Singleton.IsServer)
			return;

		if (_spawnRoutine != null)
			StopCoroutine(_spawnRoutine);

		enemyWaveTimer.StartDownTimeTimer(enemyWaves[_currentWave].enemyDownTime);
		enemyWaveVisuals.SetVisualType(GameStateType.Night, 3f);

		Debug.Log($"Wave {_currentWave + 1} complete.");

		//Start downtime for next wave, currently no fail safe to check if any enemies are left in the map, so should add that as well
	}
	/// <summary>
	/// This also plays, everytime the static Action timerFinished is Invoked();
	/// Maybe create a fail safe if we dont want that to happen everytime it is finished
	/// </summary>
	private void CheckForFinish()
	{
		if (AbstractEnemy.All.Count > 0)
			return;

		WaveFinish();
		CancelInvoke();
	}
	private IEnumerator SpawnLoop()
	{
		while (true)
		{
			if (!NetworkManager.Singleton.IsServer)
				yield break; // safety

			yield return new WaitForSeconds(UnityEngine.Random.Range(enemyWaves[_currentWave].minSpawnInterval, enemyWaves[_currentWave].maxSpawnInterval));

			if (_remainingThisWave == null || _remainingThisWave.Sum() == 0)
				yield break;

			TrySpawnOne();
		}
	}
	private void TrySpawnOne()
	{
		if (!NetworkManager.Singleton.IsServer)
			return;
		if (_remainingThisWave == null || _remainingThisWave.Length == 0)
			return;
		if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
			return;
		if (_spawnPointsThisWave == null || _spawnPointsThisWave.Length == 0)
			return;

		//Makes a new Array, and sets the up where count is how many enemies are left, and idx is what tier that enemy is.
		var available = _remainingThisWave
			.Select((count, idx) => new { count, idx })
			.Where(x => x.count > 0)
			.ToArray();

		if (available.Length == 0)
			return;

		int tierIdx = available[UnityEngine.Random.Range(0, available.Length)].idx;

		if (tierIdx < 0 || tierIdx >= enemyTiers.Length)
			return;

		var tier = enemyTiers[tierIdx];
		if (tier.enemyPrefabs == null || tier.enemyPrefabs.Length == 0)
			return;

		// pick random prefab in tier
		var prefab = tier.enemyPrefabs[UnityEngine.Random.Range(0, tier.enemyPrefabs.Length)];

		var sp = _spawnPointsThisWave[UnityEngine.Random.Range(0, _spawnPointsThisWave.Length)];

		Vector3 offset = UnityEngine.Random.insideUnitSphere * sp.spawnRadius;
		offset.y = 0;
		Vector3 candidate = sp.spawnPoint.position + offset;

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
			{
				Debug.LogWarning($"Spawned enemy '{prefab.name}' has no NetworkObject!");
				return;
			}

			_remainingThisWave[tierIdx]--;
		}
	}
	//private bool TrySpawnOne()
	//{
	//	if (!NetworkManager.Singleton.IsServer)
	//		return false;

	//	if (_remainingThisWave == null || _remainingThisWave.Length == 0)
	//		return false;

	//	if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
	//		return false;

	//	if (_spawnPointsThisWave == null || _spawnPointsThisWave.Length == 0)
	//		return false;

	//	// pick a random tier with remaining > 0
	//	var available = _remainingThisWave
	//		.Select((count, idx) => new { count, idx })
	//		.Where(x => x.count > 0)
	//		.ToArray();

	//	if (available.Length == 0)
	//		return false;

	//	int tierIdx = available[UnityEngine.Random.Range(0, available.Length)].idx;

	//	if (tierIdx < 0 || tierIdx >= enemyTiers.Length)
	//		return false;

	//	var tier = enemyTiers[tierIdx];
	//	if (tier.enemyPrefabs == null || tier.enemyPrefabs.Length == 0)
	//		return false;

	//	// pick random prefab in tier
	//	var prefab = tier.enemyPrefabs[UnityEngine.Random.Range(0, tier.enemyPrefabs.Length)];

	//	// try a few positions so we don't stall if one is invalid
	//	const int positionAttempts = 8;

	//	for (int i = 0; i < positionAttempts; i++)
	//	{
	//		// pick random spawn point
	//		var sp = _spawnPointsThisWave[UnityEngine.Random.Range(0, _spawnPointsThisWave.Length)];

	//		if (sp == null || sp.spawnPoint == null)
	//			continue;

	//		Vector3 offset = UnityEngine.Random.insideUnitSphere * sp.spawnRadius;
	//		offset.y = 0f;
	//		Vector3 candidate = sp.spawnPoint.position + offset;

	//		// obstacle collision check (skip if mask is Nothing / 0)
	//		if (obstacleMask.value != 0 && Physics.CheckSphere(candidate, 0.5f, obstacleMask))
	//			continue;

	//		// NavMesh sample
	//		if (!NavMesh.SamplePosition(candidate, out var hit, maxNavSampleDistance, NavMesh.AllAreas))
	//			continue;

	//		// SERVER spawns networked enemy
	//		var inst = Instantiate(prefab, hit.position, Quaternion.identity);
	//		var nwo = inst.GetComponent<NetworkObject>();

	//		if (nwo == null)
	//		{
	//			Debug.LogWarning($"Spawned enemy '{prefab.name}' has no NetworkObject! Destroying instance.");
	//			Destroy(inst);
	//			continue;
	//		}

	//		nwo.Spawn();

	//		var lvl = inst.GetComponent<EnemyLevelling>();
	//		if (lvl != null)
	//			lvl.CheckAndAssignLevel(_currentWave, GetWaveTierIsFirstSpawned(tierIdx));

	//		_remainingThisWave[tierIdx]--;
	//		return true;
	//	}

	//	// couldn't find a valid position this tick
	//	return false;
	//}
	private void OnDrawGizmos()
	{
		Gizmos.color = new Color(1f, 0f, 0f, 0.6f);
		if (enemySpawnPoints.Length > 0)
		{
			foreach (var sp in enemySpawnPoints)
				if (sp != null)
					if (sp.spawnPoint != null)
						Gizmos.DrawSphere(sp.spawnPoint.position, sp.spawnRadius);
		}
	}
}

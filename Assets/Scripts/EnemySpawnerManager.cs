using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawnerManager : NetworkBehaviour
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

	[Header("DEFAULT VALUE IS -1")]
	public int _currentWave = -1;
	private int[] _remainingThisWave;
	private Coroutine _spawnRoutine;

	[SerializeField] private TMPro.TextMeshProUGUI timerText;
	[SerializeField] private string[] actionAfterWave;

	private void Awake()
	{
		if (timerText != null)
			timerText.text = string.Empty;

		if (instance != null)
			Destroy(gameObject);
		else
			instance = this;
	}

	/// <summary>
	/// Called by WaveController. Only the server actually starts the wave.
	/// </summary>
	public void StartWave(int waveIndex)
	{
		if (!NetworkManager.Singleton.IsServer)
			return;

		if (waveIndex < 0 || waveIndex >= waves.Length)
		{
			Debug.Log("No more waves to run.");
			return;
		}

		_currentWave = waveIndex;

		var wave = waves[waveIndex];

		minSpawnInterval = wave.minimiumSpawnInterval;
		maxSpawnInterval = wave.maximumSpawnInterval;

		_remainingThisWave = wave.countsPerTier.ToArray();
		Debug.Log($"Wave {waveIndex + 1} started: total enemies = {_remainingThisWave.Sum()}");

		// music on all clients
		SetBattleMusicStateClientRpc(true);

		if (_spawnRoutine != null)
			StopCoroutine(_spawnRoutine);

		_spawnRoutine = StartCoroutine(SpawnLoop());
	}

	private void WaveFinish()
	{
		if (!NetworkManager.Singleton.IsServer)
			return;

		// music off on all clients
		SetBattleMusicStateClientRpc(false);

		//if (_currentWave >= 0 && _currentWave < actionAfterWave.Length)
		//	DialogueManager.instance.SetActive(actionAfterWave[_currentWave], true);

		if (_spawnRoutine != null)
			StopCoroutine(_spawnRoutine);

		Debug.Log($"Wave {_currentWave + 1} complete.");

		// start countdown to next wave (server only; UI updated via RPC)
		StartCoroutine(TimerCountdown(60));
	}

	private IEnumerator TimerCountdown(int totalSeconds)
	{
		int remaining = totalSeconds;
		while (remaining >= 0)
		{
			UpdateTimerClientRpc(remaining);
			yield return new WaitForSeconds(1f);
			remaining--;
		}

		ClearTimerClientRpc();

		// Countdown finished, tell everyone to play wave intro animation
		StartWaveAnimation();
	}

	private IEnumerator SpawnLoop()
	{
		while (true)
		{
			if (!NetworkManager.Singleton.IsServer)
				yield break; // safety

			yield return new WaitForSeconds(Random.Range(minSpawnInterval, maxSpawnInterval));

			if (_remainingThisWave == null || _remainingThisWave.Sum() == 0)
			{
				WaveFinish();
				yield break;
			}

			TrySpawnOne();
		}
	}

	private void TrySpawnOne()
	{
		if (!NetworkManager.Singleton.IsServer)
			return;

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
			// SERVER spawns networked enemy
			var inst = Instantiate(prefab, hit.position, Quaternion.identity);
			var nwo = inst.GetComponent<NetworkObject>();
			if (nwo != null)
				nwo.Spawn();
			else
				Debug.LogWarning($"Spawned enemy '{prefab.name}' has no NetworkObject!");

			_remainingThisWave[tierIdx]--;
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
		if (spawnPoints != null)
		{
			foreach (var sp in spawnPoints)
				if (sp != null)
					Gizmos.DrawSphere(sp.position, spawnRadius);
		}
	}

	public bool ReachedFinalWave()
	{
		return _currentWave >= waves.Length - 1;
	}

	// --------- RPCs for music, timer & wave UI ----------

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void SetBattleMusicStateClientRpc(bool inBattle)
	{
		var music = FindFirstObjectByType<MusicManager>();
		if (music == null) return;

		if (inBattle)
			music.BattleResume();
		else
			music.BattlePause();
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void UpdateTimerClientRpc(int remainingSeconds)
	{
		if (timerText == null) return;

		int minutes = remainingSeconds / 60;
		int seconds = remainingSeconds % 60;
		timerText.text = $"{minutes}:{seconds:00}";
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void ClearTimerClientRpc()
	{
		if (timerText != null)
			timerText.text = string.Empty;
	}

	//[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void StartWaveAnimation()
	{
		var waveCtrl = FindFirstObjectByType<WaveController>();
		if (waveCtrl != null)
			waveCtrl.StartWaveAnimation();
	}
}

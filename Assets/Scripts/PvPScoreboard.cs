using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PvPScoreboard : MonoBehaviour
{
	public static PvPScoreboard Instance { get; private set; }

	[SerializeField] private Transform contentRoot;
	[SerializeField] private PvPScoreboardRow rowPrefab;

	private readonly Dictionary<ulong, PvPScoreboardRow> rows =
		new Dictionary<ulong, PvPScoreboardRow>();

	private float resyncTimer = 0f;
	private const float RESYNC_INTERVAL = 1f; // seconds

	private void Awake()
	{
		Instance = this;
	}

	private void OnDestroy()
	{
		if (Instance == this)
			Instance = null;
	}

	private void OnEnable()
	{
		Resync();
	}

	private void Update()
	{
		resyncTimer -= Time.unscaledDeltaTime;
		if (resyncTimer <= 0f)
		{
			resyncTimer = RESYNC_INTERVAL;
			Resync();
		}
	}

	private void Resync()
	{
		// 1) Add missing players
		var players = FindObjectsByType<PlayerPvP>(FindObjectsSortMode.None);

		foreach (var p in players)
		{
			if (p == null || !p.IsSpawned) continue;

			ulong key = p.NetworkObjectId;
			if (!rows.ContainsKey(key))
			{
				var row = Instantiate(rowPrefab, contentRoot);
				row.Init(p);
				rows[key] = row;
			}
		}

		// 2) Remove rows whose players no longer exist
		var toRemove = new List<ulong>();
		foreach (var kvp in rows)
		{
			var row = kvp.Value;
			if (row == null || row.Player == null || !row.Player.IsSpawned)
			{
				if (row != null)
					Destroy(row.gameObject);
				toRemove.Add(kvp.Key);
			}
		}

		foreach (var key in toRemove)
			rows.Remove(key);
	}

	// Optional: keep your explicit register/unregister
	public void RegisterPlayer(PlayerPvP stats)
	{
		if (stats == null || !stats.IsSpawned) return;

		ulong key = stats.NetworkObjectId;
		if (rows.ContainsKey(key)) return;

		var row = Instantiate(rowPrefab, contentRoot);
		row.Init(stats);
		rows[key] = row;
	}

	public void UnregisterPlayer(PlayerPvP stats)
	{
		if (stats == null) return;

		ulong key = stats.NetworkObjectId;
		if (!rows.TryGetValue(key, out var row)) return;

		Destroy(row.gameObject);
		rows.Remove(key);
	}
}

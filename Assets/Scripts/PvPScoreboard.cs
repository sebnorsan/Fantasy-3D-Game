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
			else
			{
				// careful: this re-subscribes events each second – you might want to remove this
				rows[key].Init(p);
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

		// 3) Sort rows by kills (and optional tie-breaker)
		SortRows();
	}

	private void SortRows()
	{
		// Copy rows into a list
		var list = new List<PvPScoreboardRow>(rows.Values);

		// Sort: most kills at top, then fewest deaths, then stable by id
		list.Sort((a, b) =>
		{
			if (a == null || a.Player == null) return 1;
			if (b == null || b.Player == null) return -1;

			// 1) Kills DESC
			int killCompare = b.Player.Kills.Value.CompareTo(a.Player.Kills.Value);
			if (killCompare != 0) return killCompare;

			// 2) Deaths ASC (less deaths = higher)
			int deathCompare = a.Player.Deaths.Value.CompareTo(b.Player.Deaths.Value);
			if (deathCompare != 0) return deathCompare;

			// 3) Tie-breaker: NetworkObjectId ASC
			return a.Player.NetworkObjectId.CompareTo(b.Player.NetworkObjectId);
		});

		// Apply order to hierarchy (0 = top)
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i] != null)
				list[i].transform.SetSiblingIndex(i);
		}
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

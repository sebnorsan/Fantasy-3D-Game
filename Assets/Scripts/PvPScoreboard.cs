using System.Collections.Generic;
using UnityEngine;

public class PvPScoreboard : MonoBehaviour
{
	public static PvPScoreboard Instance { get; private set; }

	[SerializeField] private Transform contentRoot;
	[SerializeField] private PvPScoreboardRow rowPrefab;

	private readonly Dictionary<ulong, PvPScoreboardRow> rows =
		new Dictionary<ulong, PvPScoreboardRow>();

	private void Awake()
	{
		Instance = this;

		// Register any already spawned players
		foreach (var stats in FindObjectsByType<PlayerPvP>(FindObjectsSortMode.None))
			RegisterPlayer(stats);
	}

	private void OnDestroy()
	{
		if (Instance == this)
			Instance = null;
	}

	public void RegisterPlayer(PlayerPvP stats)
	{
		ulong key = stats.OwnerClientId;
		if (rows.ContainsKey(key)) return;

		var row = Instantiate(rowPrefab, contentRoot);
		row.Init(stats);
		rows[key] = row;
	}

	public void UnregisterPlayer(PlayerPvP stats)
	{
		ulong key = stats.OwnerClientId;
		if (!rows.TryGetValue(key, out var row)) return;

		Destroy(row.gameObject);
		rows.Remove(key);
	}
}

using System.Collections.Generic;
using UnityEngine;

public class PvPScoreboard : MonoBehaviour
{
	public static PvPScoreboard Instance { get; private set; }

	[SerializeField] private Transform contentRoot;     // parent with VerticalLayoutGroup
	[SerializeField] private PvPScoreboardRow rowPrefab;

	private readonly Dictionary<ulong, PvPScoreboardRow> rows =
		new Dictionary<ulong, PvPScoreboardRow>();

	private void Awake()
	{
		Instance = this;
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

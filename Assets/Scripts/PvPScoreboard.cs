using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Collections;
using static UnityEngine.Rendering.DebugUI.Table;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PvPScoreboard : MonoBehaviour
{
	public static PvPScoreboard Instance { get; private set; }

	[SerializeField] private Transform contentRoot;
	[SerializeField] private PvPScoreboardRow rowPrefab;
	[SerializeField] private GridLayoutGroup gridLayoutGroup;

	private readonly Dictionary<ulong, PvPScoreboardRow> rows =
		new Dictionary<ulong, PvPScoreboardRow>();

	private float resyncTimer = 0f;
	private const float RESYNC_INTERVAL = 1f; // seconds
	private Coroutine sortRoutine;

	private void Awake()
	{
		Instance = this;

		if (gridLayoutGroup == null && contentRoot != null)
			gridLayoutGroup = contentRoot.GetComponent<GridLayoutGroup>();
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
		if (sortRoutine != null)
			StopCoroutine(sortRoutine);

		sortRoutine = StartCoroutine(SortRowsLerped());
	}

	private IEnumerator SortRowsLerped()
	{
		var list = new List<PvPScoreboardRow>(rows.Values);

		// 1) capture where visuals are NOW
		foreach (var r in list)
		{
			if (r != null)
				r.CaptureStartPos();
		}

		// 2) sort
		list.Sort((a, b) =>
		{
			if (a == null || a.Player == null) return 1;
			if (b == null || b.Player == null) return -1;

			int killCompare = b.Player.Kills.Value.CompareTo(a.Player.Kills.Value);
			if (killCompare != 0) return killCompare;

			int deathCompare = a.Player.Deaths.Value.CompareTo(b.Player.Deaths.Value);
			if (deathCompare != 0) return deathCompare;

			return a.Player.NetworkObjectId.CompareTo(b.Player.NetworkObjectId);
		});

		// 3) apply sibling index – GridLayoutGroup will place them
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i] != null)
				list[i].transform.SetSiblingIndex(i);
		}

		// wait one layout pass
		yield return new WaitForEndOfFrame();

		// 4) tell each row to slide its visuals from old -> new
		foreach (var r in list)
		{
			if (r != null)
				r.SetTargetToCurrentPos();
		}

		sortRoutine = null;
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
		StartCoroutine(UnregisterIE(stats));
	}
	private IEnumerator UnregisterIE(PlayerPvP stats)
	{
		if (stats == null) yield return null;

		ulong key = stats.NetworkObjectId;
		if (!rows.TryGetValue(key, out var row)) yield return null;

		if (row != null)
			row.GetComponentInChildren<Animator>().SetTrigger("Outro");
		
		yield return new WaitForSeconds(1f);

		if (row != null)
			Destroy(row.gameObject);

		rows.Remove(key);

		// make sure the remaining rows re-evaluate who is on top
		SortRows();
	}
}

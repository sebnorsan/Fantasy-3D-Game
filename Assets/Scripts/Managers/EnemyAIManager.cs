using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemyAIManager : MonoBehaviour
{
	public static EnemyAIManager Instance { get; private set; }

	public List<AbstractEnemy> enemies = new List<AbstractEnemy>();

	[Header("AI LOD distances")]
	[SerializeField] private float nearDist = 10f;
	[SerializeField] private float midDist = 25f;
	[SerializeField] private float farDist = 40f;

	// optional: explicitly assign which transform to use for LOD
	[SerializeField] private Transform lodReference; // e.g. main player / crystal

	private float nearDistSqr;
	private float midDistSqr;
	private float farDistSqr;

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;

		nearDistSqr = nearDist * nearDist;
		midDistSqr = midDist * midDist;
		farDistSqr = farDist * farDist;
	}

	public void Register(AbstractEnemy enemy)
	{
		if (enemy == null) return;
		if (!enemies.Contains(enemy))
			enemies.Add(enemy);
	}

	public void Unregister(AbstractEnemy enemy)
	{
		if (enemy == null) return;
		enemies.Remove(enemy);
	}

	private void Update()
	{
		if (lodReference == null)
		{
			var localClient = NetworkManager.Singleton.LocalClient;
			if (localClient != null && localClient.PlayerObject != null)
				lodReference = localClient.PlayerObject.transform;
		}

		// still only simulate AI on server
		if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
		if (enemies.Count == 0) return;

		int frame = Time.frameCount;

		for (int i = enemies.Count - 1; i >= 0; i--)
		{
			var enemy = enemies[i];
			if (enemy == null || !enemy.IsSpawned)
			{
				enemies.RemoveAt(i);
				continue;
			}

			float distSqr = GetLocalRefDistSqr(enemy.transform.position);

			int interval = 1; // close = every frame

			if (distSqr > farDistSqr) interval = 8;
			else if (distSqr > midDistSqr) interval = 4;
			else if (distSqr > nearDistSqr) interval = 2;

			if (frame % interval != 0)
				continue;

			enemy.TickAI();
		}
	}

	// ---- ONLY local reference, no "closest player" loop ----
	private float GetLocalRefDistSqr(Vector3 pos)
	{
		// 1) If you dragged a transform into lodReference, use that
		if (lodReference != null)
			return (lodReference.position - pos).sqrMagnitude;

		// 2) Fallback: use LocalClient's player object (host/client)
		var nm = NetworkManager.Singleton;
		if (nm != null && nm.LocalClient != null && nm.LocalClient.PlayerObject != null)
		{
			var p = nm.LocalClient.PlayerObject.transform.position;
			return (p - pos).sqrMagnitude;
		}

		// 3) If nothing available, just treat as "near"
		return 0f;
	}
}

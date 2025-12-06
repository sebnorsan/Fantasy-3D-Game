using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemyLODManager : MonoBehaviour
{
	public static EnemyLODManager instance;

	[SerializeField] private Transform player;
	public List<EnemyAnimatorLOD> enemies = new List<EnemyAnimatorLOD>();

	int index;
	private void Awake()
	{
		if (instance != null)
			Destroy(gameObject);
		else
			instance = this;
	}
	void Update()
	{
		if (player == null)
		{
			var localClient = NetworkManager.Singleton.LocalClient;
			if (localClient != null && localClient.PlayerObject != null)
				player = localClient.PlayerObject.transform;
		}

		if (player == null || enemies.Count == 0) return;

		int checksPerFrame = 200;

		for (int i = 0; i < checksPerFrame && enemies.Count > 0; i++)
		{
			if (index >= enemies.Count) index = 0;

			var e = enemies[index];
			index++;

			if (!e) continue;

			e.UpdateLOD(player.position);
		}
	}

}

using UnityEngine;

[CreateAssetMenu(menuName = "Spawning/Enemy Tier")]
public class EnemyTier : ScriptableObject
{
	[Tooltip("All the prefabs that belong to this tier.")]
	public GameObject[] enemyPrefabs;
}
using UnityEngine;

[CreateAssetMenu(menuName = "Spawning/Wave Definition")]
public class WaveDefinition : ScriptableObject
{
	public float minimiumSpawnInterval = 2;
	public float maximumSpawnInterval = 7;

	[Tooltip("For each tier in the Manager, how many to spawn this wave.")]
	public int[] countsPerTier;
}

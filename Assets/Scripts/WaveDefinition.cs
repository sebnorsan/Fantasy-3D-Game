using UnityEngine;

[CreateAssetMenu(menuName = "Spawning/Wave Definition")]
public class WaveDefinition : ScriptableObject
{
	[Tooltip("For each tier in the Manager, how many to spawn this wave.")]
	public int[] countsPerTier;
}

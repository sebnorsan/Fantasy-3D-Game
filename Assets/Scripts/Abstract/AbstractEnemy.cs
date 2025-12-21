using Unity.Netcode;
using UnityEngine;

public abstract class AbstractEnemy : NetworkBehaviour
{
	[SerializeField] private EnemyReferences eRef;

	[Space(5)]

	[Header("References")]
	public EnemyAnimatorLOD lod_anim;

	public int xpDrop = 1;

	private void Start()
	{
		EnemyLODManager.instance?.enemies.Add(lod_anim);
	}
}
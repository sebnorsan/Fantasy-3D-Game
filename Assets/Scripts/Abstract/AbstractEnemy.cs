using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class AbstractEnemy : NetworkBehaviour
{
	[SerializeField] private EnemyReferences eRef;

	[Space(5)]

	[Header("References")]
	public EnemyAnimatorLOD lod_anim;

	public int xpDrop = 1;

	public static readonly List<AbstractEnemy> All = new();

	private void OnEnable()
	{
		if (!All.Contains(this))
			All.Add(this);
	}

	private void OnDisable()
	{
		All.Remove(this);
	}


	private void Start()
	{
		EnemyLODManager.instance?.enemies.Add(lod_anim);
	}
	public void AlertOfTargetDeath()
	{
		eRef.enemyNavigation.TargetSlain();
	}
}
using UnityEngine;

public class EnemyMultipliers : AbstractMultipliers
{
	[Space(5)]

	[SerializeField] private EnemyReferences eRef;

	protected override void ApplyValues()
	{
		eRef.enemyNavigation.SetSpeedMultiplier();
		eRef.enemyDamagable.SetHealthMultiplier();
	}
}

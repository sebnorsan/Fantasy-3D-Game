using UnityEngine;

public class EnemyReferences : MonoBehaviour
{
	public AbstractEnemy enemy;
	public AbstractEnemyAttack enemyAttack;
    public AbstractEnemyDamagable enemyDamagable;
    public AbstractEnemyNavigation enemyNavigation;
	public EnemyAnimator enemyAnimator;
	//---Set during runtime---
	[HideInInspector] public EnemyTarget enemyTarget = null;
}

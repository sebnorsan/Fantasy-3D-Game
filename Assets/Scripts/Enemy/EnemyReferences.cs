using UnityEngine;

public class EnemyReferences : MonoBehaviour
{
	public AbstractEnemy enemy;
	public AbstractEnemyAttack enemyAttack;
    public AbstractEnemyDamagable enemyDamagable;
    public AbstractEnemyNavigation enemyNavigation;
	public EnemyAnimator enemyAnimator;
	public EnemyMultipliers enemyMultipliers;
	public EnemyLevelling enemyLevel;
	public EnemyGraphics enemyGraphics;
	public ExperienceEmitter experienceEmitter;
	//---Set during runtime---
	[HideInInspector] public EnemyTarget enemyTarget = null;
}

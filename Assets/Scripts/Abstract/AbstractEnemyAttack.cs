using Unity.Netcode;
using UnityEngine;
using System.Collections;

public abstract class AbstractEnemyAttack : NetworkBehaviour
{
	[SerializeField] private EnemyReferences eRef;

	[Space(5)]

	[SerializeField] private int damage = 1;
	[SerializeField] private float attackDelay = 3;

	[SerializeField] private float attackTurnSpeed = 10f;

	private Coroutine attackCoroutine;

	public void AE_AttackTarget()
	{
		if (!NetworkManager.Singleton.IsServer) return;

		if (eRef.enemyTarget == null) return;

		eRef.enemyTarget.TakeDamage(damage);
	}
	/// <summary>
	/// These are called from the EnemyTarget script itself,
	/// when an enemy enters its trigger, it starts attacking
	/// and when an enemy leaves its trigger, it stops.
	/// </summary>
	//------------------------
	public void StartAttack()
	{
		if (attackCoroutine == null)
			attackCoroutine = StartCoroutine(AttackNumerator());
	}

	public void StopAttack()
	{
		if (attackCoroutine != null)
			StopCoroutine(attackCoroutine);
	}
	//------------------------
	protected virtual IEnumerator AttackNumerator()
	{
		eRef.enemyAnimator.A_SetWalk(false);
		float elapsed = 0f;
		while (elapsed < attackDelay)
		{
			elapsed += Time.deltaTime;
			RotateTowardsTarget();
			yield return null;
		}
		eRef.enemyAnimator.A_Attack();
		attackCoroutine = StartCoroutine(AttackNumerator());
	}
	private void RotateTowardsTarget()
	{
		if (eRef.enemyTarget == null) return;

		Vector3 dir = eRef.enemyTarget.transform.position - transform.position;
		dir.y = 0f;
		if (dir.sqrMagnitude < 0.0001f) return;

		Quaternion lookRot = Quaternion.LookRotation(dir);
		transform.rotation = Quaternion.Slerp(
			transform.rotation,
			lookRot,
			Time.deltaTime * attackTurnSpeed
		);
	}
}
using Unity.Netcode;
using UnityEngine;
using System.Collections;

public abstract class AbstractEnemyAttack : NetworkBehaviour
{
	[SerializeField] private EnemyReferences eRef;

	[Space(5)]

	[SerializeField] private float damageToPlayer = 1;
	[SerializeField] private float damageToTarget = 1;
	[SerializeField] private float attackDelay = 3;

	[SerializeField] private float attackTurnSpeed = 10f;

	private Coroutine attackCoroutine;

	public void AE_AttackTarget()
	{
		if (!NetworkManager.Singleton.IsServer) return;

		if (eRef.playerTarget != null)
			eRef.playerTarget.TakeDamage(damageToPlayer, transform.position, null, 1);
		if (eRef.enemyTarget != null)
			eRef.enemyTarget.TakeDamage(eRef.enemyMultipliers.GetDamageMulti(damageToTarget));
	}
	/// <summary>
	/// These are called from the EnemyTarget script itself,
	/// when an enemy enters its trigger, it starts attacking
	/// and when an enemy leaves its trigger, it stops.
	/// </summary>
	//------------------------
	public void StartAttack()
	{
		if (!NetworkManager.Singleton.IsServer) return;

		if (attackCoroutine == null)
			attackCoroutine = StartCoroutine(AttackNumerator());
	}

	public void StopAttack()
	{
		if (!NetworkManager.Singleton.IsServer) return;

		if (attackCoroutine != null)
		{
			StopCoroutine(attackCoroutine);
			attackCoroutine = null; // <-- important
		}
		eRef.enemyAnimator.A_SetWalk(true); // optional: so they resume moving
	}

	//------------------------
	protected virtual IEnumerator AttackNumerator()
	{
		eRef.enemyAnimator.A_SetWalk(false);
		float elapsed = 0f;
		float delay = eRef.enemyMultipliers.ApplyInverseMultiplier(attackDelay, eRef.enemyMultipliers.attackSpeedMultiplier);

		if (eRef.playerTarget != null)
			eRef.enemyAnimator.A_Attack();

		while (elapsed < delay)
		{
			if (GetTargetTransform() == null)
			{
				attackCoroutine = null;
				eRef.enemyAnimator.A_SetWalk(true);
				yield break;
			}

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

		Vector3 dir = GetTargetTransform().position - transform.position;
		dir.y = 0f;
		if (dir.sqrMagnitude < 0.0001f) return;

		Quaternion lookRot = Quaternion.LookRotation(dir);
		transform.rotation = Quaternion.Slerp(
			transform.rotation,
			lookRot,
			Time.deltaTime * attackTurnSpeed
		);
	}
	private Transform GetTargetTransform()
	{
		if (eRef.playerTarget != null) 
			return eRef.playerTarget.transform;
		if (eRef.enemyTarget != null) 
			return eRef.enemyTarget.transform;
		return null;
	}

}
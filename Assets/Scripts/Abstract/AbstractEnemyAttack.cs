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
	[SerializeField] private float playerAttackRange = 1.5f;
	[SerializeField] private float attackTurnSpeed = 10f;

	private Coroutine attackCoroutine;

	public float PlayerAttackRange => playerAttackRange;

	public void AE_AttackTarget()
	{
		if (!NetworkManager.Singleton.IsServer) return;

		if (eRef.playerTarget != null)
			eRef.playerTarget.TakeDamage(damageToPlayer, transform.position, null, 1);

		if (eRef.enemyTarget != null)
			eRef.enemyTarget.TakeDamage(eRef.enemyMultipliers.GetMulti(damageToTarget, Multiplier.Damage));
	}

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
			attackCoroutine = null;
		}

		eRef.enemyAnimator.A_SetWalk(true);
	}

	protected virtual IEnumerator AttackNumerator()
	{
		eRef.enemyAnimator.A_SetWalk(false);

		while (true)
		{
			if (GetTargetTransform() == null)
			{
				attackCoroutine = null;
				eRef.enemyAnimator.A_SetWalk(true);
				yield break;
			}

			float delay = eRef.enemyMultipliers.ApplyInverseMultiplier(
				attackDelay,
				eRef.enemyMultipliers.GetMultiplierPercent(Multiplier.AtkSpeed)
			);

			// PLAYER TARGET = direct damage, no animation
			if (eRef.playerTarget != null)
			{
				float sqr = (eRef.playerTarget.transform.position - transform.position).sqrMagnitude;
				if (sqr <= playerAttackRange * playerAttackRange)
					eRef.playerTarget.TakeDamage(damageToPlayer, transform.position, null, 1);

				float elapsed = 0;

				while (elapsed < delay)
				{
					if (GetTargetTransform() == null || eRef.playerTarget == null)
					{
						attackCoroutine = null;
						eRef.enemyAnimator.A_SetWalk(true);
						yield break;
					}

					elapsed += Time.deltaTime;
					yield return null;
				}

				continue;
			}

			// CRYSTAL / NORMAL TARGET = keep animation attack
			eRef.enemyAnimator.A_Attack();

			float animElapsed = 0f;
			while (animElapsed < delay)
			{
				if (GetTargetTransform() == null)
				{
					attackCoroutine = null;
					eRef.enemyAnimator.A_SetWalk(true);
					yield break;
				}

				animElapsed += Time.deltaTime;
				RotateTowardsTarget();
				yield return null;
			}
		}
	}

	private void RotateTowardsTarget()
	{
		Transform target = GetTargetTransform();
		if (target == null) return;

		Vector3 dir = target.position - transform.position;
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
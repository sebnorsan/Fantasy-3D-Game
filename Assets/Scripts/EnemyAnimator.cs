using UnityEngine;

public class EnemyAnimator : MonoBehaviour
{
	[SerializeField] private EnemyReferences eRef;

	[Space(5)]

	[SerializeField] private Animator anim;

    public void A_TakeDamage()
    {
		anim?.SetTrigger("Damage");
	}
	public void A_SetWalk(bool b)
	{
		anim?.SetBool("Walking", b);
	}
	public void A_Attack()
	{
		anim?.SetTrigger("Attack");
	}
}

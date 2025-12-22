using Unity.Netcode;
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
	public void A_SetVictory(bool b)
	{
		anim?.SetBool("Victory", b);
	}
	public void A_SetTarget(bool targetNotExist)
	{
		anim?.SetBool("NoTarget", targetNotExist);
	}

	public void AE_DespawnObject()
	{
		DespawnServerRpc();
	}
	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void DespawnServerRpc()
	{
		eRef.enemyDamagable.DespawnObject();
	}
}

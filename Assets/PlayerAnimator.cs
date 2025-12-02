using Unity.Netcode;
using UnityEngine;

public class PlayerAnimator : NetworkBehaviour
{
	[SerializeField] private Animator firstPersonAnim;
	[SerializeField] private Animator thirdPersonAnim;
	[SerializeField] private GameObject gfx;

	public override void OnNetworkSpawn()
	{
		if (IsOwner)
			gfx.SetActive(false);
	}

	public void A_TakeDamage()
	{

	}
	public void A_Walking()
	{

	}
	public void A_Jump()
	{

	}
}

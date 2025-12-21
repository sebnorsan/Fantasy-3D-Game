using System.Collections;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class PlayerAnimator : NetworkBehaviour
{
	[SerializeField] private PlayerReferences pRef;

	[SerializeField] private Animator multiplayerBowAnim; // the world / 3rd-person one
	[SerializeField] private Animator multiplayerCharacterAnim;

	[Space(10)]
	[SerializeField] private float walkSpeedAnim;
	[SerializeField] private float runSpeedAnim;

	[Space(10)]

	private bool jumpBuffer = false;

	private void Update()
	{
		// Only the owner has valid input / local bow
		if (!IsOwner) return;

		A_WalkingChecker();

		AnimatorStateInfo bowStateInfo = multiplayerBowAnim.GetCurrentAnimatorStateInfo(0);

		bool affectedByDrawSpeed = false;
		if (bowStateInfo.IsName("bow_loadIn") || bowStateInfo.IsName("bow_loaded"))
			affectedByDrawSpeed = true;

		multiplayerBowAnim.speed = affectedByDrawSpeed ? pRef.playerMultipliers.GetAttackSpeedMulti(pRef.bowScript.arrowDrawSpeed) : 1f;

		AnimatorStateInfo charStateInfo = multiplayerCharacterAnim.GetCurrentAnimatorStateInfo(0);

		if (charStateInfo.IsName("Walk") && pRef.playerController.isRunning)
			multiplayerCharacterAnim.speed = runSpeedAnim;
		else
			multiplayerCharacterAnim.speed = walkSpeedAnim;
	}

	public void A_ResetBow()
	{
		multiplayerBowAnim.SetTrigger("Reset");
	}
	public void A_LoadBow()
	{
		multiplayerBowAnim.ResetTrigger("Shoot");
		multiplayerBowAnim.SetBool("Load", true);
	}

	public void A_UnLoadBow()
	{
		multiplayerBowAnim.SetBool("Load", false);
	}

	public void A_ShootBow()
	{
		multiplayerBowAnim.SetTrigger("Shoot");
	}




	public void A_TakeDamage()
	{
		multiplayerCharacterAnim.ResetTrigger("TakeDamage");
		multiplayerCharacterAnim.SetTrigger("TakeDamage");
	}
	public void A_WalkingChecker()
	{
		multiplayerCharacterAnim.SetBool("Walking", pRef.playerController.isMoving);
	}
	private float bufferTimer = .2f;
	public void A_Jump() 
	{
		if (jumpBuffer) return;

		jumpBuffer = true;
		Invoke(nameof(ResetJumpBuffer), bufferTimer);

		multiplayerCharacterAnim.ResetTrigger("Jump");
		multiplayerCharacterAnim.SetTrigger("Jump");

		SetInAir(true);
	}
	private void ResetJumpBuffer() => jumpBuffer = false;
	public void A_Land()
	{
		if (landBuffer)
		{
			SetInAir(false);

			if (bufferCoroutine != null)
				StopCoroutine(bufferCoroutine);

			return;
		}

		if (!multiplayerCharacterAnim.GetBool("InAir")) return;

		multiplayerCharacterAnim.ResetTrigger("Land");
		multiplayerCharacterAnim.SetTrigger("Land");

		SetInAir(false);
	}

	private bool landBuffer = false;
	public void SetInAir(bool b)
	{
		if (b)
		{
			landBuffer = true;
			if (bufferCoroutine != null)
				StopCoroutine(bufferCoroutine);
			bufferCoroutine = StartCoroutine(ResetLandingBuffer(.2f));
		}

		multiplayerCharacterAnim.SetBool("InAir", b);
	}
	private Coroutine bufferCoroutine = null;
	private IEnumerator ResetLandingBuffer(float timeSet)
	{
		yield return new WaitForSeconds(timeSet);
		landBuffer = false;
	}
	public void A_SetCrouch(bool crouching)
	{
		multiplayerCharacterAnim.SetBool("Crouching", crouching);
	}
}

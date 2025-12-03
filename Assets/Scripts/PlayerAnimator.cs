using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class PlayerAnimator : NetworkBehaviour
{
	[SerializeField] private Animator multiplayerBowAnim; // the world / 3rd-person one
	[SerializeField] private Animator multiplayerCharacterAnim;

	[Space(10)]
	[SerializeField] private float walkSpeedAnim;
	[SerializeField] private float runSpeedAnim;

	[Space(10)]

	[SerializeField] private PlayerController localPlayer;

	private BowScript localBowScript;

	private bool jumpBuffer = false;

	private void Start()
	{
		localBowScript = localPlayer.GetComponentInChildren<BowScript>();
	}

	private void Update()
	{
		// Only the owner has valid input / local bow
		if (!IsOwner) return;

		A_WalkingChecker();
		CheckForInAir();

		AnimatorStateInfo bowStateInfo = multiplayerBowAnim.GetCurrentAnimatorStateInfo(0);
		multiplayerBowAnim.speed = bowStateInfo.IsName("bow_loadIn") ? localBowScript.arrowDrawSpeed : 1f;

		AnimatorStateInfo charStateInfo = multiplayerCharacterAnim.GetCurrentAnimatorStateInfo(0);

		if (charStateInfo.IsName("Walk") && localPlayer.isRunning)
			multiplayerCharacterAnim.speed = runSpeedAnim;
		else
			multiplayerCharacterAnim.speed = walkSpeedAnim;
	}

	private void CheckForInAir()
	{
		//if (jumpBuffer) return;

		//if (multiplayerCharacterAnim.GetBool("InAir"))
		//	if (localPlayer.isGrounded)
		//	{
		//		A_Land();
		//		SetInAir(false);
		//	}
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
		multiplayerCharacterAnim.SetBool("Walking", localPlayer.Moving);
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

			ResetLandingBuffer();

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
			Invoke(nameof(ResetLandingBuffer), .1f);
		}

		multiplayerCharacterAnim.SetBool("InAir", b);
	}
	private void ResetLandingBuffer() => landBuffer = false;
}

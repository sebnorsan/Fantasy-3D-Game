using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class BowScript : MonoBehaviour
{
	public int arrowDamage = 1;
	public float arrowSpeed = 30f;
	public List<ArrowEffect> arrowEffect = new List<ArrowEffect>();
	public float arrowDrawSpeed = 1f;
	public float arrowSize = 1f;
	public int lightningChain = 3;
	public bool arrowCutsTrees = false;

	[Space(75)]
	private Animator anim;
	private HandsSmooth hs;
	private bool canShoot = true;

	[SerializeField] private GameObject arrowFired;   // MUST be the NetworkObject arrow prefab
	[SerializeField] private Transform arrowTransform;

	private PlayerController playerController;
	private BowNetCode bowNetcode;

	private void Start()
	{
		playerController = GetComponentInParent<PlayerController>();
		if (!playerController) return;

		if (!playerController.IsOwner) return;

		hs = GetComponentInParent<HandsSmooth>();
		anim = GetComponent<Animator>();

		bowNetcode = playerController.GetComponent<BowNetCode>();
		if (!bowNetcode)
			Debug.LogError("BowNetcode missing on player root!");
	}

	private void Update()
	{
		if (!playerController || !playerController.IsOwner) return;
		if (!canShoot) return;

		if (Input.GetMouseButton(0)) LoadBow();
		else UnLoadBow();

		if (Input.GetMouseButtonUp(0)) ShootBow();

		AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
		anim.speed = stateInfo.IsName("bow_loadIn") ? arrowDrawSpeed : 1f;
	}

	private void LoadBow()
	{
		anim.ResetTrigger("Shoot");
		hs.UnassignMaxAmounts();
		anim.SetBool("Load", true);
	}

	private void UnLoadBow()
	{
		hs.ReassignMaxAmounts();
		anim.SetBool("Load", false);
	}

	private void ShootBow()
	{
		anim.SetTrigger("Shoot");
		canShoot = false;
		Invoke(nameof(ResetShot), .35f);
	}

	private void ResetShot() => canShoot = true;

	// Call this from your animation event at the moment the arrow should fire
	public void InstantiateArrow()
	{
		Debug.Log("aaaaaa");

		if (!playerController || !playerController.IsOwner) return;
		if (!bowNetcode) return;

		// Aim direction from the owner's camera
		var cam = playerController.GetComponentInChildren<Camera>();
		Vector3 shootDir = cam.transform.forward;

		// send stats + fire request to server
		int[] effects = arrowEffect.Select(e => (int)e).ToArray();

		bowNetcode.SpawnArrowRequest(
			arrowFired,
			arrowTransform.position,
			arrowTransform.rotation,
			shootDir,
			arrowDamage,
			effects,
			arrowSpeed,
			arrowCutsTrees,
			lightningChain,
			arrowSize
		);
	}

	public void AssignMiddleString() => GetComponentInChildren<BowStringRend>().AssignMid();
	public void UnAssignMiddleString() => GetComponentInChildren<BowStringRend>().UnAssignMid();
}

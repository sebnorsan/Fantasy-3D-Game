using Unity.Netcode;
using UnityEngine;

public class BowScript : MonoBehaviour
{
	public int arrowDamage = 1;
	public float arrowSpeed = 30f;

	public float arrowDrawSpeed = 1f;
	public float arrowSize = 1f;

	[Space(75)]
	private Animator anim;
	private HandsSmooth hs;
	private bool canShoot = true;

	[SerializeField] private GameObject arrowFired;
	[SerializeField] private Transform arrowTransform;

	private PlayerController playerController;
	private BowNetCode bowNetcode;

	private PlayerAnimator playerAnimator;

	private void Start()
	{
		playerController = GetComponentInParent<PlayerController>();
		playerAnimator = playerController.GetComponentInChildren<PlayerAnimator>();
		if (!playerController) return;

		if (!playerController.IsOwner)
		{
			GetComponent<Outline>().OutlineMode = Outline.Mode.OutlineVisible;
			return;
		}

		hs = GetComponentInParent<HandsSmooth>();
		anim = GetComponent<Animator>();

		bowNetcode = playerController.GetComponent<BowNetCode>();
		if (!bowNetcode)
			Debug.LogError("BowNetcode missing on player root!");
	}

	private void Update()
	{
		if (!playerController || !playerController.IsOwner) return;

		if (!canShoot || !playerController.canMove) return;

		if (Input.GetMouseButton(0)) LoadBow();
		else UnLoadBow();

		if (Input.GetMouseButtonUp(0)) ShootBow();

		AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
		anim.speed = stateInfo.IsName("bow_loadIn") ? arrowDrawSpeed : 1f;
	}

	private void LoadBow()
	{
		playerAnimator.A_LoadBow();

		anim.ResetTrigger("Shoot");
		hs.UnassignMaxAmounts();
		anim.SetBool("Load", true);
	}

	private void UnLoadBow()
	{
		playerAnimator.A_UnLoadBow();

		hs.ReassignMaxAmounts();
		anim.SetBool("Load", false);
	}

	private void ShootBow()
	{
		playerAnimator.A_ShootBow();

		anim.SetTrigger("Shoot");
		canShoot = false;
		Invoke(nameof(ResetShot), .35f);
	}

	private void ResetShot() => canShoot = true;

	// Call this from your animation event at the moment the arrow should fire
	public void InstantiateArrow()
	{
		if (!playerController || !playerController.IsOwner) return;
		if (!bowNetcode) return;

		// Aim direction from the owner's camera
		var cam = playerController.GetComponentInChildren<Camera>();
		Vector3 shootDir = cam.transform.forward;

		SpawnArrow(arrowFired, arrowTransform.position, arrowTransform.rotation, shootDir, arrowDamage, arrowSpeed, arrowSize);
	}
	private void SpawnArrow(
		GameObject go,
		Vector3 pos,
		Quaternion rot,
		Vector3 shootDir,
		int dmg,
		float spd,
		float size)
	{
		ulong shooterId = NetworkManager.Singleton.LocalClientId;

		var arrowObj = Instantiate(go, pos, rot);
		var arrow = arrowObj.GetComponent<Arrow>();

		arrow.Initialize(dmg, spd, size, shootDir, playerController.transform.position, shooterId, true, bowNetcode);

		bowNetcode.SpawnArrowVisualServerRpc(pos, rot, shootDir, dmg, spd, size, shooterId);
	}
	public void AssignMiddleString() => GetComponentInChildren<BowStringRend>().AssignMid();
	public void UnAssignMiddleString() => GetComponentInChildren<BowStringRend>().UnAssignMid();
}

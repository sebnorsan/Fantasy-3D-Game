using Unity.Netcode;
using UnityEngine;

public class BowScript : MonoBehaviour
{
	[SerializeField] private PlayerReferences pRef;
	[SerializeField] private PlayerInputs pInput;

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

	private void Start()
	{
		if (!pRef) return;

		if (!pRef.IsOwner)
		{
			GetComponent<Outline>().OutlineMode = Outline.Mode.OutlineVisible;
			return;
		}

		hs = GetComponentInParent<HandsSmooth>();
		anim = GetComponent<Animator>();
	}

	private void Update()
	{
		if (!EventManager.instance.IsCameraPlayerMode()) return;

		if (!pRef || !pRef.IsOwner) return;

		if (!canShoot || !pRef.playerController.canMove) return;

		if (Input.GetKey(pInput.shootKey)) LoadBow();
		else UnLoadBow();

		if (Input.GetKeyUp(pInput.shootKey)) ShootBow();

		AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
		anim.speed = stateInfo.IsName("bow_loadIn") ? arrowDrawSpeed : 1f;
	}

	private void LoadBow()
	{
		pRef.playerAnimator.A_LoadBow();

		anim.ResetTrigger("Shoot");
		hs.UnassignMaxAmounts();
		anim.SetBool("Load", true);
	}

	private void UnLoadBow()
	{
		pRef.playerAnimator.A_UnLoadBow();

		hs.ReassignMaxAmounts();
		anim.SetBool("Load", false);
	}

	private void ShootBow()
	{
		pRef.playerAnimator.A_ShootBow();

		anim.SetTrigger("Shoot");
		canShoot = false;
		Invoke(nameof(ResetShot), .35f);
	}

	private void ResetShot() => canShoot = true;

	// Call this from your animation event at the moment the arrow should fire
	public void InstantiateArrow()
	{
		if (!pRef || !pRef.IsOwner || !pRef.bowNetCode) return;

		// Aim direction from the owner's camera
		Vector3 shootDir = pRef.playerCam.transform.forward;

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

		arrow.Initialize(dmg, spd, size, shootDir, pRef.playerController.transform.position, shooterId, true, pRef.bowNetCode);

		pRef.bowNetCode.SpawnArrowVisualServerRpc(pos, rot, shootDir, dmg, spd, size, shooterId);
	}
}

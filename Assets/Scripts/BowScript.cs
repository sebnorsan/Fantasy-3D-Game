using EvolveGames;
using System.Net.NetworkInformation;
using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

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
	[SerializeField] private GameObject arrowFired;
	[SerializeField] private Transform arrowTransform;
	private PlayerController playerController;
	private void Start()
	{
		playerController = GetComponentInParent<PlayerController>();

		if (!playerController.IsOwner) return;

		hs = GetComponentInParent<HandsSmooth>();
		anim = GetComponent<Animator>();
	}
	private void Update()
	{
		if (!playerController.IsOwner) return;

		if (!canShoot) return;

		if (Input.GetMouseButton(0))
			LoadBow();
		else
			UnLoadBow();

		if (Input.GetMouseButtonUp(0))
			ShootBow();

		// 🔥 Adjust speed if we’re in "bow_loadIn"
		AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
		if (stateInfo.IsName("bow_loadIn"))
		{
			anim.speed = arrowDrawSpeed; // e.g. 1f = normal, 2f = double speed
		}
		else
		{
			anim.speed = 1f; // reset to normal for everything else
		}
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
	
	//singleplayer only
	//public void InstantiateArrow()
	//{
	//	//arrowFired.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
	//	var go = Instantiate(arrowFired, arrowTransform.position, arrowFired.transform.rotation);

	//	go.TryGetComponent(out Arrow arrowComponent);

	//	arrowComponent.Initialize(this, GetComponentInParent<PlayerController>());
	//}
	public void InstantiateArrow()
	{
		if (!playerController.IsOwner) return;

		// call server to spawn arrow
		SpawnArrowServerRpc(arrowTransform.position, arrowTransform.rotation);
	}

	[ServerRpc]
	private void SpawnArrowServerRpc(Vector3 pos, Quaternion rot, ServerRpcParams rpcParams = default)
	{
		var arrowObj = Instantiate(arrowFired, pos, rot);
		var arrow = arrowObj.GetComponent<Arrow>();

		var shooterPos = playerController.transform.position;

		Ray ray = playerController.cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
		var shootDir = ray.direction.normalized;

		arrow.ServerInitialize(
			arrowDamage,
			arrowEffect.ToArray(),
			arrowSpeed,
			arrowCutsTrees,
			lightningChain,
			arrowSize,
			shootDir,
			shooterPos,
			rpcParams.Receive.SenderClientId
		);

		arrowObj.GetComponent<NetworkObject>().Spawn();

	}

	public void AssignMiddleString() => GetComponentInChildren<BowStringRend>().AssignMid();
	public void UnAssignMiddleString() => GetComponentInChildren<BowStringRend>().UnAssignMid();
}

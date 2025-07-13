using EvolveGames;
using System.Net.NetworkInformation;
using UnityEngine;

public class BowScript : MonoBehaviour
{
	private Animator anim;
	private HandsSmooth hs;
	private bool canShoot = true;
	[SerializeField] private GameObject arrowFired;
	[SerializeField] private Transform arrowTransform;
	private void Start()
	{
		hs = GetComponentInParent<HandsSmooth>();
		anim = GetComponent<Animator>();
	}
	private void Update()
	{
		if (!canShoot) return;

		if (Input.GetMouseButton(0))
			LoadBow();
		else
			UnLoadBow();

		if (Input.GetMouseButtonUp(0))
			ShootBow();
	}
	private void LoadBow()
	{
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
		Invoke(nameof(ResetShot), .15f);
	}
	private void ResetShot() => canShoot = true;
	public void InstantiateArrow()
	{
		//arrowFired.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
		Instantiate(arrowFired, arrowTransform.position, arrowFired.transform.rotation);
	}
	public void AssignMiddleString() => GetComponentInChildren<BowStringRend>().AssignMid();
	public void UnAssignMiddleString() => GetComponentInChildren<BowStringRend>().UnAssignMid();
}

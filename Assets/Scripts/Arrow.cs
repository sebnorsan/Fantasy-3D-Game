using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Arrow : MonoBehaviour
{
	[Header("Damage & Effects")]
	public int arrowDamage = 1;

	[Header("Flight Settings")]
	public float initialSpeed = 30f;
	public float dropGravity = 1f;
	public float lifeTime = 10f;

	private readonly Quaternion modelCorrection = Quaternion.Euler(90f, 0f, 0f);
	private Vector3 initPlayerPos;
	private Vector3 velocity;
	private Rigidbody rb;
	private ulong shooterClientId;
	[SerializeField] private bool isAuthority = false;

	private List<ulong> clientsHit = new List<ulong>();

	private BowNetCode bowNetCode;

	private void Awake()
	{
		rb = GetComponent<Rigidbody>();

		rb.isKinematic = false;
		rb.useGravity = false;
	}

	public void Initialize(
		int damage,
		float speed,
		float size,
		Vector3 shootDir,
		Vector3 shooterPos,
		ulong clientShooting,
		bool isAuthority,
		BowNetCode localNetCode = null
	)
	{
		this.isAuthority = isAuthority;
		bowNetCode = localNetCode;

		shooterClientId = clientShooting;

		arrowDamage = damage;
		initialSpeed = speed;

		transform.localScale *= size;

		initPlayerPos = shooterPos;

		rb.useGravity = false;

		shootDir = shootDir.normalized;
		velocity = shootDir * initialSpeed;
		transform.rotation = Quaternion.LookRotation(shootDir) * modelCorrection;

		StartCoroutine(LifeTimer());
		ShakeShooter();
		EnableTrailAfterDelay(0.03f);
	}

	private IEnumerator LifeTimer()
	{
		yield return new WaitForSeconds(lifeTime);
		Destroy(gameObject);
	}

	private void EnableTrailAfterDelay(float delay)
	{
		StartCoroutine(EnableTrail(delay));
	}

	private IEnumerator EnableTrail(float delay)
	{
		yield return new WaitForSeconds(delay);
		var tr = GetComponentInChildren<TrailRenderer>();
		if (tr != null) tr.enabled = true;
	}

	private void ShakeShooter()
	{
		if (!isAuthority) return;

		var c = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerReferences>().cameraShaker;

		c.ShakeOnce(2f, 3f, .1f, .2f);
	}

	private void FixedUpdate()
	{
		rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
		velocity += Vector3.down * dropGravity * Time.fixedDeltaTime;

		if (velocity.sqrMagnitude > 0.001f)
			rb.MoveRotation(Quaternion.LookRotation(velocity.normalized) * modelCorrection);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!isAuthority) return;

		CheckDamage(other.gameObject);
	}
	private void OnCollisionEnter(Collision collision)
	{
		if (!isAuthority) return;

		CheckDamage(collision.gameObject);
	}

	private void CheckDamage(GameObject go)
	{
		if (!go.TryGetComponent<IDamagable>(out var dmg))
			return;
		if (!go.TryGetComponent<NetworkObject>(out var netObj))
			return;

		ulong targetNetId = netObj.NetworkObjectId;
		Vector3 hitPoint = initPlayerPos;

		if (targetNetId == NetworkManager.Singleton.LocalClientId || clientsHit.Contains(targetNetId))
			return;

		clientsHit.Add(targetNetId);
		bowNetCode.HitServerRpc(targetNetId, arrowDamage, hitPoint, shooterClientId);
	}

	

}

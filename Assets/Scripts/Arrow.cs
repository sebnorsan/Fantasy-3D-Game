using System.Collections;
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
	private bool isAuthority;

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
		bool isAuthority
	)
	{
		this.isAuthority = isAuthority;

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

		EZCameraShake.CameraShaker.Instance.ShakeOnce(2f, 3f, .1f, .2f);
	}

	private void FixedUpdate()
	{
		rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
		velocity += Vector3.down * dropGravity * Time.fixedDeltaTime;

		if (velocity.sqrMagnitude > 0.001f)
			rb.MoveRotation(Quaternion.LookRotation(velocity.normalized) * modelCorrection);
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (!isAuthority) return;

		if (!collision.gameObject.TryGetComponent<IDamagable>(out var dmg))
			return;
		if (!collision.gameObject.TryGetComponent<NetworkObject>(out var netObj))
			return;

		ulong targetNetId = netObj.NetworkObjectId;
		Vector3 hitPoint = collision.GetContact(0).point;
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	void HitServerRpc(ulong targetNetId, int amount, Vector3 hitPoint)
	{
		var target = NetworkManager.Singleton.SpawnManager.SpawnedObjects[targetNetId];
		if (target.TryGetComponent<PlayerDamagable>(out var dmg))
			dmg.TakeDamage(amount, hitPoint);
	}

}

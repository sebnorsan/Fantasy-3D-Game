using UnityEngine;
using EZCameraShake;
using System.Security.Cryptography;

[RequireComponent(typeof(Rigidbody))]
public class Arrow : MonoBehaviour
{
	[Header("Damage & Effects")]
	public int arrowDamage = 1;
	public ArrowEffect arrowEffect;	
	public enum ArrowEffect
	{
		Normal
	}

	[Header("Flight Settings")]
	public float initialSpeed = 30f;   // How fast the arrow is shot
	public float dropGravity = 1f;     // How strong the downward pull is
	public float lifeTime = 10f;       // Destroy after this many seconds

	// Correct for a mesh that needs a 90° pitch to face its Z+ forward
	private readonly Quaternion modelCorrection = Quaternion.Euler(90f, 0f, 0f);

	private Vector3 initPlayerPos;
	private Vector3 velocity;
	private Rigidbody rb;
	private void Start()
	{
		initPlayerPos = FindFirstObjectByType<PlayerController>().transform.position;

		CameraShaker.Instance.ShakeOnce(2f, 3f, .1f, .2f);

		rb = GetComponent<Rigidbody>();
		rb.useGravity = false; // We’ll apply our own “gravity”

		// 1) Compute aim direction from screen center
		Camera cam = Camera.main;
		Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
		Vector3 shootDir = ray.direction.normalized;

		// 2) Set initial velocity
		velocity = shootDir * initialSpeed;

		// 3) Orient arrow (with model correction)
		transform.rotation = Quaternion.LookRotation(shootDir) * modelCorrection;

		// 4) Auto‑destroy to clean up
		Destroy(gameObject, lifeTime);

		Invoke(nameof(EnableTrailAfterDelay), 0.03f);
	}
	private void EnableTrailAfterDelay() => GetComponentInChildren<TrailRenderer>().enabled = true;

	void FixedUpdate()
	{
		// Move along our velocity vector
		Vector3 newPos = rb.position + velocity * Time.fixedDeltaTime;
		rb.MovePosition(newPos);

		// Apply downward “drop”
		velocity += Vector3.down * dropGravity * Time.fixedDeltaTime;

		// Rotate to follow the flight path (plus correction)
		if (velocity.sqrMagnitude > 0.001f)
		{
			Quaternion aimRot = Quaternion.LookRotation(velocity.normalized);
			rb.MoveRotation(aimRot * modelCorrection);
		}
	}
	//bool hasStuck = false;
	private void OnCollisionEnter(Collision collision)
	{
		//if (hasStuck) return;
		//hasStuck = true;

		if (collision.gameObject.TryGetComponent(out IDamagable component))
		{
			component.TakeDamage(arrowDamage);
			component.DamageEffects(initPlayerPos);
		}

		//var tr = GetComponentInChildren<TrailRenderer>();

		//float dist = Vector3.Distance(transform.position, FindFirstObjectByType<PlayerController>().transform.position);
		//float t = Mathf.Clamp01(dist / 350f);
		
		//tr.time = Mathf.Lerp(tr.time * 0.05f, tr.time, t);
	}
}

using System.Collections;
using UnityEngine;

public class Footsteps : MonoBehaviour
{
	public FootstepLibrary[] libraries;

	[System.Serializable]
	public class FootstepLibrary
	{
		public string libId = "";
		public Material[] materialsRecognized;
		public AudioClip[] footsteps;
	}

	private FootstepLibrary currLib;

	private Coroutine coroutine;
	public float timeBetweenSteps;
	public void PlayFootsteps()
	{
		if (coroutine != null)
			return;

		InvokeRepeating(nameof(CheckForMaterial), 0f, .2f);

		coroutine = StartCoroutine(Numerator());
	}
	public void StopFootsteps()
	{
		CancelInvoke();

		if (coroutine != null)
		{
			StopCoroutine(coroutine);
			coroutine = null;
		}
	}
	public void PlayOneOff()
	{
		PlayFootstep();
	}
	private void PlayFootstep()
	{
		if (currLib != null)
		{
			var audio = currLib.footsteps[Random.Range(0, currLib.footsteps.Length)];

			var a = new AudioToPlay();
			a.audioToPlay = audio;

			a.audioVolume = 1f;
			a.maxDistance = 500f;

			EventManager.instance.PlayThisSound(a);
			EventManager.instance.RandomizePitchOnSound(audio, .9f, 1.1f);
		}
	}

	private IEnumerator Numerator()
	{
		bool firstTime = true;

		while (true)
		{
			float timeToPlay = timeBetweenSteps;

			if (Input.GetKey(KeyCode.LeftShift))
				timeToPlay /= 1.3f;

			if (firstTime)
			{
				yield return new WaitForSeconds(timeToPlay / 2);
				firstTime = false;
			}
			else
				yield return new WaitForSeconds(timeToPlay);

			PlayFootstep();
		}
	}
	private void CheckForMaterial()
	{
		var player = FindFirstObjectByType<PlayerController>();

		Collider[] groundColliders = Physics.OverlapSphere(player.groundCheck.position, player.checkRadius + .1f, LayerMask.GetMask("Ground", "MovingPlatform"));

		if (groundColliders == null || groundColliders.Length == 0) return;

		Collider closestCollider = null;
		float closestDistance = float.MaxValue;

		foreach (Collider col in groundColliders)
		{
			// Find the closest point on the collider to groundCheck.position
			Vector3 closestPoint = col.bounds.ClosestPoint(player.groundCheck.position);
			float distance = Vector3.Distance(player.groundCheck.position, closestPoint);

			if (distance < closestDistance)
			{
				closestDistance = distance;
				closestCollider = col;

			}
		}

		if (closestCollider != null)
		{
			FootstepLibrary tempLib = null;

			foreach (var lib in libraries)
			{
				if (tempLib != null)
					break;

				foreach (var mat in lib.materialsRecognized)
				{
					if (closestCollider.gameObject.GetComponent<MeshRenderer>().material.name.Contains(mat.name))
					{
						tempLib = lib;
						break;
					}
				}
			}

			if (tempLib != null)
				if (currLib != tempLib)
					currLib = tempLib;
		}
	}
}

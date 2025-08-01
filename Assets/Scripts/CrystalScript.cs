using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class CrystalScript : MonoBehaviour
{
	[SerializeField] private GameObject crystalShield;
	[SerializeField] private float radius;
	public int maxHealth = 1000;
	[SerializeField] private MeshRenderer[] renderersToFlash;
	public int currentHealth;
	private List<(MeshRenderer renderer, Material[] originals)> affected =
		new List<(MeshRenderer renderer, Material[] originals)>();

	public bool thorns, deadly;

	private void Start()
	{
		crystalShield.SetActive(true);
		affected = new List<(MeshRenderer renderer, Material[] originals)>(renderersToFlash.Length);
		foreach (var rend in renderersToFlash)
			affected.Add((rend, rend.materials));
		currentHealth = maxHealth;
	}
	private void OnDrawGizmos()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(transform.position, radius);
	}
	public void TakeDamage(int dmg)
	{
		currentHealth -= dmg;
		StartCoroutine(DamageEffects());
		if (currentHealth <= 0)
			Die();
	}
	private void Die()
	{
		Debug.Log("Crystal has been destroyed!!");
		Destroy(this);
	}
	private Material flashMaterial;
	private float flashDuration = .1f;
	Material[] _originals;
	System.Collections.IEnumerator DamageEffects()
	{
		GetComponentInChildren<ParticleSystem>().Play();
		GetComponent<Animator>().SetTrigger("TakeDamage");

		// 1) Load flash material if not already loaded
		if (flashMaterial == null)
			flashMaterial = Resources.Load<Material>("Materials/FlashMaterial");

		// 3) Store originals and apply flash
		foreach (var rend in renderersToFlash)
		{
			// create an array filled with flashMaterial
			var flashMats = new Material[rend.materials.Length];
			for (int i = 0; i < flashMats.Length; i++)
				flashMats[i] = flashMaterial;

			// apply
			rend.materials = flashMats;
		}

		// 4) Wait
		yield return new WaitForSeconds(flashDuration);

		// 5) Revert all
		foreach (var (renderer, originals) in affected)
			renderer.materials = originals;
	}
	private void OnTriggerEnter(Collider other)
	{
		if (other.TryGetComponent(out AbstractEnemy enemy))
			enemy.StartAttack();
	}
	private void OnTriggerExit(Collider other)
	{
		if (other.TryGetComponent(out AbstractEnemy enemy))
			enemy.StopAttack();
	}
	public void Regeneration()
    {
		Invoke(nameof(Regeneration), .5f);
		currentHealth++;
		if (maxHealth < currentHealth)
			currentHealth = maxHealth;
    }
}

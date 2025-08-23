using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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

	[Header("UI")]
	[Tooltip("Slider showing health")]
	public Slider healthSlider;
	[Tooltip("Speed at which the slider value lerps")]
	[SerializeField] private float sliderLerpSpeed = 5f;

	private Material flashMaterial;
	private float flashDuration = .1f;

	public ParticleSystem explosionpfx;

	private void Start()
	{
		crystalShield.SetActive(true);
		affected = new List<(MeshRenderer renderer, Material[] originals)>(renderersToFlash.Length);
		foreach (var rend in renderersToFlash)
			affected.Add((rend, rend.materials));
		currentHealth = maxHealth;

		if (healthSlider != null)
		{
			healthSlider.maxValue = maxHealth;
			healthSlider.value = maxHealth;
		}
	}

	private void Update()
	{
		if (healthSlider != null)
		{
			float target = currentHealth;
			healthSlider.value = Mathf.Lerp(healthSlider.value, target, Time.deltaTime * sliderLerpSpeed);
		}
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
		explosionpfx.Play();
		explosionpfx.gameObject.GetComponent<AudioSource>().Play();
		FindFirstObjectByType<WaveController>().GetComponent<Animator>().SetTrigger("Lose");
		Destroy(gameObject);
	}

	private IEnumerator DamageEffects()
	{
		GetComponentInChildren<ParticleSystem>().Play();
		GetComponentInChildren<AudioSource>().Play();
		EventManager.instance.RandomizePitchOnSound(GetComponentInChildren<AudioSource>().clip, .6f, .9f);
		GetComponent<Animator>().SetTrigger("TakeDamage");

		if (flashMaterial == null)
			flashMaterial = Resources.Load<Material>("Materials/FlashMaterial");

		foreach (var rend in renderersToFlash)
		{
			var flashMats = new Material[rend.materials.Length];
			for (int i = 0; i < flashMats.Length; i++)
				flashMats[i] = flashMaterial;
			rend.materials = flashMats;
		}

		yield return new WaitForSeconds(flashDuration);

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
		if (currentHealth > maxHealth)
			currentHealth = maxHealth;
	}
}

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ExperienceEmitter : MonoBehaviour
{
	[SerializeField] private bool playOnStart = false;
	[SerializeField] private bool deparentOnEmit = false;

	[Space(5)]

	[SerializeField] private Gradient experienceGradientInspector;
	public static Gradient ExperienceGradient { get; private set; }

	private ParticleSystem pfxSys;
	private Collider localPlayerCollider;

	[Space(10)]

	[Range(1,1000)]
    [SerializeField] private int experiencePerParticle = 1;

	[Space(2)]

	[SerializeField] private float experiencePerSecond = 3;
    [SerializeField] private float experienceSeconds = 1;

	[Space(5)]

	[SerializeField, Tooltip("Computed: experiencePerSecond * experienceSeconds")]
	private float totalExperience;

	private static readonly List<ParticleSystem.Particle> _tmp = new();

	private Coroutine bindRoutine;

#if UNITY_EDITOR
	private void OnValidate()
	{
		if (Application.isPlaying) return;

		Initializers();
	}
#endif
	
	private void Awake()
	{
		Initializers();
	}
	private void Initializers()
	{
		totalExperience = (experiencePerSecond * experienceSeconds) - 1;

		if (pfxSys == null)
			pfxSys = GetComponent<ParticleSystem>();

		SyncStaticGradient();
		SyncParticleSystemValues();
		ConfigureTriggerModule();
	}
	private void OnEnable()
	{
		if (bindRoutine != null) StopCoroutine(bindRoutine);
		bindRoutine = StartCoroutine(BindLocalPlayerColliderRoutine());
	}

	private void OnDisable()
	{
		if (bindRoutine != null) StopCoroutine(bindRoutine);
		bindRoutine = null;
	}
	#region BindPlayerCollider
	private IEnumerator BindLocalPlayerColliderRoutine()
	{
		// Wait for NetworkManager + local player spawn
		while (true)
		{
			if (NetworkManager.Singleton != null &&
				NetworkManager.Singleton.IsListening &&
				NetworkManager.Singleton.LocalClient != null &&
				NetworkManager.Singleton.LocalClient.PlayerObject != null)
			{
				var playerGo = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;

				// Prefer a dedicated pickup collider if you have one, otherwise any collider.
				// Replace this with a specific component if needed.
				localPlayerCollider = playerGo.GetComponentInChildren<CapsuleCollider>(true);

				if (localPlayerCollider != null)
				{
					BindTriggerCollider(localPlayerCollider);
					yield break;
				}
			}

			yield return null;
		}
	}

	private void ConfigureTriggerModule()
	{
		var trigger = pfxSys.trigger;
		trigger.enabled = true;
		trigger.inside = ParticleSystemOverlapAction.Callback;
	}

	private void BindTriggerCollider(Collider col)
	{
		var trigger = pfxSys.trigger;
		trigger.enabled = true;

		// First time: add
		if (trigger.colliderCount == 0)
		{
			trigger.AddCollider(col);
			return;
		}

		// Later: replace index 0
		trigger.SetCollider(0, col);

		// Optional: if you ever added more than one, you can leave them or remove extras.
		// (Removing extras depends on Unity version API; many projects just keep one.)
	}

	#endregion

	private void Start()
	{
		if (playOnStart)
			StartEmit();
	}
	public void StartEmit()
    {
		var savedScale = transform.lossyScale;

		if (deparentOnEmit)
			transform.parent = null;

		transform.localScale = savedScale;

		pfxSys.Play();
    }
    

	private void OnParticleTrigger()
	{
		KillAndCollect(ParticleSystemTriggerEventType.Inside);
	}
	private void KillAndCollect(ParticleSystemTriggerEventType type)
	{
		int count = pfxSys.GetTriggerParticles(type, _tmp);
		if (count <= 0) return;

		// XP per particle collected
		PlayerManager.instance.playerExp.AddXp(count * experiencePerParticle);

		Debug.Log("Collected particle with value: " + experiencePerParticle);

		for (int i = 0; i < count; i++)
		{
			var p = _tmp[i];
			p.remainingLifetime = 0f;   // kill particle
			_tmp[i] = p;
		}

		pfxSys.SetTriggerParticles(type, _tmp);
		_tmp.Clear();
	}
	private void SyncStaticGradient()
	{
		if (experienceGradientInspector == null) return;

		if (ExperienceGradient == null)
			ExperienceGradient = new Gradient();

		// Deep-copy so edits don't depend on reference equality
		ExperienceGradient.SetKeys(
			experienceGradientInspector.colorKeys,
			experienceGradientInspector.alphaKeys
		);

		// Optional (depending on Unity version)
		ExperienceGradient.mode = experienceGradientInspector.mode;
	}
	private void SyncParticleSystemValues()
	{
		var main = pfxSys.main;
		var emission = pfxSys.emission;

		pfxSys.Stop();
		pfxSys.Clear();

		main.playOnAwake = false;
		main.loop = false;

		main.duration = experienceSeconds;

		// EXACT emission count over time
		emission.rateOverTime = experiencePerSecond;

		//int total = Mathf.Max(0, Mathf.RoundToInt(experiencePerSecond * experienceSeconds));
		//if (experiencePerSecond > 0f && total > 0)
		//{
		//	float interval = 1f / experiencePerSecond;

		//	// Create 1-particle bursts at 0, interval, 2*interval...
		//	var bursts = new ParticleSystem.Burst[total];
		//	for (int i = 0; i < total; i++)
		//		bursts[i] = new ParticleSystem.Burst(i * interval, 1);

		//	emission.SetBursts(bursts);
		//}

		// Color
		if (ExperienceGradient != null)
		{
			float t = Mathf.InverseLerp(1f, 1000f, experiencePerParticle);
			main.startColor = ExperienceGradient.Evaluate(t);
		}
	}

}

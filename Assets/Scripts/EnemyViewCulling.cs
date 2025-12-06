using Unity.Netcode;
using UnityEngine;

public class EnemyViewCulling : MonoBehaviour
{
	[SerializeField] private Renderer[] renderers;
	[SerializeField] private EnemyAnimatorLOD animatorLOD;
	[SerializeField] private Animator[] extraAnimators;        // optional
	[SerializeField] private MonoBehaviour[] extraClientLogic; // optional

	void Awake()
	{
		if (renderers == null || renderers.Length == 0)
			renderers = GetComponentsInChildren<Renderer>(includeInactive: true);

		if (!animatorLOD)
			animatorLOD = GetComponent<EnemyAnimatorLOD>();

		if (extraAnimators == null || extraAnimators.Length == 0)
			extraAnimators = GetComponentsInChildren<Animator>(includeInactive: true);
	}

	void OnBecameVisible()
	{
		SetClientActive(true);
	}

	void OnBecameInvisible()
	{
		SetClientActive(false);
	}

	void SetClientActive(bool active)
	{
		// visuals
		foreach (var r in renderers)
			if (r) r.enabled = active;

		// animator LOD
		if (animatorLOD) animatorLOD.enabled = active;

		// any extra animators
		foreach (var a in extraAnimators)
			if (a) a.enabled = active;

		// any extra purely-client scripts (VFX, audio, etc.)
		foreach (var s in extraClientLogic)
			if (s) s.enabled = active;
	}
}

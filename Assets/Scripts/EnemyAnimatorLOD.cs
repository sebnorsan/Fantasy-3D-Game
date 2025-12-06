using UnityEngine;

public class EnemyAnimatorLOD : MonoBehaviour
{
	public Animator blinkAnimator;
	public Animator enemyAnimator;

	public float spawnAnimationTime = 1.5f;

	[Header("Distance thresholds")]
	public float nearDist = 10f;
	public float midDist = 20f;
	public float farDist = 35f;

	[HideInInspector] public int framesPerUpdate = 1;

	bool fullyOff;
	int frameOffset;

	bool canUpdate = false;

	void Awake()
	{
		if (!enemyAnimator) enemyAnimator = GetComponent<Animator>();
		frameOffset = Random.Range(0, 10);

		Invoke(nameof(SetCanUpdate), spawnAnimationTime);
	}
	private void SetCanUpdate() => canUpdate = true;
	// Called by the manager, NOT every frame for every enemy
	public void UpdateLOD(Vector3 playerPos)
	{
		if (!enemyAnimator || !canUpdate) return;

		float d2 = (transform.position - playerPos).sqrMagnitude;
		float near2 = nearDist * nearDist;
		float mid2 = midDist * midDist;
		float far2 = farDist * farDist;

		if (d2 > far2)
		{
			fullyOff = true;
			enemyAnimator.enabled = false;
			return;
		}
		if (d2 > near2)
			blinkAnimator.enabled = false;
		fullyOff = false;
		enemyAnimator.enabled = true;

		if (d2 < near2) framesPerUpdate = 1;
		else if (d2 < mid2) framesPerUpdate = 2;
		else framesPerUpdate = 4;
	}

	void LateUpdate()
	{
		if (!enemyAnimator || fullyOff) return;

		if (framesPerUpdate <= 1)
		{
			enemyAnimator.enabled = true;
			blinkAnimator.enabled = true;
			return;
		}

		bool tickNow = ((Time.frameCount + frameOffset) % framesPerUpdate) == 0;
		enemyAnimator.enabled = tickNow;
	}
}

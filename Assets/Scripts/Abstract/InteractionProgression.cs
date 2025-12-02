using EZCameraShake;
using UnityEngine;

public abstract class InteractionProgression : MonoBehaviour
{
	public float timeTillCompletion = 1f;
	[HideInInspector] public bool progressionStarted = false;

	[Space(10)]

	public float mag;
	public float rough;

	CameraShakeInstance shake;

	public virtual void StartProgression()
	{
		if (shake == null)
			shake = CameraShaker.Instance.StartShake(mag, rough, timeTillCompletion);

		InteractionProgressBar.instance.StartAnimation(timeTillCompletion);
		Invoke(nameof(FinishAnimation), timeTillCompletion);
		progressionStarted = true;
	}
	public virtual void CancelProgression()
	{
		if (shake != null)
		{
			shake.StartFadeOut(1f);
			shake = null;
		}

		InteractionProgressBar.instance.StopAnimation();
		CancelInvoke();
		progressionStarted = false;
	}
	protected virtual void FinishAnimation()
	{
		CancelProgression();

		InteractionProgressBar.instance.anim.SetTrigger("Finish");
	}
	protected virtual void Update()
	{
		if (progressionStarted)
			if (Input.GetKeyUp(KeyCode.E))
				CancelProgression();
	}
}
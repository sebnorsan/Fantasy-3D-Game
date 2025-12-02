using UnityEngine;

public abstract class AbstractEvent : MonoBehaviour
{
	public bool debugOnly = false;

	[Space(10)]

	public EventActivation eventActivation;

	[Space(15)]

	public bool showGizmo = true;
	public Color gizmoColor = Color.white;
	protected virtual void Awake()
	{
		if (eventActivation == EventActivation.Remote)
			return;

		if (eventActivation == EventActivation.Awake)
			Invoke(nameof(CallEvent), .1f);
	}
	private void OnEnable()
	{
		if (eventActivation == EventActivation.Remote)
			return;

		if (eventActivation != EventActivation.Enable)
			return;

		CallEvent();
	}
	protected virtual void OnDrawGizmos()
	{
		if (eventActivation == EventActivation.Remote)
			return;

		if (!showGizmo)
			return;

		Gizmos.color = gizmoColor;

		if (GetComponent<BoxCollider>())
			Gizmos.DrawCube(transform.position, transform.localScale);
	}
	public abstract void CallEvent();

	private void OnTriggerEnter(Collider other)
	{
		if (eventActivation == EventActivation.Remote)
			return;

		if (eventActivation != EventActivation.Trigger || eventActivation == EventActivation.Collision)
			return;

		if (other.gameObject.CompareTag("Player"))
			CallEvent();
	}
	private void OnCollisionEnter(Collision collision)
	{
		if (eventActivation == EventActivation.Remote)
			return;

		if (eventActivation != EventActivation.Collision || eventActivation == EventActivation.Trigger)
			return;

		if (collision.gameObject.CompareTag("Player"))
			CallEvent();
	}
}
public enum EventActivation
{
	Trigger,
	Awake,
	Remote,
	Collision,
	Enable
}
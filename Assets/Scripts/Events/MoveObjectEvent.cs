using UnityEngine;
using System.Collections;

public class MoveObjectEvent : AbstractEvent
{
	[Space(15)]
	[Space(0)]
	public Vector3 positionToMoveTo;
	public Quaternion rotationToMoveTo = Quaternion.identity;

	private Vector3 startVector3;
	private Quaternion startQuaternion;

	public AnimationCurve positionCurve;
	public AnimationCurve rotationCurve;

	public float duration = 2f;

	public bool repeat;
	public float repeatPause = .5f;

	private void OnValidate()
	{
		int movingPlatform = LayerMask.NameToLayer("MovingPlatform");
		
		if (gameObject.layer != movingPlatform)
			gameObject.layer = movingPlatform;
		if (gameObject.isStatic)
			gameObject.isStatic = false;
	}
	private void Start()
	{
		showGizmo = false;

		startVector3 = gameObject.transform.position;
		startQuaternion = gameObject.transform.rotation;

		CallEvent();
	}
	protected override void OnDrawGizmos()
	{
		base.OnDrawGizmos();

		if (!showGizmo)
			return;

		MeshFilter meshFilter = GetComponent<MeshFilter>();
		if (meshFilter == null || meshFilter.sharedMesh == null)
			return;

		Vector3 targetPosition = transform.position + positionToMoveTo;
		Quaternion combinedRotation = transform.rotation * rotationToMoveTo;
		Vector3 scale = transform.localScale;

		Gizmos.color = gizmoColor;
		Gizmos.DrawMesh(meshFilter.sharedMesh, targetPosition, combinedRotation, scale);
	}
	public override void CallEvent()
	{
		StartCoroutine(ChangeBasedOnCurve(startVector3, transform.position + positionToMoveTo, startQuaternion, transform.rotation * rotationToMoveTo));
	}

	private IEnumerator ChangeBasedOnCurve(Vector3 startPos, Vector3 endPos, Quaternion startRot, Quaternion endRot)
	{
		float t = 0f;
		while (t < 1f)
		{
			t += Time.deltaTime / duration;
			float clampedT = Mathf.Clamp01(t);
			float curveTPos = positionCurve.Evaluate(clampedT);
			float curveTRot = rotationCurve.Evaluate(clampedT);
			gameObject.transform.position = Vector3.Lerp(startPos, endPos, curveTPos);
			gameObject.transform.rotation = Quaternion.Lerp(startRot, endRot, curveTRot);
			yield return null;
		}
		gameObject.transform.position = endPos;
		gameObject.transform.rotation = endRot;

		yield return new WaitForSeconds(repeatPause);

		if (repeat)
			StartCoroutine(ChangeBasedOnCurve(endPos, startPos, endRot, startRot));
	}
}

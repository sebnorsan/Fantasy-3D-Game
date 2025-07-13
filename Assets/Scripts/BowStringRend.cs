using NUnit.Framework;
using UnityEngine;

public class BowStringRend : MonoBehaviour
{
	[Header("References")]
	public Transform leftNock;    // Where the string attaches on the left
	public Transform rightNock;   // Where the string attaches on the right
	public Transform stringHandle; // The point the player pulls on

	[Space(20)]

	public Transform midParent;

	private LineRenderer lr;

	void Start()
	{
		lr = GetComponent<LineRenderer>();
		// We want three points: left end, middle (pulled) and right end
		lr.positionCount = 3;
	}

	void Update()
	{
		// 0: Left attachment
		lr.SetPosition(0, leftNock.localPosition);
		// 1: Pulled handle (you’ll move this transform via input/IK/etc)
		if (!midAssigned)
			lr.SetPosition(1, stringHandle.localPosition);
		else
		{
			var pos = new Vector3(midParent.localPosition.x, midParent.localPosition.y - 1.2f, midParent.localPosition.z);
			lr.SetPosition(1, pos);
		}
		// 2: Right attachment
		lr.SetPosition(2, rightNock.localPosition);

		// (Optional) If you want curvature rather than straight lines, 
		// you could subdivide into more points and calculate a Bezier:
		//Vector3 p0 = leftNock.position;
		//Vector3 p1 = stringHandle.position;
		//Vector3 p2 = rightNock.position;
		//for (int i = 0; i < lr.positionCount; i++)
		//{
		//	float t = i / (float)(lr.positionCount - 1);
		//	// Quadratic Bezier: B(t)= (1-t)^2 p0 + 2(1-t)t p1 + t^2 p2
		//	Vector3 point = (1 - t) * (1 - t) * p0
		//				  + 2 * (1 - t) * t * p1
		//				  + t * t * p2;
		//	lr.SetPosition(i, point);
		//}
	}
	private bool midAssigned = false;
	public void AssignMid()
	{
		midAssigned = true;
	}
	public void UnAssignMid()
	{
		midAssigned = false;
	}
}

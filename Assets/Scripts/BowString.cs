using UnityEngine;

public class BowString : MonoBehaviour
{
	public void AssignMiddleString() => GetComponentInChildren<BowStringRend>().AssignMid();
	public void UnAssignMiddleString() => GetComponentInChildren<BowStringRend>().UnAssignMid();
}

using UnityEngine;

public class QuestObjectCounter : MonoBehaviour
{
	public int countToReach;
	[TextArea]
	public string questDescription;
	public bool isActive = true;


	private void OnEnable()
	{
		Invoke(nameof(Function), 1f);
	}
	private void OnDisable()
	{
		CancelInvoke();
	}
	private void Function()
	{
		if (countToReach <= 0)
			countToReach = GetComponentsInChildren<QuestObject>().Length;

		isActive = false;
		GetComponentInChildren<QuestObject>().SetQuest();
		isActive = true;
	}
	public void RemoveCount()
	{
		countToReach--;
		if (countToReach <= 1)
			isActive = false;
	}
}

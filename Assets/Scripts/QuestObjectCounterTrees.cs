using EasyTextEffects.Editor.MyBoxCopy.Extensions;
using UnityEngine;

public class QuestObjectCounterTrees : MonoBehaviour
{
    public int countToReach;
    [TextArea]
    public string questDescription;
    public bool isActive = true;
	public enum QuestType
	{
		MainQuest,
		SideQuest
	}
	[SerializeField] private QuestType questType;

	public int xpGain = 100;

	public string actionToEnable = "";

	private void OnEnable()
	{
		Invoke(nameof(SetQuest), .1f);
	}
	private void OnDisable()
	{
		CancelInvoke();
	}
	
	public void RemoveCount()
	{
		countToReach--;
		if (countToReach <= 0)
			FinishQuest();
	}
	public void SetQuest()
	{
		FindFirstObjectByType<BowScript>().arrowCutsTrees = true;

		switch (questType)
		{
			case QuestType.MainQuest:
				QuestManager.instance.SetMainQuest(questDescription);
				break;
			case QuestType.SideQuest:
				QuestManager.instance.SetSideQuest(questDescription);
				break;
		}
	}
	public void FinishQuest()
	{
		FindFirstObjectByType<BowScript>().arrowCutsTrees = false;

		GameManager.instance.AddXp(xpGain);

		if (!actionToEnable.IsNullOrEmpty())
			DialogueManager.SetActive(actionToEnable, true);

		//foreach (var item in GameObject.FindGameObjectsWithTag("Tree"))
  //      {
		//	var ko = item.GetComponent<KillableObject>();
		//	if (ko != null)
		//		ko.TakeDamage(100, ko.transform.position);
  //      }

		switch (questType)
		{
			case QuestType.MainQuest:
				QuestManager.instance.FinishMainQuest();
				break;
			case QuestType.SideQuest:
				QuestManager.instance.FinishSideQuest();
				break;
		}
		Destroy(gameObject);
	}
}

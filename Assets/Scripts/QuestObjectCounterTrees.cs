using EasyTextEffects.Editor.MyBoxCopy.Extensions;
using Unity.Netcode;
using UnityEngine;

public class QuestObjectCounterTrees : NetworkBehaviour
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

	public override void OnNetworkSpawn()
	{
		if (NetworkManager.Singleton.IsServer)
			Invoke(nameof(SetQuest), .1f);
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
			DialogueManager.instance.SetActive(actionToEnable, true);

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

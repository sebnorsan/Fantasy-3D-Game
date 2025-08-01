using UnityEngine;

public class QuestObject : MonoBehaviour
{
	[TextArea]
	[SerializeField] private string questDesc;
    public enum QuestType
    {
        MainQuest,
        SideQuest
    }
    [SerializeField] private QuestType questType;
	private void OnEnable()
	{
		Invoke(nameof(SetQuest), .1f);
	}
	private void OnDisable()
	{
		CancelInvoke();
	}
	public void FinishQuest()
    {
		var questCounter = GetComponentInParent<QuestObjectCounter>();
		if (questCounter != null && questCounter.isActive)
		{
			questCounter.RemoveCount();
			return;
		}

		switch (questType)
		{
			case QuestType.MainQuest:
				QuestManager.instance.FinishMainQuest();
				break;
			case QuestType.SideQuest:
				QuestManager.instance.FinishSideQuest();
				break;
		}
	}
	public void SetQuest()
	{
		var questCounter = GetComponentInParent<QuestObjectCounter>();
		if (questCounter != null && questCounter.isActive)
			return;

		switch (questType)
		{
			case QuestType.MainQuest:
				QuestManager.instance.SetMainQuest(questDesc);
				break;
			case QuestType.SideQuest:
				QuestManager.instance.SetSideQuest(questDesc);
				break;
		}
	}
}

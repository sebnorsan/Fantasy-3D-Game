using EasyTextEffects.Editor.MyBoxCopy.Extensions;
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
    public QuestType questType;
	[SerializeField] private bool reenableKingQuest;
	[SerializeField] private bool setKingSideQuest;
	[SerializeField] ScriptableObject_NPC_Dialogue dia;

	public int xpGain = 100;

	public string actionToEnable = "";

	private void OnEnable()
	{
		Invoke(nameof(SetQuest), .3f);
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

		GameManager.instance.AddXp(xpGain);

		if (!actionToEnable.IsNullOrEmpty())
			DialogueManager.SetActive(actionToEnable, true);

		switch (questType)
		{
			case QuestType.MainQuest:
				QuestManager.instance.FinishMainQuest();
				break;
			case QuestType.SideQuest:
				QuestManager.instance.FinishSideQuest();
				break;
		}

		if (setKingSideQuest)
			FindFirstObjectByType<NPC_Interactable>().gameObject.GetComponent<QuestObject>().questType = QuestType.SideQuest;
		if (reenableKingQuest)
			FindFirstObjectByType<NPC_Interactable>().KingDialogue(dia);
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

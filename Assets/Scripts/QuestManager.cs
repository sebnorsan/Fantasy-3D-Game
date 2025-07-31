using TMPro;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager instance;

    [SerializeField] private GameObject mainQuestObj;
	[SerializeField] private TextMeshProUGUI mainQuestDescription;
    [SerializeField] private GameObject sideQuestObj;
	[SerializeField] private TextMeshProUGUI sideQuestDescription;

	private void Awake()
	{
		if (instance != null)
			Destroy(gameObject);
		else
			instance = this;
	}
	public void SetMainQuest(string desc)
	{
		mainQuestDescription.text = desc;
		SetAnimationTrigger(mainQuestObj, "Enter");
	}
	public void SetSideQuest(string desc)
	{
		sideQuestDescription.text = desc;
		SetAnimationTrigger(sideQuestObj, "Enter");
	}
	public void FinishMainQuest()
	{
		SetAnimationTrigger(mainQuestObj, "Finish");
	}
	public void FinishSideQuest()
	{
		SetAnimationTrigger(sideQuestObj, "Finish");
	}
	private void SetAnimationTrigger(GameObject obj, string trigger)
	{
		obj.GetComponent<Animator>().SetTrigger(trigger);
	}
}

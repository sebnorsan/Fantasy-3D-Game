using Unity.Netcode;
using UnityEngine;

public class QuestObjectCounter : MonoBehaviour
{
	public int countToReach;
	[TextArea]
	public string questDescription;
	public bool isActive = true;


	public QuestObject[] objectives;

	private void OnEnable()
	{
		Invoke(nameof(Function), 1f);
	}
	private void OnDisable()
	{
		CancelInvoke();
	}
	[SerializeField] private NetworkObject[] objectivePrefabs;

	private void Function()
	{
		if (!NetworkManager.Singleton.IsServer) return;

		if (countToReach <= 0)
			countToReach = objectivePrefabs.Length;

		isActive = false;

		for (int i = 0; i < objectivePrefabs.Length; i++)
		{
			var inst = Instantiate(objectivePrefabs[i], objectivePrefabs[i].transform.position, objectivePrefabs[i].transform.rotation);

			inst.GetComponent<QuestObject>().counter = this;

			inst.Spawn();
			objectives[i] = inst.GetComponent<QuestObject>();
		}

		objectives[0].SetQuest();
		isActive = true;
	}

	public void RemoveCount()
	{
		countToReach--;
		if (countToReach <= 1)
			isActive = false;
	}
}

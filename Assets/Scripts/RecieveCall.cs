using UnityEngine;

public class RecieveCall : MonoBehaviour
{
	[SerializeField] GameObject[] objToRemove;
	[SerializeField] private NPC_Interactable_Remote npcRemote;
	[SerializeField] private ScriptableObject_NPC_Dialogue dialogueToReplace;
	private void OnTriggerEnter(Collider other)
	{
		if (other.GetComponent<PlayerController>())
		{
			if (dialogueToReplace != null)
				npcRemote.npcOwner.ChangeDialogue(dialogueToReplace);

			npcRemote.CallUp();

			foreach (var obj in objToRemove)
				Destroy(obj);
		}
	}
}

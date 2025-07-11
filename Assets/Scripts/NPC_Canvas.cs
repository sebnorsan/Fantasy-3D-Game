using EasyTextEffects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NPC_Canvas : MonoBehaviour
{
	public static NPC_Canvas singleton;

	public TextEffect textEffects;
	public TextMeshProUGUI textMesh;
	public TextMeshProUGUI textMeshName;
	public Transform npcPicturesParent;

	private void Awake()
	{
		if (singleton != null)
			Destroy(gameObject);
		else
			singleton = this;

		DeactivateCanvas();
	}
	public void ActivateCanvas()
	{
		foreach (Transform c in transform)
			c.gameObject.SetActive(true);

		GetComponent<Animator>().SetTrigger("Enter");
	}
	public void DeactivateCanvas()
	{
		foreach (Transform c in transform)
			c.gameObject.SetActive(false);

		GetComponent<Animator>().SetTrigger("Exit");
	}
	public void SetDialogue(string npcName, string npcTalk, WordEffect[] wordEffects)
	{
		textMeshName.text = npcName;
		textMesh.text = npcTalk;

		ApplyWordEffects(wordEffects);
		textEffects.Refresh();
	}
	public void SetNpcPicture(Sprite picToEnable)
	{
		foreach (Transform c in npcPicturesParent)
		{
			c.gameObject.SetActive(false);

			if (c.gameObject.GetComponent<Image>().sprite == picToEnable)
				c.gameObject.SetActive(true);
		}
	}

	private void ApplyWordEffects(WordEffect[] effects)
	{
		foreach (var effect in effects)
		{
			// Preserve existing tag-based effects
			string tagsToAdd = "";
			switch (effect.wordEffectType)
			{
				case TextType.Normal:
					break;
				case TextType.Aggressive:
					tagsToAdd = "w2+c2+rotate+scale";
					break;
				case TextType.Whisper:
					break;
			}
			if (!string.IsNullOrEmpty(tagsToAdd))
			{
				tagsToAdd = "<link=" + tagsToAdd + ">";
				if (textMesh.text.Contains(effect.wordAffected))
				{
					textMesh.text = textMesh.text.Replace(
						effect.wordAffected,
						tagsToAdd + effect.wordAffected + "</link>"
					);
				}
			}
		}
	}
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyMemberRow : MonoBehaviour
{
	[SerializeField] private TMP_Text nameText;
	[SerializeField] private Image avatarImage;

	public void SetName(string n) => nameText.text = n;
	public void SetAvatar(Sprite s) => avatarImage.sprite = s;
}

using Steamworks.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class LobbyRowView : MonoBehaviour
{
	[SerializeField] private TMP_Text lobbyNameText;
	[SerializeField] private TMP_Text countText;
	[SerializeField] private Button joinButton;
	[SerializeField] private GameObject lockIcon;

	private Lobby boundLobby;
	private bool passworded;
	private Action<Lobby, bool> onJoin;

	public void Bind(Lobby lobby, string name, int members, int max, bool isPassworded, Action<Lobby, bool> onJoinPressed)
	{
		boundLobby = lobby;
		passworded = isPassworded;
		onJoin = onJoinPressed;

		lobbyNameText.text = name;
		countText.text = $"( {members} / {max} )";
		if (lockIcon != null) lockIcon.SetActive(passworded);

		joinButton.onClick.RemoveAllListeners();
		joinButton.onClick.AddListener(() => onJoin?.Invoke(boundLobby, passworded));
	}
}

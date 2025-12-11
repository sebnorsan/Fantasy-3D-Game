using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
	[SerializeField] private KeyCode toggleKey = KeyCode.Escape;

	[Header("Panels")]
	[SerializeField] private GameObject pauseRootPanel;  // whole pause/options menu
	[SerializeField] private GameObject settingsPanel;   // settings submenu only

	[Header("Buttons")]
	[SerializeField] private Button resumeButton;
	[SerializeField] private Button settingsButton;
	[SerializeField] private Button quitToLobbyButton;
	[SerializeField] private Button quitGameButton;
	[SerializeField] private Button settingsCloseButton;

	private bool isOpen = false;

	private void Awake()
	{
		// start closed
		if (pauseRootPanel != null)
			pauseRootPanel.SetActive(false);
		if (settingsPanel != null)
			settingsPanel.SetActive(false);

		if (resumeButton != null)
			resumeButton.onClick.AddListener(ResumeGame);

		if (settingsButton != null)
			settingsButton.onClick.AddListener(ToggleSettingsSubmenu);

		if (settingsCloseButton != null)
			settingsCloseButton.onClick.AddListener(ToggleSettingsSubmenu);

		if (quitToLobbyButton != null)
			quitToLobbyButton.onClick.AddListener(QuitToLobby);

		if (quitGameButton != null)
			quitGameButton.onClick.AddListener(QuitGame);
	}

	private void Update()
	{
		if (NetworkManager.Singleton == null) return;

		if (Input.GetKeyDown(toggleKey))
		{
			if (isOpen) ToggleClose();
			else ToggleOpen();
		}
	}

	private void ToggleSettingsSubmenu()
	{
		if (settingsPanel == null) return;
		settingsPanel.SetActive(!settingsPanel.activeSelf);
	}

	private void QuitToLobby()
	{
		if (GameNetworkManager.instance != null)
			GameNetworkManager.instance.LeaveGameAndReturnToMenu();
	}

	public void ResumeGame()
	{
		if (isOpen)
			ToggleClose();
	}

	public void QuitGame()
	{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
		Application.Quit();
#endif
	}

	private void ToggleOpen()
	{
		isOpen = true;

		if (pauseRootPanel != null)
			pauseRootPanel.SetActive(true);

		// we don't touch settingsPanel here (it remembers last state)

		if (EventManager.instance != null && EventManager.instance.IsCameraPlayerMode())
			EventManager.instance.ToggleCameraMode();
	}

	private void ToggleClose()
	{
		isOpen = false;

		if (pauseRootPanel != null)
			pauseRootPanel.SetActive(false);

		if (EventManager.instance != null && !EventManager.instance.IsCameraPlayerMode())
			EventManager.instance.ToggleCameraMode();
	}
}

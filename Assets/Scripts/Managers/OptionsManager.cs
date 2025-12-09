using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class OptionsManager : MonoBehaviour
{
	public static OptionsManager instance;

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

	#region OptionVariables
	[Space(10)]
	public float sensitivity = 2f;
	public float minSensitivity = 0;
	public float maxSensitivity = 5;
	[Space(5)]
	public float fov = 90;
	public float minFov = 50;
	public float maxFov = 120;
	[Space(5)]
	public bool toggleRun = false;
	#endregion

	#region OptionVariablesKeys
	private string K_sensitivity = "sens";
	private string K_fov = "fov";
	private string K_toggleRun = "tglrun";
	#endregion

	#region OptionObjects
	[Space(4)]
	[Header("Sensitivity")]
	[SerializeField] private Slider sens_slider;
	[SerializeField] private TMP_InputField sens_inputField;
	[Space(4)]
	[Header("Field of View")]
	[SerializeField] private Slider fov_slider;
	[SerializeField] private TMP_InputField fov_inputField;
	[Space(4)]
	[Header("Toggle run")]
	[SerializeField] private Toggle run_toggle;
	#endregion

	private bool isOpen = false;
	private PlayerController player;

	private void OnValidate()
	{
		if (!Application.isPlaying)
			SetMinMaxValues();
	}

	private void Awake()
	{
		if (instance != null)
		{
			Destroy(this);
			return;
		}
		instance = this;

		
	}
	private void Update()
	{
		if (!NetworkManager.Singleton) return;

		if (player == null)
		{
			var localClient = NetworkManager.Singleton.LocalClient;
			if (localClient != null && localClient.PlayerObject != null)
			{
				player = localClient.PlayerObject.GetComponent<PlayerReferences>().playerController;
				PlayerObjectFound();
			}
		}

		// Open / close pause menu with key
		if (Input.GetKeyDown(toggleKey))
		{
			if (isOpen) ToggleClose();
			else ToggleOpen();
		}
	}

	// --------- MENU BUTTON HANDLERS ---------

	private void ToggleSettingsSubmenu()
	{
		if (settingsPanel == null) return;
		// Just flip its activeSelf; we never touch it when closing the whole menu,
		// so its "open/closed" state is remembered.
		settingsPanel.SetActive(!settingsPanel.activeSelf);
	}

	private void QuitToLobby()
	{
		// Clean disconnect + back to lobby/main menu
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

	// --------- OPTIONS INIT / APPLY (unchanged logic) ---------
	#region InitialVariableApplies
	private void InitialApplies()
	{
		ApplySettingsSubscribes();
		ApplyButtonSubscribes();
		SetMinMaxValues();

		if (!OptionsPrefs.Has(K_sensitivity))
			OptionsPrefs.SetFloat(K_sensitivity, sensitivity);
		if (!OptionsPrefs.Has(K_fov))
			OptionsPrefs.SetFloat(K_fov, fov);
		if (!OptionsPrefs.Has(K_toggleRun))
			OptionsPrefs.SetBool(K_toggleRun, toggleRun);

		ApplyOptionValues();
	}
	private void ApplyButtonSubscribes()
	{
		// Pause menu closed by default
		if (pauseRootPanel != null)
			pauseRootPanel.SetActive(false);
		// Optional: start with settings submenu closed
		if (settingsPanel != null)
			settingsPanel.SetActive(false);

		// Wire up buttons like MainMenuManager
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
	private void ApplySettingsSubscribes()
	{
		sens_slider.onValueChanged.AddListener(ChangeSensitivitySlider);
		sens_inputField.onEndEdit.AddListener(ChangeSensitivityInputField);

		fov_slider.onValueChanged.AddListener(ChangeFovSlider);
		fov_inputField.onEndEdit.AddListener(ChangFovInputField);

		run_toggle.onValueChanged.AddListener(ToggleRun);
	}
	private void SetMinMaxValues()
	{
		sens_slider.minValue = minSensitivity;
		sens_slider.maxValue = maxSensitivity;

		fov_slider.minValue = minFov;
		fov_slider.maxValue = maxFov;
	}
	#endregion

	#region PlayerApplies
	private void PlayerObjectFound()
	{
		ApplyOptionValues();
		InitialApplies();
	}
	public void ApplyOptionValues()
	{
		sensitivity = OptionsPrefs.GetFloat(K_sensitivity);
		fov = OptionsPrefs.GetFloat(K_fov);
		toggleRun = OptionsPrefs.GetBool(K_toggleRun);

		ApplyToPlayer();
		ApplyVisualUpdates();
	}

	public void ApplyToPlayer()
	{
		ApplySens();
		ApplyFOV();
		ApplyRunToggle();
	}

	private void ApplySens() => player.lookSpeed = sensitivity;
	private void ApplyFOV() => player.ChangeFieldOfView(fov);
	private void ApplyRunToggle() => player.runToggle = toggleRun;
	#endregion

	#region DynamicOptionFunctions
	public void ChangeSensitivitySlider(float value)
	{
		OptionsPrefs.SetFloat(K_sensitivity, value);
		ApplyOptionValues();
	}
	public void ChangeSensitivityInputField(string text)
	{
		if (!float.TryParse(text, out float value))
		{
			sens_inputField.SetTextWithoutNotify($"{sensitivity}");
			return;
		}

		OptionsPrefs.SetFloat(K_sensitivity, CheckSensValue(value));
		ApplyOptionValues();
	}
	private float CheckSensValue(float value) => Mathf.Clamp(value, minSensitivity, maxSensitivity);

	public void ChangeFovSlider(float value)
	{
		OptionsPrefs.SetFloat(K_fov, value);
		ApplyOptionValues();
	}
	public void ChangFovInputField(string text)
	{
		if (!float.TryParse(text, out float value))
		{
			fov_inputField.SetTextWithoutNotify($"{fov}");
			return;
		}

		OptionsPrefs.SetFloat(K_fov, CheckFovValue(value));
		ApplyOptionValues();
	}
	private float CheckFovValue(float value) => Mathf.Clamp(value, minFov, maxFov);

	public void ToggleRun(bool isOn)
	{
		OptionsPrefs.SetBool(K_toggleRun, isOn);
		ApplyOptionValues();
	}
	#endregion

	#region VisualUpdates
	public void ApplyVisualUpdates()
	{
		SensitivityVisual();
		FovVisual();
		ToggleRunVisual();
	}
	private void SensitivityVisual()
	{
		sens_inputField.SetTextWithoutNotify(sensitivity.ToString("F2"));
		sens_slider.value = sensitivity;
	}
	private void FovVisual()
	{
		fov_inputField.SetTextWithoutNotify(fov.ToString("F0"));
		fov_slider.value = fov;
	}
	private void ToggleRunVisual()
	{
		run_toggle.SetIsOnWithoutNotify(toggleRun);
	}
	#endregion

	#region Open/Close/Toggle
	private void ToggleOpen()
	{
		isOpen = true;

		if (pauseRootPanel != null)
			pauseRootPanel.SetActive(true);

		// We do NOT touch settingsPanel here -> it remembers its last activeSelf state

		if (EventManager.instance.IsCameraPlayerMode())
			EventManager.instance.ToggleCameraMode();
	}

	private void ToggleClose()
	{
		isOpen = false;

		if (pauseRootPanel != null)
			pauseRootPanel.SetActive(false);

		// Still don't touch settingsPanel.activeSelf

		if (!EventManager.instance.IsCameraPlayerMode())
			EventManager.instance.ToggleCameraMode();
	}
	#endregion
}


public static class OptionsPrefs
{
	private const string KeyPrefix = "Options_";
	private static string FullKey(string optionName) => KeyPrefix + optionName;

	public static bool Has(string optionName) => PlayerPrefs.HasKey(FullKey(optionName));

	#region FLOATS
	public static float GetFloat(string optionName, float defaultValue = 0f)
		=> PlayerPrefs.GetFloat(FullKey(optionName), defaultValue);

	public static void SetFloat(string optionName, float value, bool saveNow = false)
	{
		PlayerPrefs.SetFloat(FullKey(optionName), value);
		if (saveNow) PlayerPrefs.Save();
	}
	#endregion
	#region INTEGERS
	public static int GetInt(string optionName, int defaultValue = 0)
		=> PlayerPrefs.GetInt(FullKey(optionName), defaultValue);

	public static void SetInt(string optionName, int value, bool saveNow = false)
	{
		PlayerPrefs.SetInt(FullKey(optionName), value);
		if (saveNow) PlayerPrefs.Save();
	}
	#endregion
	#region STRINGS
	public static string GetString(string optionName, string defaultValue = "")
		=> PlayerPrefs.GetString(FullKey(optionName), defaultValue);

	public static void SetString(string optionName, string value, bool saveNow = false)
	{
		PlayerPrefs.SetString(FullKey(optionName), value);
		if (saveNow) PlayerPrefs.Save();
	}
	#endregion
	#region BOOLEANS
	public static bool GetBool(string optionName, bool defaultValue = false)
		=> PlayerPrefs.GetInt(FullKey(optionName), defaultValue ? 1 : 0) != 0;

	public static void SetBool(string optionName, bool value, bool saveNow = false)
	{
		PlayerPrefs.SetInt(FullKey(optionName), value ? 1 : 0);
		if (saveNow) PlayerPrefs.Save();
	}
	#endregion
	#region UTILITY
	public static void Delete(string optionName)
		=> PlayerPrefs.DeleteKey(FullKey(optionName));

	public static void Save()
		=> PlayerPrefs.Save();
	#endregion
}
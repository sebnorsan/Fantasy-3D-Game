using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class OptionsManager : MonoBehaviour
{
	public static OptionsManager instance;

	[Header("Tabs")]
	[SerializeField] private GameObject gameSettingsPanel;   // contains sens/FOV/run etc.
	[SerializeField] private GameObject videoSettingsPanel;  // your video UI
	[SerializeField] private GameObject audioSettingsPanel;  // your audio UI
	
	[Space(5)]

	[SerializeField] private Button gameTabButton;
	[SerializeField] private Button videoTabButton;
	[SerializeField] private Button audioTabButton;

	[Space(5)]

	[SerializeField] private AudioMixer masterMixer; // All three groups live in this one mixer asset
	[SerializeField] private AudioMixer musicMixer;  // Music subgroup
	[SerializeField] private AudioMixer sfxMixer;    // SFX subgroup

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

	[Space(5)]

	public float masterVol = 1f;
	public float musicVol = 1f;
	public float sfxVol = 1f;

	[Space(5)]

	#endregion

	#region OptionVariablesKeys
	private string K_sensitivity = "sens";
	private string K_fov = "fov";
	private string K_toggleRun = "tglrun";

	private string K_masterVol = "mastervolume";
	private string K_musicVol = "musicvolume";
	private string K_sfxVol = "sfxvolume";
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

		InitialApplies();  // always init UI + prefs, even in main menu
		InitTabs();
	}

	private void Update()
	{
		// In menu scenes without NGO, just skip the player lookup,
		// but the UI + prefs still work.
		if (NetworkManager.Singleton == null) return;

		if (player == null)
		{
			var localClient = NetworkManager.Singleton.LocalClient;
			if (localClient != null && localClient.PlayerObject != null)
			{
				player = localClient.PlayerObject.GetComponent<PlayerReferences>().playerController;
				PlayerObjectFound();
			}
		}
	}


	#region Tabs
	private void InitTabs()
	{
		// Button listeners
		if (gameTabButton != null)
			gameTabButton.onClick.AddListener(ShowGameTab);

		if (videoTabButton != null)
			videoTabButton.onClick.AddListener(ShowVideoTab);

		if (audioTabButton != null)
			audioTabButton.onClick.AddListener(ShowAudioTab);

		// Default tab = Game
		ShowGameTab();
	}

	private void ShowGameTab()
	{
		SetTabActive(gameSettingsPanel);
	}

	private void ShowVideoTab()
	{
		SetTabActive(videoSettingsPanel);
	}

	private void ShowAudioTab()
	{
		SetTabActive(audioSettingsPanel);
	}

	private void SetTabActive(GameObject active)
	{
		SetTabInactive();

		if (active != null) active.SetActive(true);
	}
	private void SetTabInactive()
	{
		audioSettingsPanel.SetActive(false);
		gameSettingsPanel.SetActive(false);
		videoSettingsPanel.SetActive(false);
	}
	#endregion

	// --------- OPTIONS INIT / APPLY ---------
	#region InitialVariableApplies
	private void InitialApplies()
	{
		ApplySettingsSubscribes();
		SetMinMaxValues();

		if (!OptionsPrefs.Has(K_sensitivity))
			OptionsPrefs.SetFloat(K_sensitivity, sensitivity);
		if (!OptionsPrefs.Has(K_fov))
			OptionsPrefs.SetFloat(K_fov, fov);
		if (!OptionsPrefs.Has(K_toggleRun))
			OptionsPrefs.SetBool(K_toggleRun, toggleRun);

		if (!OptionsPrefs.Has(K_masterVol))
			OptionsPrefs.SetFloat(K_masterVol, masterVol);
		if (!OptionsPrefs.Has(K_musicVol))
			OptionsPrefs.SetFloat(K_musicVol, musicVol);
		if (!OptionsPrefs.Has(K_sfxVol))
			OptionsPrefs.SetFloat(K_sfxVol, sfxVol);

		ApplyOptionValues();
	}

	private void ApplySettingsSubscribes()
	{
		if (sens_slider != null)
			sens_slider.onValueChanged.AddListener(ChangeSensitivitySlider);
		if (sens_inputField != null)
			sens_inputField.onEndEdit.AddListener(ChangeSensitivityInputField);

		if (fov_slider != null)
			fov_slider.onValueChanged.AddListener(ChangeFovSlider);
		if (fov_inputField != null)
			fov_inputField.onEndEdit.AddListener(ChangFovInputField);

		if (run_toggle != null)
			run_toggle.onValueChanged.AddListener(ToggleRun);
	}

	private void SetMinMaxValues()
	{
		if (sens_slider != null)
		{
			sens_slider.minValue = minSensitivity;
			sens_slider.maxValue = maxSensitivity;
		}

		if (fov_slider != null)
		{
			fov_slider.minValue = minFov;
			fov_slider.maxValue = maxFov;
		}
	}
	#endregion

	#region PlayerApplies
	private void PlayerObjectFound()
	{
		// player just appeared (game scene) -> apply current prefs to them
		ApplyToPlayer();
	}

	public void ApplyOptionValues()
	{
		sensitivity = OptionsPrefs.GetFloat(K_sensitivity);
		fov = OptionsPrefs.GetFloat(K_fov);
		toggleRun = OptionsPrefs.GetBool(K_toggleRun);

		masterVol = OptionsPrefs.GetFloat(K_masterVol);
		musicVol = OptionsPrefs.GetFloat(K_musicVol);
		sfxVol = OptionsPrefs.GetFloat(K_sfxVol);

		if (masterVol == 0 || musicVol == 0 || sfxVol == 0)
		{
			masterVol = 1f;
			musicVol = 0f;
			sfxVol = 0f;

			//Summon sound settings panel, like in ultrakill
		}

		ApplyToPlayer();
		ApplyVisualUpdates();
		ApplyVolumes();
	}

	public void ApplyToPlayer()
	{
		if (player == null) return;

		ApplySens();
		ApplyFOV();
		ApplyRunToggle();
	}

	private void ApplySens() => player.lookSpeed = sensitivity;
	private void ApplyFOV() => player.ChangeFieldOfView(fov);
	private void ApplyRunToggle() => player.runToggle = toggleRun;
	#endregion
	#region Applies
	void ApplyVolumes()
	{
		float musicEffT = masterVol * musicVol;
		float sfxEffT = masterVol * sfxVol;

		float masterDb = TtoDb(masterVol);
		float musicDb = TtoDb(musicEffT);
		float sfxDb = TtoDb(sfxEffT);

		masterMixer.SetFloat("volume", masterDb);
		musicMixer.SetFloat("volume", musicDb);
		sfxMixer.SetFloat("volume", sfxDb);
	}
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
			if (sens_inputField != null)
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
			if (fov_inputField != null)
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

	private float SliderToT(float sliderValue) => Mathf.InverseLerp(-10f, 0f, sliderValue);
	private float TtoDb(float t)
	{
		float curved = 1f - Mathf.Pow(1f - t, 4f);
		return Mathf.Lerp(-80f, 0f, curved);
	}
	public void OnMasterSliderChanged(float sliderValue)
	{
		masterVol = SliderToT(sliderValue);
		OptionsPrefs.SetFloat(K_masterVol, masterVol);
		ApplyVolumes();
	}

	public void OnMusicSliderChanged(float sliderValue)
	{
		musicVol = SliderToT(sliderValue);
		OptionsPrefs.SetFloat(K_musicVol, musicVol);
		ApplyVolumes();
	}

	public void OnSfxSliderChanged(float sliderValue)
	{
		sfxVol = SliderToT(sliderValue);
		OptionsPrefs.SetFloat(K_sfxVol, sfxVol);
		ApplyVolumes();
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
		if (sens_inputField != null)
			sens_inputField.SetTextWithoutNotify(sensitivity.ToString("F2"));
		if (sens_slider != null)
			sens_slider.value = sensitivity;
	}

	private void FovVisual()
	{
		if (fov_inputField != null)
			fov_inputField.SetTextWithoutNotify(fov.ToString("F0"));
		if (fov_slider != null)
			fov_slider.value = fov;
	}

	private void ToggleRunVisual()
	{
		if (run_toggle != null)
			run_toggle.SetIsOnWithoutNotify(toggleRun);
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
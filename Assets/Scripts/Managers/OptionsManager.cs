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

	public float masterVol = 0f;
	public float musicVol = -10f;
	public float sfxVol = -10f;

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

	[Space(10)]
	[Header("Audio - Master")]
	[SerializeField] private Slider master_slider;
	[SerializeField] private TMP_InputField master_inputField;

	[Space(4)]
	[Header("Audio - Music")]
	[SerializeField] private Slider music_slider;
	[SerializeField] private TMP_InputField music_inputField;

	[Space(4)]
	[Header("Audio - SFX")]
	[SerializeField] private Slider sfx_slider;
	[SerializeField] private TMP_InputField sfx_inputField;
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
	}
	private void Start()
	{
		InitialApplies();
		InitTabs();
	}
	public void ReSetupAudioUI()
	{
		SetMinMaxValues();
		ApplyOptionValues();
	}

	private void Update()
	{
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
		if (audioSettingsPanel != null)
			audioSettingsPanel.SetActive(false);
		if (gameSettingsPanel != null)
			gameSettingsPanel.SetActive(false);
		if (videoSettingsPanel != null)
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

		if (master_slider != null)
			master_slider.onValueChanged.AddListener(OnMasterSliderChanged);
		if (master_inputField != null)
			master_inputField.onEndEdit.AddListener(ChangeMasterInputField);

		if (music_slider != null)
			music_slider.onValueChanged.AddListener(OnMusicSliderChanged);
		if (music_inputField != null)
			music_inputField.onEndEdit.AddListener(ChangeMusicInputField);

		if (sfx_slider != null)
			sfx_slider.onValueChanged.AddListener(OnSfxSliderChanged);
		if (sfx_inputField != null)
			sfx_inputField.onEndEdit.AddListener(ChangeSfxInputField);
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

		if (master_slider != null)
		{
			master_slider.minValue = -10f;
			master_slider.maxValue = 0f;
		}
		if (music_slider != null)
		{
			music_slider.minValue = -10f;
			music_slider.maxValue = 0f;
		}
		if (sfx_slider != null)
		{
			sfx_slider.minValue = -10f;
			sfx_slider.maxValue = 0f;
		}
	}
	#endregion

	#region PlayerApplies
	private void PlayerObjectFound()
	{
		ApplyToPlayer();
	}

	public void ApplyOptionValues()
	{
		sensitivity = OptionsPrefs.GetFloat(K_sensitivity);
		fov = OptionsPrefs.GetFloat(K_fov);
		toggleRun = OptionsPrefs.GetBool(K_toggleRun);

		masterVol = OptionsPrefs.GetFloat(K_masterVol, 0f);
		musicVol = OptionsPrefs.GetFloat(K_musicVol, -10f);
		sfxVol = OptionsPrefs.GetFloat(K_sfxVol, -10f);

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
		float masterT = DbLikeToT(masterVol);
		float musicT = DbLikeToT(musicVol);
		float sfxT = DbLikeToT(sfxVol);

		float musicEffT = masterT * musicT;
		float sfxEffT = masterT * sfxT;

		float masterDb = TtoDb(masterT);
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

	private float TtoDb(float t)
	{
		float curved = 1f - Mathf.Pow(1f - t, 4f);
		return Mathf.Lerp(-80f, 0f, curved);
	}
	public void OnMasterSliderChanged(float sliderValue)
	{
		masterVol = ClampDbLike(sliderValue);
		OptionsPrefs.SetFloat(K_masterVol, masterVol);
		MasterVolumeVisual();
		ApplyVolumes();
	}

	public void OnMusicSliderChanged(float sliderValue)
	{
		musicVol = ClampDbLike(sliderValue);
		OptionsPrefs.SetFloat(K_musicVol, musicVol);
		MusicVolumeVisual();
		ApplyVolumes();
	}

	public void OnSfxSliderChanged(float sliderValue)
	{
		sfxVol = ClampDbLike(sliderValue);
		OptionsPrefs.SetFloat(K_sfxVol, sfxVol);
		SfxVolumeVisual();
		ApplyVolumes();
	}

	public void ChangeMasterInputField(string text)
	{
		if (!float.TryParse(text, out float percent))
		{
			MasterVolumeVisual();
			return;
		}

		percent = Mathf.Clamp(percent, 0f, 100f);

		masterVol = PercentToDbLike(percent);          // <- REAL value -10..0
		OptionsPrefs.SetFloat(K_masterVol, masterVol);

		if (master_slider != null)
			master_slider.SetValueWithoutNotify(masterVol);

		MasterVolumeVisual();
		ApplyVolumes();
	}

	public void ChangeMusicInputField(string text)
	{
		if (!float.TryParse(text, out float percent))
		{
			MusicVolumeVisual();
			return;
		}

		percent = Mathf.Clamp(percent, 0f, 100f);

		musicVol = PercentToDbLike(percent);     // -10..0
		OptionsPrefs.SetFloat(K_musicVol, musicVol);

		if (music_slider != null)
			music_slider.SetValueWithoutNotify(musicVol);

		MusicVolumeVisual();
		ApplyVolumes();
	}

	public void ChangeSfxInputField(string text)
	{
		if (!float.TryParse(text, out float percent))
		{
			SfxVolumeVisual();
			return;
		}

		percent = Mathf.Clamp(percent, 0f, 100f);

		sfxVol = PercentToDbLike(percent);       // -10..0
		OptionsPrefs.SetFloat(K_sfxVol, sfxVol);

		if (sfx_slider != null)
			sfx_slider.SetValueWithoutNotify(sfxVol);

		SfxVolumeVisual();
		ApplyVolumes();
	}


	#endregion

	#region VisualUpdates
	public void ApplyVisualUpdates()
	{
		SensitivityVisual();
		FovVisual();
		ToggleRunVisual();

		MasterVolumeVisual();
		MusicVolumeVisual();
		SfxVolumeVisual();
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

	private float ClampDbLike(float v) => Mathf.Clamp(v, -10f, 0f);
	private float DbLikeToT(float dbLike) => Mathf.InverseLerp(-10f, 0f, dbLike);
	private float DbLikeToPercent(float dbLike) => DbLikeToT(dbLike) * 100f;
	private float PercentToDbLike(float percent) => Mathf.Lerp(-10f, 0f, Mathf.Clamp01(percent / 100f));
	private void MasterVolumeVisual()
	{
		if (master_slider != null)
			master_slider.SetValueWithoutNotify(masterVol);

		if (master_inputField != null)
			master_inputField.SetTextWithoutNotify(
				Mathf.RoundToInt(DbLikeToPercent(masterVol)).ToString()
			);
	}

	private void MusicVolumeVisual()
	{
		if (music_slider != null)
			music_slider.SetValueWithoutNotify(musicVol);

		if (music_inputField != null)
			music_inputField.SetTextWithoutNotify(
				Mathf.RoundToInt(DbLikeToPercent(musicVol)).ToString()
			);
	}

	private void SfxVolumeVisual()
	{
		if (sfx_slider != null)
			sfx_slider.SetValueWithoutNotify(sfxVol);

		if (sfx_inputField != null)
			sfx_inputField.SetTextWithoutNotify(
				Mathf.RoundToInt(DbLikeToPercent(sfxVol)).ToString()
			);
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
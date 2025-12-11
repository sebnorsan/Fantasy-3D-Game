using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class OptionsManager : MonoBehaviour
{
	public static OptionsManager instance;

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

		ApplyToPlayer();
		ApplyVisualUpdates();
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
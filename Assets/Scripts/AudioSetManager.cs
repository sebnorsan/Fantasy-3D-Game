using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AudioSetManager : MonoBehaviour
{
	private const string K_AudioSetupDone = "audio_setup_done";

	[Header("Panel")]
	[SerializeField] private GameObject audioSetupPanel;     // root panel
	[SerializeField] private CanvasGroup canvasGroup;        // on same panel
	[SerializeField] private Button finishButton;

	[Header("Master Volume")]
	[SerializeField] private Slider masterSlider;
	[SerializeField] private TMP_InputField masterInputField;

	[Header("Music Volume")]
	[SerializeField] private Slider musicSlider;
	[SerializeField] private TMP_InputField musicInputField;

	[Header("SFX Volume")]
	[SerializeField] private Slider sfxSlider;
	[SerializeField] private TMP_InputField sfxInputField;

	[Header("Refs")]
	[SerializeField] private OptionsManager optionsManager;
	[SerializeField] private bool forceShowInEditor = false;

	[SerializeField] private float fadeDuration = 0.5f;

	private const float MIN_DB = -10f;
	private const float MAX_DB = 0f;

	private void Start()
	{
		// fallback assignments
		if (audioSetupPanel == null)
			audioSetupPanel = gameObject;

		if (canvasGroup == null && audioSetupPanel != null)
			canvasGroup = audioSetupPanel.GetComponent<CanvasGroup>();

		if (optionsManager == null)
			optionsManager = OptionsManager.instance;

		// ALWAYS apply saved options at boot (even if panel won't show)
		if (optionsManager != null)
			optionsManager.ApplyOptionValues();

		bool done = OptionsPrefs.GetBool(K_AudioSetupDone, false);

#if UNITY_EDITOR
		if (forceShowInEditor)
			done = false;
#endif

		if (done)
		{
			if (audioSetupPanel != null)
				Destroy(audioSetupPanel);
			Destroy(this);
			return;
		}

		// first-time setup
		if (audioSetupPanel != null)
			audioSetupPanel.SetActive(true);

		if (canvasGroup != null)
		{
			canvasGroup.alpha = 1f;
			canvasGroup.interactable = true;
			canvasGroup.blocksRaycasts = true;
		}

		if (optionsManager != null)
			optionsManager.ApplyOptionValues();

		ApplyFirstTimeDefaults();
		SetupVolumeUI();
		SetupListeners();

	}
	private void ApplyFirstTimeDefaults()
	{
		if (optionsManager == null) return;

		// backend values (-10..0)
		optionsManager.OnMasterSliderChanged(0f);    // 100%
		optionsManager.OnMusicSliderChanged(-10f);   // 0%
		optionsManager.OnSfxSliderChanged(-10f);     // 0%
	}

	private void SetupVolumeUI()
	{
		if (optionsManager == null) return;

		// backend values are -10..0
		float masterDb = optionsManager.masterVol;
		float musicDb = optionsManager.musicVol;
		float sfxDb = optionsManager.sfxVol;

		if (masterSlider != null)
		{
			masterSlider.minValue = MIN_DB;
			masterSlider.maxValue = MAX_DB;
			masterSlider.SetValueWithoutNotify(masterDb);
		}
		if (musicSlider != null)
		{
			musicSlider.minValue = MIN_DB;
			musicSlider.maxValue = MAX_DB;
			musicSlider.SetValueWithoutNotify(musicDb);
		}
		if (sfxSlider != null)
		{
			sfxSlider.minValue = MIN_DB;
			sfxSlider.maxValue = MAX_DB;
			sfxSlider.SetValueWithoutNotify(sfxDb);
		}

		if (masterInputField != null)
		{
			masterInputField.contentType = TMP_InputField.ContentType.IntegerNumber;
			masterInputField.SetTextWithoutNotify(DbToPercentInt(masterDb).ToString());
		}
		if (musicInputField != null)
		{
			musicInputField.contentType = TMP_InputField.ContentType.IntegerNumber;
			musicInputField.SetTextWithoutNotify(DbToPercentInt(musicDb).ToString());
		}
		if (sfxInputField != null)
		{
			sfxInputField.contentType = TMP_InputField.ContentType.IntegerNumber;
			sfxInputField.SetTextWithoutNotify(DbToPercentInt(sfxDb).ToString());
		}
	}

	private void SetupListeners()
	{
		// Sliders -> call OptionsManager (backend), then update THIS inputfield (0..100)
		if (masterSlider != null) masterSlider.onValueChanged.AddListener(OnMasterSliderLocal);
		if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicSliderLocal);
		if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxSliderLocal);

		// Inputfields -> parse 0..100, convert to -10..0, set slider, then call OptionsManager
		if (masterInputField != null) masterInputField.onEndEdit.AddListener(_ => OnMasterInputLocal());
		if (musicInputField != null) musicInputField.onEndEdit.AddListener(_ => OnMusicInputLocal());
		if (sfxInputField != null) sfxInputField.onEndEdit.AddListener(_ => OnSfxInputLocal());

		if (finishButton != null)
			finishButton.onClick.AddListener(OnConfirmAudioSettings);
	}

	// ---------- Local wrappers ----------

	private void OnMasterSliderLocal(float db)
	{
		db = Mathf.Clamp(db, MIN_DB, MAX_DB);
		optionsManager?.OnMasterSliderChanged(db);

		if (masterInputField != null)
			masterInputField.SetTextWithoutNotify(DbToPercentInt(db).ToString());
	}

	private void OnMusicSliderLocal(float db)
	{
		db = Mathf.Clamp(db, MIN_DB, MAX_DB);
		optionsManager?.OnMusicSliderChanged(db);

		if (musicInputField != null)
			musicInputField.SetTextWithoutNotify(DbToPercentInt(db).ToString());
	}

	private void OnSfxSliderLocal(float db)
	{
		db = Mathf.Clamp(db, MIN_DB, MAX_DB);
		optionsManager?.OnSfxSliderChanged(db);

		if (sfxInputField != null)
			sfxInputField.SetTextWithoutNotify(DbToPercentInt(db).ToString());
	}

	private void OnMasterInputLocal()
	{
		int pct = ParsePercent(masterInputField);
		float db = PercentToDb(pct);

		if (masterSlider != null) masterSlider.SetValueWithoutNotify(db);
		optionsManager?.OnMasterSliderChanged(db);

		masterInputField.SetTextWithoutNotify(pct.ToString());
	}

	private void OnMusicInputLocal()
	{
		int pct = ParsePercent(musicInputField);
		float db = PercentToDb(pct);

		if (musicSlider != null) musicSlider.SetValueWithoutNotify(db);
		optionsManager?.OnMusicSliderChanged(db);

		musicInputField.SetTextWithoutNotify(pct.ToString());
	}

	private void OnSfxInputLocal()
	{
		int pct = ParsePercent(sfxInputField);
		float db = PercentToDb(pct);

		if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(db);
		optionsManager?.OnSfxSliderChanged(db);

		sfxInputField.SetTextWithoutNotify(pct.ToString());
	}

	// ---------- Mapping ----------

	private int DbToPercentInt(float db)
	{
		float t = Mathf.InverseLerp(MIN_DB, MAX_DB, db);
		return Mathf.Clamp(Mathf.RoundToInt(t * 100f), 0, 100);
	}

	private float PercentToDb(int percent)
	{
		float t = Mathf.Clamp01(percent / 100f);
		return Mathf.Lerp(MIN_DB, MAX_DB, t);
	}

	private int ParsePercent(TMP_InputField field)
	{
		if (field == null) return 0;
		if (!int.TryParse(field.text, out int pct)) pct = 0;
		return Mathf.Clamp(pct, 0, 100);
	}

	// Hooked to Finish button
	public void OnConfirmAudioSettings()
	{
		OptionsPrefs.SetBool(K_AudioSetupDone, true, true); // already saves the bool

		// also flush floats now
		OptionsPrefs.Save();

		if (optionsManager != null)
			optionsManager.ApplyOptionValues(); // re-apply mixers + visuals from prefs

		// fade/destroy...
		if (canvasGroup != null && audioSetupPanel != null)
			StartCoroutine(FadeOutAndDestroy());
		else
		{
			if (audioSetupPanel != null) Destroy(audioSetupPanel);
			Destroy(this);
		}
	}



	private IEnumerator FadeOutAndDestroy()
	{
		float t = 0f;
		float duration = Mathf.Max(0.01f, fadeDuration);

		canvasGroup.interactable = false;
		canvasGroup.blocksRaycasts = false;

		while (t < duration)
		{
			t += Time.unscaledDeltaTime;
			float lerp = t / duration;
			canvasGroup.alpha = Mathf.Lerp(1f, 0f, lerp);
			yield return null;
		}

		canvasGroup.alpha = 0f;

		if (audioSetupPanel != null)
			Destroy(audioSetupPanel);

		Destroy(this);
	}
}

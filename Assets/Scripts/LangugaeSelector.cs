using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LanguageSelector : MonoBehaviour
{
    [Header("UI References")]
    public Toggle englishToggle;
    public Toggle spanishToggle;
    public TextMeshProUGUI statusText;

    public enum Language
    {
        English,
        Spanish
    }

    public Language CurrentLanguage { get; private set; } = Language.English;

    private void Start()
    {
        // Connect UI events
        englishToggle.onValueChanged.AddListener(OnEnglishToggleChanged);
        spanishToggle.onValueChanged.AddListener(OnSpanishToggleChanged);

        UpdateStatusLabel();
    }

    private void OnDestroy()
    {
        // Clean up listeners (good practice)
        englishToggle.onValueChanged.RemoveListener(OnEnglishToggleChanged);
        spanishToggle.onValueChanged.RemoveListener(OnSpanishToggleChanged);
    }

    private void OnEnglishToggleChanged(bool isOn)
    {
        if (isOn)
        {
            SetLanguage(Language.English);
        }
    }

    private void OnSpanishToggleChanged(bool isOn)
    {
        if (isOn)
        {
            SetLanguage(Language.Spanish);
        }
    }

    private void SetLanguage(Language lang)
    {
        CurrentLanguage = lang;
        UpdateStatusLabel();

        // Hook into your own logic here, e.g.:
        // LocalizationManager.Instance.SetLanguage(lang.ToString());
        Debug.Log($"[LanguageSelector] Language changed to: {lang}");
    }

    private void UpdateStatusLabel()
    {
        if (statusText != null)
        {
            statusText.text = $"Current language: {CurrentLanguage}";
        }
    }
}

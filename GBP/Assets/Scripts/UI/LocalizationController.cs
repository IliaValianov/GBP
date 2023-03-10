using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class LocalizationController : MonoBehaviour
{
	public Transform container;
	public GameObject languageTogglePrefab;

	private AsyncOperationHandle m_InitializeOperation;
	private Dictionary<Locale, Toggle> m_Toggles = new Dictionary<Locale, Toggle>();
	private ToggleGroup m_ToggleGroup;

	private void Start()
	{
		// SelectedLocaleAsync will ensure that the locales have been initialized and a locale has been selected.
		m_InitializeOperation = LocalizationSettings.SelectedLocaleAsync;
		if (m_InitializeOperation.IsDone)
		{
			InitializeCompleted(m_InitializeOperation);
		}
		else
		{
			m_InitializeOperation.Completed += InitializeCompleted;
		}
	}

	private void InitializeCompleted(AsyncOperationHandle obj)
	{
		LocalizationSettings.SelectedLocaleChanged += LocalizationSettings_SelectedLocaleChanged;

		// The toggle group will ensure that only 1 language is selected at a time.
		m_ToggleGroup = container.gameObject.AddComponent<ToggleGroup>();

		// Create an option in the dropdown for each Locale
		var locales = LocalizationSettings.AvailableLocales.Locales;
		for (int i = 0; i < locales.Count; ++i)
		{
			var locale = locales[i];

			var languageToggle = Instantiate(languageTogglePrefab, container);
			languageToggle.name = locale.Identifier.CultureInfo != null
				? locale.Identifier.CultureInfo.NativeName
				: locale.ToString();
			var label = languageToggle.GetComponentInChildren<TMP_Text>();
			label.text = languageToggle.name;

			var toggle = languageToggle.GetComponent<Toggle>();
			toggle.SetIsOnWithoutNotify(LocalizationSettings.SelectedLocale == locale);

			// We use a dictionary of the toggles so we can quickly update the selected locale if it is changed by another script.
			m_Toggles[locale] = toggle;

			toggle.onValueChanged.AddListener(val =>
			{
				if (val)
				{
					// Unsubscribe from SelectedLocaleChanged so we don't get an unnecessary callback from the change we are about to make.
					LocalizationSettings.SelectedLocaleChanged -= LocalizationSettings_SelectedLocaleChanged;

					LocalizationSettings.SelectedLocale = locale;

					// Resubscribe to SelectedLocaleChanged so that we can stay in sync with changes that may be made by other scripts.
					LocalizationSettings.SelectedLocaleChanged += LocalizationSettings_SelectedLocaleChanged;
				}
			});

			toggle.group = m_ToggleGroup;
		}
	}

	private void LocalizationSettings_SelectedLocaleChanged(Locale locale)
	{
		if (m_Toggles.TryGetValue(locale, out var toggle))
		{
			toggle.SetIsOnWithoutNotify(true);
		}
	}
}
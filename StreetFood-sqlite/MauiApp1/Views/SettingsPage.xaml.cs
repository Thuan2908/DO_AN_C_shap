#pragma warning disable CA1416
using System;
using System.Globalization;
using System.Threading;
using MauiApp1.Resources.Languages;
using MauiApp1.Services;

namespace MauiApp1.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();

        // ===== Load settings =====
        volumeSlider.Value = SettingsService.Volume;
        speedSlider.Value = SettingsService.SpeechSpeed;
        gpsSwitch.IsToggled = SettingsService.GPSBackground;

        // ===== Load Theme =====
        if (SettingsService.Theme == "Dark")
        {
            themeSwitch.IsToggled = true;
            themeLabel.Text = "🌙 Dark";
        }
        else
        {
            themeSwitch.IsToggled = false;
            themeLabel.Text = "☀ Light";
        }

        // ===== Update UI =====
        VolumeChanged(volumeSlider, new ValueChangedEventArgs(0, volumeSlider.Value));
        SpeedChanged(speedSlider, new ValueChangedEventArgs(0, speedSlider.Value));

        UpdateLanguageLabel();
    }

    // ===== Volume =====
    void VolumeChanged(object sender, ValueChangedEventArgs e)
    {
        int volume = (int)e.NewValue;
        SettingsService.Volume = volume;

        volumeLabel.FormattedText = new FormattedString
        {
            Spans =
            {
                new Span { Text = AppResources.Volume },
                new Span { Text = ": " },
                new Span { Text = $"{volume}%" }
            }
        };
    }

    // ===== Speed =====
    void SpeedChanged(object sender, ValueChangedEventArgs e)
    {
        double speed = e.NewValue;
        SettingsService.SpeechSpeed = speed;

        speedLabel.FormattedText = new FormattedString
        {
            Spans =
            {
                new Span { Text = AppResources.SpeechSpeed },
                new Span { Text = ": " },
                new Span { Text = $"{speed:F1}x" }
            }
        };
    }

    // ===== GPS =====
    void GpsToggled(object sender, ToggledEventArgs e)
    {
        bool enabled = e.Value;
        SettingsService.GPSBackground = enabled;

        if (enabled)
            BackgroundGpsManager.Start();
        else
            BackgroundGpsManager.Stop();
    }

    // ===== THEME (SWITCH) =====
    void ThemeToggled(object sender, ToggledEventArgs e)
    {
        bool isDark = e.Value;

        if (isDark)
        {
            SettingsService.Theme = "Dark";
            Application.Current.UserAppTheme = AppTheme.Dark;
            themeLabel.Text = "🌙 Dark";
        }
        else
        {
            SettingsService.Theme = "Light";
            Application.Current.UserAppTheme = AppTheme.Light;
            themeLabel.Text = "☀ Light";
        }
    }

    // ===== LANGUAGE =====
    async void ChooseLanguage(object sender, EventArgs e)
    {
        string result = await DisplayActionSheetAsync(
            AppResources.ChooseLanguage,
            AppResources.Cancel,
            null,
            "English",
            "Tiếng Việt",
            "日本語"
        );

        if (result == null || result == AppResources.Cancel)
            return;

        if (result == "English")
            SetLanguage("en");
        else if (result == "Tiếng Việt")
            SetLanguage("vi");
        else if (result == "日本語")
            SetLanguage("ja");
    }

    void SetLanguage(string lang)
    {
        SettingsService.Language = lang;

        var culture = new CultureInfo(lang);

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        AppResources.Culture = culture;

        var app = Application.Current;

        if (app != null && app.Windows.Count > 0)
        {
            app.Windows[0].Page = new AppShell();
        }
    }

    void UpdateLanguageLabel()
    {
        var lang = SettingsService.Language;

        if (lang == "en")
            languageLabel.Text = "English";
        else if (lang == "vi")
            languageLabel.Text = "Tiếng Việt";
        else if (lang == "ja")
            languageLabel.Text = "日本語";
    }

    // ===== RESET =====
    async void ResetSettings(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlertAsync(
            AppResources.Reset,
            AppResources.ResetConfirm,
            AppResources.OK,
            AppResources.Cancel);

        if (!confirm) return;

        // reset settings
        SettingsService.Volume = 100;
        SettingsService.SpeechSpeed = 1.0;
        SettingsService.GPSBackground = true;
        SettingsService.Theme = "Light";

        // update UI
        volumeSlider.Value = 100;
        speedSlider.Value = 1.0;
        gpsSwitch.IsToggled = true;

        themeSwitch.IsToggled = false;
        themeLabel.Text = "☀ Light";

        // apply theme
        Application.Current.UserAppTheme = AppTheme.Light;

        // bật lại GPS
        BackgroundGpsManager.Start();

        // reset language
        SetLanguage("vi");
    }
}
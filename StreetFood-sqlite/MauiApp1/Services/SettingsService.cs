using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Maui.Storage;

namespace MauiApp1.Services
{
    public static class SettingsService
    {
        // ===== Language =====
        public static string Language
        {
            get => Preferences.Get("language", "vi");
            set => Preferences.Set("language", value);
        }

        // ===== Volume =====
        public static int Volume
        {
            get => Preferences.Get("volume", 100);
            set => Preferences.Set("volume", value);
        }

        // ===== Speech Speed =====
        public static double SpeechSpeed
        {
            get => Preferences.Get("speech_speed", 1.0);
            set => Preferences.Set("speech_speed", value);
        }

        // ===== GPS Background =====
        public static bool GPSBackground
        {
            get => Preferences.Get("gps_background", false);
            set => Preferences.Set("gps_background", value);
        }
        //THEME
        public static string Theme
        {
            get => Preferences.Get("theme", "Light"); // mặc định Light
            set => Preferences.Set("theme", value);
        }
    }


}

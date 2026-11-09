// RPMac - Internationalization (i18n)
// Copyright (C) 2026 golirino - GPL-2.0-only. Ver LICENSE y NOTICE.
//
// Lightweight string lookup: no .resx files, no Visual Studio, no dependencies.
// Keys are the English strings themselves, so the existing code reads naturally
// and the fallback is always the original text.
//
// Usage:
//   I18n.SetLanguage("it");           // set current language
//   Text = I18n.T("Settings");        // get translated string
//   I18n.T("Fan {0}: ...", n, ...);   // format with args

using System;
using System.Collections.Generic;

namespace MacBreeze {

    static partial class I18n {

        static string _lang = "de";

        // Die Wörterbücher stehen in I18n.Langs.cs (erzeugt aus tools/i18n/*.json).

        public static void SetLanguage(string lang) {
            if (lang == null) lang = DetectWindowsLanguage();
            lang = lang.ToLowerInvariant();
            if (!IsSupported(lang)) lang = "en";
            _lang = lang;
        }

        public static string CurrentLanguage { get { return _lang; } }

        static Dictionary<string, string> CurrentDict { get { return Dict(_lang); } }

        /// <summary>
        /// Get the translated string for the given key (the English text).
        /// Falls back to the key itself if no translation is found.
        /// </summary>
        public static string T(string key) {
            if (key == null) return null;
            var dict = CurrentDict;
            if (dict != null) {
                string val;
                if (dict.TryGetValue(key, out val)) return val.Replace("RPMac", "MacBreeze");
            }
            return key.Replace("RPMac", "MacBreeze");  // fallback: English (Name der App ersetzen)
        }

        /// <summary>
        /// Get a translated format string and apply string.Format with the given args.
        /// </summary>
        public static string T(string key, params object[] args) {
            return string.Format(T(key), args);
        }

        /// <summary>
        /// Returns true if the given language code is supported.
        /// </summary>
        public static bool IsSupported(string lang) {
            foreach (var l in LANGS) if (l[0] == lang) return true;
            return false;
        }

        /// <summary>Sprache von Windows, falls MacBreeze sie kennt, sonst Englisch.</summary>
        public static string DetectWindowsLanguage() {
            try {
                string two = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
                return IsSupported(two) ? two : "en";
            } catch { return "en"; }
        }
    }
}
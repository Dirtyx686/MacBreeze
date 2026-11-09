#!/usr/bin/env python3
"""Erzeugt src/gui/I18n.Langs.cs aus tools/i18n/*.json.

Jede JSON-Datei: { "englischer Text": "Übersetzung", ... }. Englisch ist die Quellsprache
(der Schlüssel selbst) und braucht keine Datei. Fehlende Einträge fallen auf Englisch zurück.
Aufruf:  python tools/gen_i18n.py
"""
import json, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
# Reihenfolge und Anzeigename in der Sprachauswahl (immer in der eigenen Sprache)
LANGS = [("de", "Deutsch"), ("en", "English"), ("fr", "Français"), ("es", "Español"),
         ("it", "Italiano"), ("pt", "Português"), ("nl", "Nederlands"), ("pl", "Polski"),
         ("ro", "Română"), ("cs", "Čeština"), ("sk", "Slovenčina"), ("sl", "Slovenščina"),
         ("hr", "Hrvatski"), ("hu", "Magyar"), ("bg", "Български"), ("el", "Ελληνικά"),
         ("sv", "Svenska"), ("da", "Dansk"), ("fi", "Suomi"), ("et", "Eesti"),
         ("lv", "Latviešu"), ("lt", "Lietuvių"), ("mt", "Malti"), ("ga", "Gaeilge")]

def cs(s):
    return '"' + s.replace('\\', '\\\\').replace('"', '\\"').replace('\r', '\\r').replace('\n', '\\n') + '"'

ref = json.load(open(os.path.join(HERE, "i18n", "de.json"), encoding="utf-8"))
out = ["// AUTOMATISCH ERZEUGT von tools/gen_i18n.py aus tools/i18n/*.json - nicht von Hand bearbeiten.",
       "// Teil von MacBreeze - GPL-2.0-only.", "", "using System.Collections.Generic;", "",
       "namespace MacBreeze {", "", "    static partial class I18n {", "",
       "        internal static readonly string[][] LANGS = new string[][] {"]
out += ['            new[] { %s, %s },' % (cs(c), cs(n)) for c, n in LANGS]
out += ["        };", "", "        static Dictionary<string, string> Dict(string lang) {", "            switch (lang) {"]
present = []
for c, _ in LANGS:
    p = os.path.join(HERE, "i18n", c + ".json")
    if c != "en" and os.path.exists(p):
        present.append(c)
        out.append('                case "%s": return L_%s;' % (c, c.upper()))
out += ["            }", "            return null;   // en: der Schlüssel ist der Text", "        }", ""]
report = []
for c in present:
    d = json.load(open(os.path.join(HERE, "i18n", c + ".json"), encoding="utf-8"))
    missing = [k for k in ref if k not in d]
    report.append("%s: %d Einträge, %d fehlen" % (c, len(d), len(missing)))
    out.append("        static readonly Dictionary<string, string> L_%s = new Dictionary<string, string> {" % c.upper())
    for k in sorted(d):
        out.append("            { %s, %s }," % (cs(k), cs(d[k])))
    out.append("        };")
    out.append("")
out += ["    }", "}", ""]
with open(os.path.join(ROOT, "src", "gui", "I18n.Langs.cs"), "w", encoding="utf-8-sig", newline="\r\n") as f:
    f.write("\n".join(out))
print("\n".join(report))

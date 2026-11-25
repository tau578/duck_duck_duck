using System;
using System.Collections.Generic;
using UnityEngine;

namespace Duck
{
    public enum Lang { RU, EN }

    /// <summary>
    /// Minimal localization: one table of keys with Russian and English text.
    /// Loc.T("key") returns the text, Loc.F("key", args) formats it. The language is saved in PlayerPrefs.
    /// </summary>
    public static class Loc
    {
        const string PrefKey = "lang";
        static bool loaded;
        static Lang current;

        public static event Action Changed;

        public static Lang Current
        {
            get
            {
                if (!loaded)
                {
                    loaded = true;
                    current = (Lang)PlayerPrefs.GetInt(PrefKey, (int)Lang.RU);
                }
                return current;
            }
            set
            {
                loaded = true;
                if (current == value) return;
                current = value;
                PlayerPrefs.SetInt(PrefKey, (int)value);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        public static void Toggle() => Current = Current == Lang.RU ? Lang.EN : Lang.RU;

        public static string T(string key)
        {
            if (!Table.TryGetValue(key, out var pair)) return key;
            return Current == Lang.EN ? pair.en : pair.ru;
        }

        public static string F(string key, params object[] args) => string.Format(T(key), args);

        /// <summary>Picks the English variant of a data name if there is one.</summary>
        public static string Pick(string ru, string en) => Current == Lang.EN && !string.IsNullOrEmpty(en) ? en : ru;

        static readonly Dictionary<string, (string ru, string en)> Table = new Dictionary<string, (string, string)>
        {
            // HUD
            ["hud.fight"] = ("В БОЙ!", "FIGHT!"),
            ["hud.restart"] = ("Ещё раз", "Play again"),
            ["hud.base"] = ("База", "Base"),
            ["hud.base_attack"] = ("База под атакой!", "Base under attack!"),
            ["hud.wave"] = ("Волна {0}/{1}{2}", "Wave {0}/{1}{2}"),
            ["hud.wave_boss"] = ("  · БОСС", "  · BOSS"),
            ["hud.wave_elite"] = ("  · элита", "  · elite"),
            ["hud.day"] = ("День {0}: строй оборону", "Day {0}: build your defense"),
            ["hud.night"] = ("Ночь {0}   {1:00}:{2:00}   врагов: {3}", "Night {0}   {1:00}:{2:00}   enemies: {3}"),
            ["hud.duck_hp"] = ("Утка {0}/{1}", "Duck {0}/{1}"),
            ["hud.duck_respawn"] = ("Утка вернётся через {0} с", "Duck is back in {0}s"),
            ["hud.build"] = ("Построить «{0}» за {1}", "Build {0} for {1}"),
            ["hud.upgrade"] = ("Улучшить «{0}» до ур.{1} за {2}", "Upgrade {0} to lv.{1} for {2}"),
            ["hud.victory"] = ("Победа!", "Victory!"),
            ["hud.defeat"] = ("База разрушена", "Base destroyed"),
            ["hud.victory_sub"] = ("Все {0} волны отбиты", "All {0} waves repelled"),
            ["hud.defeat_sub"] = ("Продержались {0} из {1} волн", "Survived {0} of {1} waves"),

            // Banners
            ["banner.start"] = ("{0}\nСтрой оборону и жми «В бой!»", "{0}\nBuild your defense and press FIGHT!"),
            ["banner.dawn"] = ("Рассвет! +{0} монет\nВолна {1} из {2} впереди", "Dawn! +{0} coins\nWave {1} of {2} ahead"),
            ["banner.boss"] = ("БОСС!", "BOSS!"),
            ["banner.elite"] = ("Элитный враг приближается!", "Elite enemy incoming!"),
            ["banner.attack"] = ("Монстры атакуют!", "Monsters attack!"),
            ["banner.duck_down"] = ("Утку подбили!\nВозвращение через {0} с", "Duck is down!\nBack in {0}s"),

            // World pop-ups
            ["fx.built"] = ("Построено!", "Built!"),
            ["fx.level"] = ("Ур. {0}!", "Lv. {0}!"),
            ["fx.destroyed"] = ("{0}: разрушено", "{0} destroyed"),
            ["fx.back"] = ("Снова в строю!", "Back in action!"),
            ["slot.empty"] = ("{0}\n{1} мон.", "{0}\n{1} coins"),
            ["slot.upgrade"] = ("ур.{0}: {1}", "lv.{0}: {1}"),
        };
    }
}

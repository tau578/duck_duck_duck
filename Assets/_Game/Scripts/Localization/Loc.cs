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
            ["hud.duck_respawn"] = ("Призрак: {0} с", "Ghost: {0}s"),
            ["hud.build"] = ("Построить «{0}» за {1}", "Build {0} for {1}"),
            ["hud.upgrade"] = ("Улучшить «{0}» до ур.{1} за {2}", "Upgrade {0} to lv.{1} for {2}"),
            ["hud.victory"] = ("Победа!", "Victory!"),
            ["hud.defeat"] = ("База разрушена", "Base destroyed"),
            ["hud.victory_sub"] = ("Отбито волн: {0} из {0}", "All {0} waves repelled"),
            ["hud.defeat_sub"] = ("Продержались {0} из {1} волн", "Survived {0} of {1} waves"),

            // Banners
            ["banner.start"] = ("{0}\nСтрой оборону и жми «В бой!»", "{0}\nBuild your defense and press FIGHT!"),
            ["banner.dawn"] = ("Рассвет! +{0} монет\nВолна {1} из {2} впереди", "Dawn! +{0} coins\nWave {1} of {2} ahead"),
            ["banner.boss"] = ("БОСС!", "BOSS!"),
            ["banner.elite"] = ("Элитный враг приближается!", "Elite enemy incoming!"),
            ["banner.rage"] = ("Момент ярости!\nВраги ускорились", "Rage moment!\nEnemies are faster"),
            ["banner.attack"] = ("Монстры атакуют!", "Monsters attack!"),
            ["banner.duck_down"] = ("Утку подбили!\nВоскрешение через {0} с", "Duck is down!\nReviving in {0}s"),

            // World pop-ups
            ["fx.built"] = ("Построено!", "Built!"),
            ["fx.level"] = ("Ур. {0}!", "Lv. {0}!"),
            ["fx.destroyed"] = ("{0}: разрушено", "{0} destroyed"),
            ["fx.restored"] = ("Восстановлено", "Rebuilt"),
            ["fx.restored_unpaid"] = ("Восстановлено, без дохода", "Rebuilt, no income"),
            ["fx.combo"] = ("КОМБО x{0}!  +{1}", "COMBO x{0}!  +{1}"),
            ["fx.back"] = ("Снова в строю!", "Back in action!"),
            ["slot.empty"] = ("{0}\n{1} мон.", "{0}\n{1} coins"),
            ["slot.upgrade"] = ("ур.{0}: {1}", "lv.{0}: {1}"),

            // Menu and meta
            ["menu.title"] = ("Утка держит оборону", "Duck Defense"),
            ["menu.gears"] = ("Шестерёнки: {0}", "Gears: {0}"),
            ["menu.chapters"] = ("Выбор уровня", "Choose a level"),
            ["menu.locked"] = ("Закрыто: пройди главу {0}", "Locked: beat chapter {0}"),
            ["menu.best"] = ("Лучшее: {0}/{1} волн", "Best: {0}/{1} waves"),
            ["menu.new"] = ("Не пройдена", "Not played yet"),
            ["menu.workshop"] = ("Мастерская: постоянные улучшения", "Workshop: permanent upgrades"),
            ["menu.reset"] = ("Сброс (dev)", "Reset (dev)"),
            ["menu.cheat"] = ("+100 (dev)", "+100 (dev)"),
            ["menu.play"] = ("Новая игра", "New game"),
            ["menu.workshop_btn"] = ("Мастерская", "Workshop"),
            ["menu.settings"] = ("Настройки", "Settings"),
            ["menu.back"] = ("Назад", "Back"),
            ["menu.unlock_all"] = ("Открыть все (dev)", "Unlock all (dev)"),
            ["set.sfx"] = ("Звуки: {0}", "Sound: {0}"),
            ["set.lang"] = ("Язык: Русский", "Language: English"),
            ["set.access"] = ("Доступность", "Accessibility"),
            ["set.large"] = ("Крупный текст: {0}", "Large text: {0}"),
            ["set.colorblind"] = ("Режим для дальтоников: {0}", "Color-blind mode: {0}"),
            ["set.motion"] = ("Без тряски и вспышек: {0}", "Reduce shake & flashes: {0}"),
            ["set.slow"] = ("Медленная игра (75%): {0}", "Slower game (75%): {0}"),
            ["set.on"] = ("вкл", "on"),
            ["set.off"] = ("выкл", "off"),
            ["meta.max"] = ("МАКС", "MAX"),
            ["meta.level"] = ("ур. {0}/{1}", "lv. {0}/{1}"),
            ["meta.cost"] = ("{0} шест.", "{0} gears"),
            ["meta.duck_power.name"] = ("Сила утки", "Duck Power"),
            ["meta.duck_power.desc"] = ("+8% урон утки за уровень", "+8% duck damage per level"),
            ["meta.fortify.name"] = ("Укрепления", "Fortifications"),
            ["meta.fortify.desc"] = ("+15% прочность построек за уровень", "+15% building HP per level"),
            ["meta.supplies.name"] = ("Запасы", "Supplies"),
            ["meta.supplies.desc"] = ("+2 монеты на старте глав 2 и 3", "+2 coins at the start of chapters 2 and 3"),
            ["meta.airstrike.name"] = ("Эскадрилья", "Squadron"),
            ["meta.airstrike.desc"] = ("+2 бомбы в авиаударе", "+2 bombs per airstrike"),
            ["result.reward"] = ("+{0} шестерёнок (всего {1})", "+{0} gears (total {1})"),
            ["hud.menu"] = ("В меню", "Menu"),
            ["hud.next"] = ("Следующая глава", "Next chapter"),
            ["banner.news"] = ("Новое: {0}", "New: {0}"),

            // Skills
            ["hud.level"] = ("Ур. {0}   {1}/{2}", "Lv. {0}   {1}/{2}"),
            ["air.button"] = ("АВИА-\nУДАР", "AIR\nSTRIKE"),
            ["air.ready"] = ("Авиаудар готов!", "Airstrike ready!"),
            ["air.banner"] = ("Авиаудар!", "Airstrike!"),
            ["chest.title"] = ("СУНДУК ТВОЙ!", "A CHEST FOR YOU!"),
            ["chest.sub"] = ("Бесплатных навыков: {0}", "Free skills: {0}"),
            ["chest.pick"] = ("Сундук: навык {0} из {1}", "Chest: skill {0} of {1}"),
            ["skill.title"] = ("Уровень {0}! Выбери навык", "Level {0}! Pick a skill"),
            ["skill.new"] = ("Новый", "New"),
            ["skill.stars"] = ("ур. {0} → {1}", "lv. {0} → {1}"),
            ["skill.awaken"] = ("ПРОБУЖДЕНИЕ", "AWAKENING"),
            ["skill.double_shot.name"] = ("Двойной выстрел", "Double Shot"),
            ["skill.double_shot.1"] = ("2 пули в залпе, каждая 70% урона", "2 bullets per shot, 70% damage each"),
            ["skill.double_shot.2"] = ("3 пули веером, каждая 60% урона", "A fan of 3 bullets, 60% damage each"),
            ["skill.double_shot.3"] = ("Пули пробивают врага: второй получает половину урона", "Bullets pierce: the second enemy takes half damage"),
            ["skill.blades.name"] = ("Вращающиеся лезвия", "Spinning Blades"),
            ["skill.blades.1"] = ("2 лезвия кружат вокруг утки", "2 blades orbit the duck"),
            ["skill.blades.2"] = ("+1 лезвие, радиус больше", "+1 blade, wider orbit"),
            ["skill.blades.3"] = ("Золотые лезвия: урон +60%", "Golden blades: +60% damage"),
            ["skill.meteor.name"] = ("Метеор", "Meteor"),
            ["skill.meteor.1"] = ("Раз в 10 с метеор падает на врага", "Every 10s a meteor hits an enemy"),
            ["skill.meteor.2"] = ("+1 метеор", "+1 meteor"),
            ["skill.meteor.3"] = ("Метеорный дождь: 3 метеора, шире взрыв", "Meteor shower: 3 meteors, bigger blast"),
            ["skill.rapid.name"] = ("Скорострельность", "Rapid Fire"),
            ["skill.rapid.1"] = ("+10% скорость стрельбы", "+10% fire rate"),
            ["skill.rapid.2"] = ("+10% скорость стрельбы", "+10% fire rate"),
            ["skill.rapid.3"] = ("+10% скорость стрельбы", "+10% fire rate"),
            ["skill.power.name"] = ("Мощная пуля", "Power Shot"),
            ["skill.power.1"] = ("+10% урон утки", "+10% duck damage"),
            ["skill.power.2"] = ("+10% урон утки", "+10% duck damage"),
            ["skill.power.3"] = ("+10% урон утки", "+10% duck damage"),
            ["skill.fortify.name"] = ("Укрепления", "Fortify"),
            ["skill.fortify.1"] = ("+25% прочность построек, полный ремонт", "+25% building HP, full repair"),
            ["skill.fortify.2"] = ("+25% прочность построек, полный ремонт", "+25% building HP, full repair"),
            ["skill.vitality.name"] = ("Живучесть", "Vitality"),
            ["skill.vitality.1"] = ("+25 ОЗ утки, полное лечение", "+25 duck HP, full heal"),
            ["skill.vitality.2"] = ("+25 ОЗ утки, полное лечение", "+25 duck HP, full heal"),
        };
    }
}

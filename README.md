# Duck Defense

Прототип по мотивам Duck Survival: рогалик-шутер с tower defense. Днём строим базу, ночью отбиваем волны.

## Что здесь лежит

| Что | Где |
|---|---|
| APK для Android | [Submission/DuckDefense.apk](Submission/DuckDefense.apk) |
| Видео геймплея 1080×1920 | [Submission/gameplay.mp4](Submission/gameplay.mp4) |
| Описание: кор, решения, развитие, монетизация | [docs/submission.md](docs/submission.md) |
| Учёт часов | [docs/hours_log.md](docs/hours_log.md) |
| ИИ-инструменты и арт | [docs/art_prompts.md](docs/art_prompts.md) |
| Разбор оригинала | [docs/duck_survival_analysis.md](docs/duck_survival_analysis.md) |
| Баланс и его изменения | [docs/chapter1_balance.md](docs/chapter1_balance.md), [docs/power_balance.md](docs/power_balance.md) |
| План прототипа | [docs/prototype_plan.md](docs/prototype_plan.md) |

## Установка и управление

Установите APK на телефон (нужно разрешить установку из этого источника) или перетащите его в окно BlueStacks.

- **День:** бегайте джойстиком, подойдите к площадке и нажмите «Построить» или «Улучшить», затем «В бой!».
- **Ночь:** утка стреляет сама; двигайтесь, выбирайте навыки, кнопкой с самолётом вызывайте авиаудар.
- Поражение — разрушение базы.

## Сборка из исходников

Unity **6000.3.22f1** с модулем Android.

1. Откройте проект.
2. Меню **Duck → Build Android APK** (результат: `Builds/Android/DuckDefense.apk`).

Из командной строки (редактор закрыт):

```
Unity.exe -batchmode -quit -projectPath <путь> -buildTarget Android -executeMethod Duck.EditorTools.BuildScript.BuildAndroid -logFile build.log
```

Сцена и данные генерируются кодом: меню **Duck → Build Game Scene**. Пункт **Reset Data To Defaults And Build** перезаписывает баланс значениями из `SceneBuilder.cs`.

## Структура

```
Assets/_Game/
  Scripts/   Core, Player, Enemies, Building, Skills, Economy, Meta, UI, Feedback, Localization
  Data/      ScriptableObject: главы, враги, постройки
  Editor/    SceneBuilder (сцена и данные), BuildScript (APK)
```

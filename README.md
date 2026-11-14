# Adaptive Fast Forward

A Bloons TD 6 mod. While fast forwarding, it lowers the speed when the simulation can't keep up and raises it back afterwards, so heavy moments slow down instead of stuttering. Every tick is simulated as before, so balance doesn't change.

- Minimum and maximum speed are in the mod settings.
- The current speed is shown left of the round counter.
- In co-op, with [Coop Speed Sync](https://github.com/Antonzlo/btd6-coop-speed-sync) installed, everyone slows down to the slowest player instead of stalling for them.

Meant for speeds above the game's own x3, for example with Faster Forward.

## Requirements

- [MelonLoader](https://github.com/LavaGang/MelonLoader)
- [BTD6 Mod Helper](https://github.com/gurrenm3/BTD-Mod-Helper)

## Install

Put `AdaptiveFastForward.dll` into the game's `Mods` folder, or install it from the Mod Browser.

## Build

```
dotnet build -c Release
```

The project looks for the game in `~/.local/share/Steam/steamapps/common/BloonsTD6`. Pass another folder with `-p:BloonsTD6=<path>`.

## Translations

English is in the code, Russian in `Localization/Russian.json`. Add `Localization/<Language>.json` with the same keys for another language.

---

# Adaptive Fast Forward (русский)

Мод для Bloons TD 6. Во время перемотки снижает скорость, когда симуляция не успевает, и потом возвращает её обратно: тяжёлые моменты идут медленнее, а не рывками. Каждый тик считается как раньше, баланс не меняется.

- Минимальная и максимальная скорость задаются в настройках мода.
- Текущая скорость показана слева от счётчика раундов.
- В коопе вместе с [Coop Speed Sync](https://github.com/Antonzlo/btd6-coop-speed-sync) все замедляются до самого медленного игрока, а не ждут его.

Рассчитан на скорости выше штатной x3, например с модом Faster Forward.

## Установка

Нужны MelonLoader и BTD6 Mod Helper. Положите `AdaptiveFastForward.dll` в папку `Mods` игры или установите мод через Mod Browser.

# NamazWidget — «Время намаза»

Виджет для **Windows** и **macOS** в одном репозитории: клонируйте — и правьте обе версии.

- Windows — C# (WPF), Windows 7/8/10/11, собирается компилятором из .NET Framework 4, ничего ставить не нужно.
- macOS — нативный виджет для строки меню macOS на Swift (AppKit + SwiftUI), macOS 12 и новее. Значка в Dock нет.

## Скачать

Готовые файлы лежат в [`release/`](release/):

| ОС | Файл | |
|---|---|---|
| Windows | [`release/NamazWidget.exe`](release/NamazWidget.exe) | |
| macOS | [`release/NamazWidget.dmg`](release/NamazWidget.dmg) | появится после первой сборки на Mac (`bash macos/build.sh`) |

## Структура

```
windows/   исходник NamazWidget.cs, значок, build.cmd
macos/     исходники Swift (Sources/, Shared/, Resources/), build.sh
release/   собранные NamazWidget.exe и NamazWidget.dmg — коммитятся в git
```

После изменений соберите нужную версию — файл в `release/` обновится — и закоммитьте его вместе с кодом.

## Сборка для Windows

```bat
windows\build.cmd
```

Результат — `release\NamazWidget.exe`. Если виджет запущен из `release\`, сначала закройте его, иначе файл занят.

## Сборка для macOS

```bash
xcode-select --install      # один раз, если нет Xcode
bash macos/build.sh         # только Apple Silicon: ARCHS=arm64 bash macos/build.sh
```

Готовый `macos/build/NamazWidget.app` перенесите в «Программы».
Если .app скопировали с другого Mac (zip, флешка), macOS может заблокировать запуск — один раз:
`xattr -dr com.apple.quarantine /Applications/NamazWidget.app` (или правый клик → «Открыть»).

`build.sh` также создаёт `release/NamazWidget.dmg`.

## Возможности

Полумесяц со звездой в строке меню — одним цветом, как значки других программ (белый в тёмной теме, чёрный в светлой).
Наведение — под ним панель со стрелкой, компактная карточка (отсчёт, текущий и следующий намаз, жамагат);
нажатие — полный вид в той же панели: слева кольцо до следующего намаза, часы и даты милади/хиджри, справа список времён и жамагат;
нажали мимо — панель закрывается. Правый клик по полумесяцу или по панели — меню виджета. Пять языков, выбор города
(поиск и список страна → регион), годовое расписание в кэше — работает без интернета,
уведомления с окном в центре экрана, звуком и уведомлением macOS, прозрачность окна-уведомления.
В полноэкранной программе (Telegram, Safari и т. п.) карточка по наведению не всплывает, а вместо окна-уведомления
приходит уведомление macOS.

Настройки: `~/Library/Application Support/NamazWidget/`.

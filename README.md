# NamazWidget — «Время намаза»

Время намаза в строке меню macOS и на рабочем столе Windows: отсчёт до следующего намаза, расписание на день, жамагат и уведомления.
Данные — с [namazvakti.com](https://namazvakti.com).

- **macOS** — нативный виджет для строки меню на Swift (AppKit + SwiftUI), macOS 12 и новее. Значка в Dock нет.
- **Windows** — C# (WPF), Windows 7 / 8 / 10 / 11. Собирается компилятором из .NET Framework 4, который уже есть в Windows, — ничего ставить не нужно.

## Скачать

| ОС | Файл | Примечание |
|---|---|---|
| macOS | [NamazWidget.dmg](release/NamazWidget.dmg) | Apple Silicon (M1 и новее); для Intel соберите из исходников |
| Windows | [NamazWidget.exe](release/NamazWidget.exe) | один файл, установка не нужна |

Приложения не подписаны, поэтому при первом запуске система может предупредить:

- **macOS** — перетащите приложение из DMG в «Программы», затем правый клик → «Открыть» или один раз выполните
  `xattr -dr com.apple.quarantine /Applications/NamazWidget.app`.
- **Windows** — в окне SmartScreen нажмите «Подробнее» → «Выполнить в любом случае».

## Возможности

- Полумесяц со звездой в строке меню — одним цветом, как значки других программ (белый в тёмной теме, чёрный в светлой).
- Наведение — компактная карточка: отсчёт, текущий и следующий намаз, жамагат.
- Нажатие — полный вид: кольцо до следующего намаза, часы, даты по милади и хиджре, список времён и жамагат.
  Нажали мимо — панель закрывается; правый клик — меню виджета.
- Пять языков: Қазақша, Русский, O'zbekcha, Кыргызча, English.
- Выбор города: поиск или список «страна → регион».
- Расписание на год сохраняется — виджет работает без интернета.
- Уведомления: окно в центре экрана со звуком и уведомление системы; прозрачность окна настраивается.
- В полноэкранной программе (Telegram, Safari и т. п.) карточка по наведению не всплывает, а вместо окна приходит уведомление macOS.

Настройки хранятся в `~/Library/Application Support/NamazWidget/` (macOS) и `%APPDATA%\NamazWidget\` (Windows).

## Структура

```
macos/     исходники Swift (Sources/, Shared/, Resources/) и build.sh
windows/   исходник NamazWidget.cs, значок и build.cmd
release/   готовые NamazWidget.dmg и NamazWidget.exe
```

## Сборка

### macOS

```bash
xcode-select --install          # один раз, если нет Xcode или Command Line Tools
bash macos/build.sh             # универсальная сборка: Apple Silicon + Intel
ARCHS=arm64 bash macos/build.sh # только Apple Silicon
```

Результат — `macos/build/NamazWidget.app` и `release/NamazWidget.dmg`.

### Windows

```bat
windows\build.cmd
```

Результат — `release\NamazWidget.exe`. Если виджет запущен из `release\`, сначала закройте его, иначе файл занят.

После изменений в коде пересоберите нужную версию и закоммитьте обновлённый файл из `release/` вместе с исходниками.

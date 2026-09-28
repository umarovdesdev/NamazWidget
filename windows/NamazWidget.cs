// Виджет «Время намаза» для Windows 7 / 8 / 10 / 11 — данные с https://namazvakti.com
// Сборка: build.cmd (компилятор C# из .NET Framework 4, есть в Windows с .NET 4.5+)
// Код написан на C# 5, чтобы собираться встроенным в Windows компилятором csc.exe.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using WinForms = System.Windows.Forms;
using Gdi = System.Drawing;

namespace NamazTimesWidget
{
    sealed class Lang
    {
        public string Code, Label, Culture;
        public Dictionary<string, string> Text, Prayers;
        public string[] Months;
    }

    static class I18n
    {
        public static readonly List<Lang> All = new List<Lang>();

        static Dictionary<string, string> Map(params string[] kv)
        {
            var d = new Dictionary<string, string>();
            for (int i = 0; i + 1 < kv.Length; i += 2) d[kv[i]] = kv[i + 1];
            return d;
        }

        static I18n()
        {
            All.Add(new Lang {
                Code = "kz", Label = "Қазақша", Culture = "kk-KZ",
                Prayers = Map("imsak", "Имсак", "bamdat", "Бамдат", "kun", "Күн", "ishraq", "Ишрақ", "kerahat", "Керахат",
                              "besin", "Бесін", "asriauual", "Асри әууал", "ekindi", "Екінді", "isfirar", "Исфирар",
                              "aqsham", "Ақшам", "ishtibaq", "Иштибақ", "quptan", "Құптан", "ishaisani", "Ишаи сани"),
                Months = new[] { "Мұхаррам", "Сафар", "Рабиул-әууәл", "Рабиул-ахир", "Жумадал-ула", "Жумадал-ахира",
                                 "Ражаб", "Шағбан", "Рамазан", "Шәууәл", "Зұлқағда", "Зұлхижжа" },
                Text = Map(
                    "Position", "Орналасуы", "PosBR", "Оң жақ төменгі бұрыш", "PosTR", "Оң жақ жоғарғы бұрыш", "PosBL", "Сол жақ төменгі бұрыш", "PosTL", "Сол жақ жоғарғы бұрыш", "PosFree", "Еркін (кез келген жерге сүйреуге болады)",
                    "FirstRunTitle", "Ел мен қаланы таңдаңыз — кесте интернетсіз жұмыс істейді",
                    "OldYear", "Жаңа жылдың кестесі әлі жүктелмеді — өткен жылдың уақыттары көрсетілген (±1–2 мин)",
                    "Transparency", "Мөлдірлік", "TransLow", "Төмен", "TransMid", "Орташа", "TransHigh", "Жоғары", "TransMax", "Барынша",
                    "DockCorner", "Жоғарғы оң бұрышқа бекіту",
                    "Minimize", "Трейге жасыру", "TrayHintTitle", "Виджет трейге жасырылды", "TrayHint", "Сағат жанындағы трейдегі ай белгішесін басыңыз (көрінбесе, алдымен ˄ басыңыз) немесе NamazWidget-ті қайта іске қосыңыз.",
                    "Browse", "Немесе тізімнен таңдаңыз:", "Country", "Ел", "Region", "Аймақ", "ListLoading", "Тізім жүктелуде…",
                    "Karahat", "Керахат", "TimesTitle", "Намаз уақыттары",
                    "HijriLabel", "Хижри", "MiladiLabel", "Миләди", "MiladiFormat", "{0} - {1}, {2} Жыл.",
                    "Until", "«{0}» уақытына дейін қалды", "HM", "{0} сағ {1} мин", "M", "{0} мин",
                    "Loading", "Жүктелуде…", "TrayTip", "Келесі намаз: {0}, {1}", "TrayDefault", "Намаз уақыты — namazvakti.com",
                    "Offline", "namazvakti.com-пен байланыс жоқ — {0} күнгі уақыттар көрсетілген",
                    "OfflineNoData", "namazvakti.com-пен байланыс жоқ. Бір минуттан кейін қайталанады…",
                    "NotifyAtMsg", "Намаз уақыты кірді", "NotifyBeforeMsg", "Басталуына {0} минут қалды", "Close", "Жабу",
                    "ChangeCity", "Қаланы өзгерту…", "ShowAll", "Асри әууал мен Ишаи саниді көрсету",
                    "Topmost", "Барлық терезелердің үстінде",
                    "Notifications", "Хабарламалар", "NotifyAtTime", "Намаз уақыты кіргенде хабарлау",
                    "NoBefore", "Алдын ала ескертпеу", "Before", "{0} минут бұрын ескерту",
                    "WinNotify", "Windows хабарламасын да көрсету",
                    "SoundMenu", "Хабарлама дыбысы", "SoundBell", "Қоңыраулар", "SoundSoft", "Жұмсақ дыбыс", "SoundSystem", "Жүйелік дыбыс",
                    "TestNotify", "Хабарламаны тексеру",
                    "Autostart", "Windows-пен бірге іске қосу", "Language", "Тіл / Language",
                    "Refresh", "Жаңарту", "OpenSite", "namazvakti.com ашу", "Hide", "Жасыру (трейдегі белгіше)", "Exit", "Шығу",
                    "TrayToggle", "Виджетті көрсету / жасыру", "Menu", "Мәзір",
                    "DlgTitle", "Қаланы таңдау — namazvakti.com", "DlgPrompt", "Қала немесе елді мекен атауы:", "Search", "Іздеу",
                    "Choose", "Таңдау", "Cancel", "Болдырмау", "Min2", "Кемінде 2 әріп", "Searching", "Іздеу…",
                    "Found", "Табылды: {0}", "NotFound", "Ештеңе табылмады", "NetError", "Желі қатесі",
                    "Jamaat", "Жамағат", "JamaatMenu", "Жамағат уақыты…", "JamaatTitle", "Жамағат уақыты",
                    "JamaatHint", "Мешіттегі жамағат уақытын СС:ММ түрінде енгізіңіз. Бос қалса — жамағат ескертпесі болмайды.",
                    "JamaatBefore", "Алдын ала ескерту:", "JamaatNoBefore", "Тек жамағат уақытында", "JamaatBeforeItem", "{0} минут бұрын",
                    "JamaatMsg", "Жамағат намазы басталады", "JamaatBeforeMsg", "Жамағатқа {0} минут қалды",
                    "Juma", "Жұма намазы (жұма күні)", "Azan", "азан {0}", "Save", "Сақтау", "BadTime", "Қате уақыт: {0}", "ByAzan", "Азан бойынша",
                    "NextJamaat", "Жамағат: {0} {1}", "ShowTimes", "Намаз уақыттарын көрсету", "HideTimes", "Намаз уақыттарын жасыру")
            });
            All.Add(new Lang {
                Code = "ru", Label = "Русский", Culture = "ru-RU",
                Prayers = Map("imsak", "Имсак", "bamdat", "Фаджр", "kun", "Восход", "ishraq", "Ишрак", "kerahat", "Зауаль карахат",
                              "besin", "Зухр", "asriauual", "Аср-и Аууаль", "ekindi", "Аср", "isfirar", "Исфирар",
                              "aqsham", "Магриб", "ishtibaq", "Иштибак", "quptan", "Иша", "ishaisani", "Иша-и Сани"),
                Months = new[] { "Мухаррам", "Сафар", "Рабиульауваль", "Рабиульахир", "Джумадаль-уля", "Джумадаль-ахира",
                                 "Раджаб", "Шаабан", "Рамадан", "Шавваль", "Зуль-каада", "Зуль-хиджа" },
                Text = Map(
                    "Position", "Положение", "PosBR", "Правый нижний угол", "PosTR", "Правый верхний угол", "PosBL", "Левый нижний угол", "PosTL", "Левый верхний угол", "PosFree", "Свободное (можно перетащить куда угодно)",
                    "FirstRunTitle", "Выберите страну и город — расписание будет работать без интернета",
                    "OldYear", "Расписание нового года ещё не скачано — показаны времена прошлого года (±1–2 мин)",
                    "Transparency", "Прозрачность", "TransLow", "Низкая", "TransMid", "Средняя", "TransHigh", "Высокая", "TransMax", "Максимальная",
                    "DockCorner", "Закрепить в правом верхнем углу",
                    "Minimize", "Свернуть в трей", "TrayHintTitle", "Виджет свёрнут в трей", "TrayHint", "Нажмите значок-полумесяц в трее у часов (если его не видно — сначала нажмите ˄) или снова запустите NamazWidget.",
                    "Browse", "Или выберите из списка:", "Country", "Страна", "Region", "Регион", "ListLoading", "Загрузка списка…",
                    "Karahat", "Карахат", "TimesTitle", "Время намазов",
                    "HijriLabel", "Хиджри", "MiladiLabel", "Милади", "MiladiFormat", "{0} - {1}, {2} год.",
                    "Until", "До «{0}» осталось", "HM", "{0} ч {1} мин", "M", "{0} мин",
                    "Loading", "Загрузка…", "TrayTip", "Намаз: {0} в {1}", "TrayDefault", "Время намаза — namazvakti.com",
                    "Offline", "Нет связи с namazvakti.com — показаны времена за {0}",
                    "OfflineNoData", "Нет связи с namazvakti.com. Повтор через минуту…",
                    "NotifyAtMsg", "Наступило время намаза", "NotifyBeforeMsg", "До начала осталось {0} мин", "Close", "Закрыть",
                    "ChangeCity", "Сменить город…", "ShowAll", "Показывать Аср-и Аууаль и Иша-и Сани",
                    "Topmost", "Поверх всех окон",
                    "Notifications", "Уведомления", "NotifyAtTime", "Уведомлять при наступлении намаза",
                    "NoBefore", "Не напоминать заранее", "Before", "Напоминать за {0} мин",
                    "WinNotify", "Также показывать уведомление Windows",
                    "SoundMenu", "Звук уведомления", "SoundBell", "Колокольчики", "SoundSoft", "Мягкий звон", "SoundSystem", "Системный звук",
                    "TestNotify", "Проверить уведомление",
                    "Autostart", "Запускать вместе с Windows", "Language", "Язык / Language",
                    "Refresh", "Обновить", "OpenSite", "Открыть namazvakti.com", "Hide", "Скрыть (значок в трее)", "Exit", "Выход",
                    "TrayToggle", "Показать / скрыть виджет", "Menu", "Меню",
                    "DlgTitle", "Выбор города — namazvakti.com", "DlgPrompt", "Название города или населённого пункта:", "Search", "Найти",
                    "Choose", "Выбрать", "Cancel", "Отмена", "Min2", "Минимум 2 буквы", "Searching", "Поиск…",
                    "Found", "Найдено: {0}", "NotFound", "Ничего не найдено", "NetError", "Ошибка сети",
                    "Jamaat", "Жамагат", "JamaatMenu", "Время жамагата…", "JamaatTitle", "Время жамагата",
                    "JamaatHint", "Введите время жамагата в мечети в формате ЧЧ:ММ. Пустое поле — без уведомления о жамагате.",
                    "JamaatBefore", "Напоминать заранее:", "JamaatNoBefore", "Только в момент жамагата", "JamaatBeforeItem", "За {0} мин",
                    "JamaatMsg", "Начинается жамагат-намаз", "JamaatBeforeMsg", "До жамагата осталось {0} мин",
                    "Juma", "Джума (пятница)", "Azan", "азан {0}", "Save", "Сохранить", "BadTime", "Неверное время: {0}", "ByAzan", "По азану",
                    "NextJamaat", "Жамагат: {0} {1}", "ShowTimes", "Показать времена намазов", "HideTimes", "Скрыть времена намазов")
            });
            All.Add(new Lang {
                Code = "uz", Label = "O'zbekcha", Culture = "uz-Latn-UZ",
                Prayers = Map("imsak", "Imsok", "bamdat", "Bomdod", "kun", "Quyosh", "ishraq", "Ishroq", "kerahat", "Zavol karohati",
                              "besin", "Peshin", "asriauual", "Asri avval", "ekindi", "Asr", "isfirar", "Isfirar",
                              "aqsham", "Shom", "ishtibaq", "Ishtibok", "quptan", "Xufton", "ishaisani", "Ishoi soniy"),
                Months = new[] { "Muharram", "Safar", "Rabiul-avval", "Rabiul-oxir", "Jumodul-avval", "Jumodul-oxir",
                                 "Rajab", "Shaʼbon", "Ramazon", "Shavvol", "Zulqaʼda", "Zulhijja" },
                Text = Map(
                    "Position", "Joylashuv", "PosBR", "Oʻng pastki burchak", "PosTR", "Oʻng yuqori burchak", "PosBL", "Chap pastki burchak", "PosTL", "Chap yuqori burchak", "PosFree", "Erkin (istalgan joyga sudrash mumkin)",
                    "FirstRunTitle", "Davlat va shaharni tanlang — jadval internetsiz ishlaydi",
                    "OldYear", "Yangi yil jadvali hali yuklanmadi — oʻtgan yil vaqtlari koʻrsatilgan (±1–2 daqiqa)",
                    "Transparency", "Shaffoflik", "TransLow", "Past", "TransMid", "Oʻrtacha", "TransHigh", "Yuqori", "TransMax", "Maksimal",
                    "DockCorner", "Yuqori oʻng burchakka mahkamlash",
                    "Minimize", "Trayga yigʻish", "TrayHintTitle", "Vidjet trayga yigʻildi", "TrayHint", "Soat yonidagi traydagi yarim oy belgisini bosing (koʻrinmasa, avval ˄ ni bosing) yoki NamazWidget'ni qayta ishga tushiring.",
                    "Browse", "Yoki roʻyxatdan tanlang:", "Country", "Davlat", "Region", "Viloyat", "ListLoading", "Roʻyxat yuklanmoqda…",
                    "Karahat", "Karohat", "TimesTitle", "Namoz vaqtlari",
                    "HijriLabel", "Hijriy", "MiladiLabel", "Milodiy", "MiladiFormat", "{0}-{1}, {2}-yil",
                    "Until", "«{0}» vaqtigacha qoldi", "HM", "{0} soat {1} daqiqa", "M", "{0} daqiqa",
                    "Loading", "Yuklanmoqda…", "TrayTip", "Namoz: {0} — {1}", "TrayDefault", "Namoz vaqtlari — namazvakti.com",
                    "Offline", "namazvakti.com bilan aloqa yoʻq — {0} sanasidagi vaqtlar koʻrsatilgan",
                    "OfflineNoData", "namazvakti.com bilan aloqa yoʻq. Bir daqiqadan soʻng qayta urinib koʻriladi…",
                    "NotifyAtMsg", "Namoz vaqti kirdi", "NotifyBeforeMsg", "Boshlanishiga {0} daqiqa qoldi", "Close", "Yopish",
                    "ChangeCity", "Shaharni oʻzgartirish…", "ShowAll", "Asri avval va Ishoi soniyni koʻrsatish",
                    "Topmost", "Barcha oynalar ustida",
                    "Notifications", "Bildirishnomalar", "NotifyAtTime", "Namoz vaqti kirganda xabar berish",
                    "NoBefore", "Oldindan eslatmaslik", "Before", "{0} daqiqa oldin eslatish",
                    "WinNotify", "Windows bildirishnomasini ham koʻrsatish",
                    "SoundMenu", "Bildirishnoma ovozi", "SoundBell", "Qoʻngʻiroqchalar", "SoundSoft", "Yumshoq jiringlash", "SoundSystem", "Tizim ovozi",
                    "TestNotify", "Bildirishnomani tekshirish",
                    "Autostart", "Windows bilan birga ishga tushirish", "Language", "Til / Language",
                    "Refresh", "Yangilash", "OpenSite", "namazvakti.com saytini ochish", "Hide", "Yashirish (tray belgisi)", "Exit", "Chiqish",
                    "TrayToggle", "Vidjetni koʻrsatish / yashirish", "Menu", "Menyu",
                    "DlgTitle", "Shaharni tanlash — namazvakti.com", "DlgPrompt", "Shahar yoki aholi punkti nomi:", "Search", "Qidirish",
                    "Choose", "Tanlash", "Cancel", "Bekor qilish", "Min2", "Kamida 2 ta harf", "Searching", "Qidirilmoqda…",
                    "Found", "Topildi: {0}", "NotFound", "Hech narsa topilmadi", "NetError", "Tarmoq xatosi",
                    "Jamaat", "Jamoat", "JamaatMenu", "Jamoat vaqti…", "JamaatTitle", "Jamoat vaqti",
                    "JamaatHint", "Masjiddagi jamoat vaqtini SS:DD koʻrinishida kiriting. Boʻsh qolsa — jamoat haqida eslatma boʻlmaydi.",
                    "JamaatBefore", "Oldindan eslatish:", "JamaatNoBefore", "Faqat jamoat vaqtida", "JamaatBeforeItem", "{0} daqiqa oldin",
                    "JamaatMsg", "Jamoat namozi boshlanmoqda", "JamaatBeforeMsg", "Jamoatga {0} daqiqa qoldi",
                    "Juma", "Juma namozi (juma kuni)", "Azan", "azon {0}", "Save", "Saqlash", "BadTime", "Notoʻgʻri vaqt: {0}", "ByAzan", "Azon boʻyicha",
                    "NextJamaat", "Jamoat: {0} {1}", "ShowTimes", "Namoz vaqtlarini koʻrsatish", "HideTimes", "Namoz vaqtlarini yashirish")
            });
            All.Add(new Lang {
                Code = "ky", Label = "Кыргызча", Culture = "ky-KG",
                Prayers = Map("imsak", "Имсак", "bamdat", "Багымдат", "kun", "Күн чыгышы", "ishraq", "Ишрак", "kerahat", "Керахат",
                              "besin", "Бешим", "asriauual", "Асри аввал", "ekindi", "Аср", "isfirar", "Исфирар",
                              "aqsham", "Шам", "ishtibaq", "Иштибак", "quptan", "Куптан", "ishaisani", "Ишаи сани"),
                Months = new[] { "Мухаррам", "Сафар", "Рабиул-аввал", "Рабиул-ахир", "Жумадал-ула", "Жумадал-ахира",
                                 "Ражаб", "Шаабан", "Рамазан", "Шаввал", "Зулкаада", "Зулхижжа" },
                Text = Map(
                    "Position", "Жайгашуу", "PosBR", "Оң ылдыйкы бурч", "PosTR", "Оң жогорку бурч", "PosBL", "Сол ылдыйкы бурч", "PosTL", "Сол жогорку бурч", "PosFree", "Эркин (каалаган жерге сүйрөп койсо болот)",
                    "FirstRunTitle", "Өлкөнү жана шаарды тандаңыз — жадыбал интернетсиз иштейт",
                    "OldYear", "Жаңы жылдын жадыбалы азырынча жүктөлө элек — өткөн жылдын убакыттары көрсөтүлдү (±1–2 мүн)",
                    "Transparency", "Тунуктук", "TransLow", "Төмөн", "TransMid", "Орточо", "TransHigh", "Жогору", "TransMax", "Эң жогору",
                    "DockCorner", "Жогорку оң бурчка бекитүү",
                    "Minimize", "Трейге жашыруу", "TrayHintTitle", "Виджет трейге жашырылды", "TrayHint", "Сааттын жанындагы трейдеги жарым ай белгисин басыңыз (көрүнбөсө, адегенде ˄ басыңыз) же NamazWidget'ти кайра ачыңыз.",
                    "Browse", "Же тизмеден тандаңыз:", "Country", "Өлкө", "Region", "Аймак", "ListLoading", "Тизме жүктөлүүдө…",
                    "Karahat", "Керахат", "TimesTitle", "Намаз убакыттары",
                    "HijriLabel", "Хижрий", "MiladiLabel", "Милади", "MiladiFormat", "{0}-{1}, {2}-ж.",
                    "Until", "«{0}» убактысына чейин калды", "HM", "{0} саат {1} мүн", "M", "{0} мүн",
                    "Loading", "Жүктөлүүдө…", "TrayTip", "Кийинки намаз: {0}, {1}", "TrayDefault", "Намаз убактысы — namazvakti.com",
                    "Offline", "namazvakti.com менен байланыш жок — {0} күнүнүн убакыттары көрсөтүлдү",
                    "OfflineNoData", "namazvakti.com менен байланыш жок. Бир мүнөттөн кийин кайра аракет кылынат…",
                    "NotifyAtMsg", "Намаз убактысы кирди", "NotifyBeforeMsg", "Башталышына {0} мүнөт калды", "Close", "Жабуу",
                    "ChangeCity", "Шаарды алмаштыруу…", "ShowAll", "Асри аввал жана Ишаи саниди көрсөтүү",
                    "Topmost", "Бардык терезелердин үстүндө",
                    "Notifications", "Билдирмелер", "NotifyAtTime", "Намаз убактысы киргенде билдирүү",
                    "NoBefore", "Алдын ала эскертпөө", "Before", "{0} мүнөт мурун эскертүү",
                    "WinNotify", "Windows билдирмесин да көрсөтүү",
                    "SoundMenu", "Билдирме үнү", "SoundBell", "Коңгуроолор", "SoundSoft", "Жумшак үн", "SoundSystem", "Системалык үн",
                    "TestNotify", "Билдирмени текшерүү",
                    "Autostart", "Windows менен кошо иштетүү", "Language", "Тил / Language",
                    "Refresh", "Жаңылоо", "OpenSite", "namazvakti.com ачуу", "Hide", "Жашыруу (трейдеги белги)", "Exit", "Чыгуу",
                    "TrayToggle", "Виджетти көрсөтүү / жашыруу", "Menu", "Меню",
                    "DlgTitle", "Шаар тандоо — namazvakti.com", "DlgPrompt", "Шаардын же айылдын аталышы:", "Search", "Издөө",
                    "Choose", "Тандоо", "Cancel", "Жокко чыгаруу", "Min2", "Кеминде 2 тамга", "Searching", "Изделүүдө…",
                    "Found", "Табылды: {0}", "NotFound", "Эч нерсе табылган жок", "NetError", "Тармак катасы",
                    "Jamaat", "Жамаат", "JamaatMenu", "Жамаат убактысы…", "JamaatTitle", "Жамаат убактысы",
                    "JamaatHint", "Мечиттеги жамаат убактысын СС:ММ түрүндө жазыңыз. Бош калса — жамаат тууралуу эскертүү болбойт.",
                    "JamaatBefore", "Алдын ала эскертүү:", "JamaatNoBefore", "Жамаат убагында гана", "JamaatBeforeItem", "{0} мүнөт мурун",
                    "JamaatMsg", "Жамаат намазы башталат", "JamaatBeforeMsg", "Жамаатка {0} мүнөт калды",
                    "Juma", "Жума намазы (жума күнү)", "Azan", "азан {0}", "Save", "Сактоо", "BadTime", "Туура эмес убакыт: {0}", "ByAzan", "Азан боюнча",
                    "NextJamaat", "Жамаат: {0} {1}", "ShowTimes", "Намаз убакыттарын көрсөтүү", "HideTimes", "Намаз убакыттарын жашыруу")
            });
            All.Add(new Lang {
                Code = "en", Label = "English", Culture = "en-US",
                Prayers = Map("imsak", "Imsak", "bamdat", "Fajr", "kun", "Sunrise", "ishraq", "Ishraq", "kerahat", "Zawal (karahat)",
                              "besin", "Dhuhr", "asriauual", "Asr (early)", "ekindi", "Asr", "isfirar", "Isfirar",
                              "aqsham", "Maghrib", "ishtibaq", "Ishtibak", "quptan", "Isha", "ishaisani", "Isha (second)"),
                Months = new[] { "Muharram", "Safar", "Rabi al-Awwal", "Rabi al-Akhir", "Jumada al-Ula", "Jumada al-Akhirah",
                                 "Rajab", "Shaban", "Ramadan", "Shawwal", "Dhu al-Qadah", "Dhu al-Hijjah" },
                Text = Map(
                    "Position", "Position", "PosBR", "Bottom-right corner", "PosTR", "Top-right corner", "PosBL", "Bottom-left corner", "PosTL", "Top-left corner", "PosFree", "Free (drag anywhere)",
                    "FirstRunTitle", "Choose your country and city — the schedule will work offline",
                    "OldYear", "New year schedule not downloaded yet — showing last year’s times (±1–2 min)",
                    "Transparency", "Transparency", "TransLow", "Low", "TransMid", "Medium", "TransHigh", "High", "TransMax", "Maximum",
                    "DockCorner", "Pin to top-right corner",
                    "Minimize", "Minimize to tray", "TrayHintTitle", "Widget minimized to the tray", "TrayHint", "Click the crescent icon in the tray next to the clock (if you can't see it, click ˄ first) or start NamazWidget again.",
                    "Browse", "Or pick from the list:", "Country", "Country", "Region", "Region", "ListLoading", "Loading list…",
                    "Karahat", "Karahat", "TimesTitle", "Prayer times",
                    "HijriLabel", "Hijri", "MiladiLabel", "Gregorian", "MiladiFormat", "{1} {0}, {2}",
                    "Until", "Time left until {0}", "HM", "{0} h {1} min", "M", "{0} min",
                    "Loading", "Loading…", "TrayTip", "Next prayer: {0} at {1}", "TrayDefault", "Prayer times — namazvakti.com",
                    "Offline", "No connection to namazvakti.com — showing times for {0}",
                    "OfflineNoData", "No connection to namazvakti.com. Retrying in a minute…",
                    "NotifyAtMsg", "Prayer time has begun", "NotifyBeforeMsg", "Starts in {0} minutes", "Close", "Close",
                    "ChangeCity", "Change city…", "ShowAll", "Show Asr (early) and Isha (second)",
                    "Topmost", "Always on top",
                    "Notifications", "Notifications", "NotifyAtTime", "Notify when prayer time begins",
                    "NoBefore", "No advance reminder", "Before", "Remind {0} minutes before",
                    "WinNotify", "Also show Windows notification",
                    "SoundMenu", "Notification sound", "SoundBell", "Chimes", "SoundSoft", "Soft ding-dong", "SoundSystem", "System sound",
                    "TestNotify", "Test notification",
                    "Autostart", "Start with Windows", "Language", "Language",
                    "Refresh", "Refresh", "OpenSite", "Open namazvakti.com", "Hide", "Hide (tray icon)", "Exit", "Exit",
                    "TrayToggle", "Show / hide widget", "Menu", "Menu",
                    "DlgTitle", "Choose city — namazvakti.com", "DlgPrompt", "City or settlement name (Latin or Cyrillic):", "Search", "Search",
                    "Choose", "Choose", "Cancel", "Cancel", "Min2", "At least 2 letters", "Searching", "Searching…",
                    "Found", "Found: {0}", "NotFound", "Nothing found", "NetError", "Network error",
                    "Jamaat", "Jama'ah", "JamaatMenu", "Jama'ah times…", "JamaatTitle", "Jama'ah times",
                    "JamaatHint", "Enter the mosque's jama'ah (congregation) time as HH:MM. Leave empty for no jama'ah reminder.",
                    "JamaatBefore", "Remind in advance:", "JamaatNoBefore", "Only at jama'ah time", "JamaatBeforeItem", "{0} min before",
                    "JamaatMsg", "Jama'ah prayer is starting", "JamaatBeforeMsg", "Jama'ah starts in {0} minutes",
                    "Juma", "Jumu'ah (Friday)", "Azan", "adhan {0}", "Save", "Save", "BadTime", "Invalid time: {0}", "ByAzan", "At adhan",
                    "NextJamaat", "Jama'ah: {0} {1}", "ShowTimes", "Show prayer times", "HideTimes", "Hide prayer times")
            });
        }

        public static Lang Find(string code)
        {
            return All.FirstOrDefault(l => l.Code == code);
        }

        public static string DefaultCode()
        {
            switch (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName)
            {
                case "kk": return "kz";
                case "uz": return "uz";
                case "ky": return "ky";
                case "en": return "en";
                default: return "ru";
            }
        }
    }

    // Type — цвет как на namaztimes.kz: Green — намаз, Red — карахат (нежелательное время), Orange — ишрак.
    // Extra — показывать только при «показывать все времена». Tracked — для уведомлений.
    sealed class PrayerDef
    {
        public readonly string Key, Type;
        public readonly bool Extra, Tracked;
        public PrayerDef(string key, string type, bool extra, bool tracked) { Key = key; Type = type; Extra = extra; Tracked = tracked; }
    }

    // Периоды прогресс-бара — копия логики namaztimes.kz: Start — начало периода, From/To — границы полосы и отсчёта.
    sealed class PeriodDef
    {
        public readonly string Start, Color, From, To;
        public PeriodDef(string start, string color, string from, string to) { Start = start; Color = color; From = from; To = to; }
    }

    sealed class Ev
    {
        public string Key, Name, Type;
        public bool Extra, Tracked;
        public DateTime Time;
    }

    sealed class Period
    {
        public string Key, Name, Color, NextName, NextKey;
        public string CompactKey;   // для карточки: только семь времён
        public DateTime From, To;
    }

    sealed class RowUi
    {
        public Border Border;
        public TextBlock Name, Time, Jamaat;
    }

    sealed class Settings
    {
        public int CityId = 8408, CountryId = 99;
        public string CityName = "Almaty", CityRegion = "Almaty", CountryName = "Kazakhstan", Lang = "", Sound = "bell";
        public int TrayHints;
        public bool CityChosen;   // первый запуск: страну и город выбирают один раз, дальше всё работает без интернета
        public bool DockTopRight;
        public string Position = "BR";   // BR, TR, BL, TL — угол экрана; Free — куда перетащили (Left/Top)
        public int Transparency = 2;   // 0 — низкая … 3 — максимальная
        public double? Left, Top;
        public bool Topmost, ShowAll, Notify = true, WinNotify = true;
        public int NotifyBefore = 5;
        // время жамагата, вводится вручную: ключ намаза (или "juma") → "HH:mm"
        public Dictionary<string, string> Jamaat = new Dictionary<string, string>();
        public int JamaatBefore = 10;
        // намазы, которые читают по азану: время жамагата = время намаза
        public HashSet<string> JamaatAzan = new HashSet<string>();
    }

    // «Стекло»: размытие того, что под окном, средствами Windows.
    // Windows 11 22H2+ — системный Acrylic (DWM), Windows 10 — акриловый акцент, иначе — непрозрачная подложка.
    static class Glass
    {
        [StructLayout(LayoutKind.Sequential)] struct AccentPolicy { public int State, Flags, GradientColor, AnimationId; }
        [StructLayout(LayoutKind.Sequential)] struct CompositionData { public int Attribute; public IntPtr Data; public int Size; }

        [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
        [DllImport("user32.dll")] static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref CompositionData data);
        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hwnd, int index, int value);
        const int GWL_STYLE = -16, WS_SYSMENU = 0x80000, GWL_EXSTYLE = -20, WS_EX_NOACTIVATE = 0x08000000;
        const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20, DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        const int DWMWCP_ROUND = 2;
        const int WCA_ACCENT_POLICY = 19, ACCENT_ENABLE_TRANSPARENTGRADIENT = 2, ACCENT_ENABLE_BLURBEHIND = 3, ACCENT_ENABLE_ACRYLICBLURBEHIND = 4;

        static int WindowsBuild
        {
            get
            {
                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                        return key == null ? 0 : int.Parse(Convert.ToString(key.GetValue("CurrentBuildNumber"), CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
                }
                catch (Exception) { return 0; }
            }
        }

        // окно не забирает фокус при нажатии (компактная карточка: клик по ней открывает полный вид)
        public static void NoActivate(Window window)
        {
            window.SourceInitialized += (s, e) =>
            {
                IntPtr hwnd = new WindowInteropHelper(window).Handle;
                SetWindowLong(hwnd, GWL_EXSTYLE, GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_NOACTIVATE);
            };
        }

        // переключить размытие у уже открытого окна (Windows 11)
        public static void SetBlur(Window window, bool blur)
        {
            IntPtr hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero || WindowsBuild < 22000 || !TransparencyEnabled) return;
            SetGlass(hwnd, blur);
        }

        // размытие с почти прозрачным оттенком (ABGR) или прозрачное стекло без размытия
        static bool SetGlass(IntPtr hwnd, bool blur)
        {
            return blur ? SetAccent(hwnd, ACCENT_ENABLE_ACRYLICBLURBEHIND, unchecked((int)0x10201E1C))
                        : SetAccent(hwnd, ACCENT_ENABLE_TRANSPARENTGRADIENT, 0);
        }

        static bool SetAccent(IntPtr hwnd, int state, int gradient)
        {
            var accent = new AccentPolicy { State = state, Flags = 2, GradientColor = gradient };
            int size = Marshal.SizeOf(accent);
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(accent, ptr, false);
                var data = new CompositionData { Attribute = WCA_ACCENT_POLICY, Data = ptr, Size = size };
                return SetWindowCompositionAttribute(hwnd, ref data) != 0;
            }
            finally { Marshal.FreeHGlobal(ptr); }
        }

        // Параметры → Персонализация → Цвета → «Эффекты прозрачности»
        static bool TransparencyEnabled
        {
            get
            {
                try
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                    {
                        object v = key == null ? null : key.GetValue("EnableTransparency");
                        return !(v is int) || (int)v != 0;
                    }
                }
                catch (Exception) { return true; }
            }
        }

        // opaque — чем залить поверхность, если размытие недоступно
        // blur = false — прозрачное стекло без размытия (уровень «Максимальная»)
        public static void Apply(Window window, Border surface, Brush opaque, bool blur = true)
        {
            window.SourceInitialized += (s, e) =>
            {
                bool ok = false;
                try
                {
                    IntPtr hwnd = new WindowInteropHelper(window).Handle;
                    HwndSource.FromHwnd(hwnd).CompositionTarget.BackgroundColor = Colors.Transparent;
                    // без системного меню Windows не рисует кнопку «×» поверх стекла
                    SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
                    int build = WindowsBuild;
                    if (!TransparencyEnabled) ok = false;   // в Windows выключены «Эффекты прозрачности»
                    else if (build >= 22000)
                    {
                        // Windows 11: скруглённые углы, тёмная рамка; стекло — размытие с едва заметным оттенком
                        // (тонирует сам виджет) или совсем прозрачное. Не зависит от того, активно ли окно.
                        int dark = 1, round = DWMWCP_ROUND;
                        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, 4);
                        DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref round, 4);
                        ok = SetGlass(hwnd, blur);
                    }
                    else if (build >= 10240)
                    {
                        // Windows 10: размытие с лёгким тёмным оттенком (ABGR)
                        ok = SetAccent(hwnd, build >= 17134 ? ACCENT_ENABLE_ACRYLICBLURBEHIND : ACCENT_ENABLE_BLURBEHIND, unchecked((int)0x66201E1C));
                    }
                }
                catch (Exception) { ok = false; }
                if (!ok) { surface.Background = opaque; surface.Tag = "opaque"; }
            };
            // SizeToContent с WindowChrome при первом показе учитывает рамку — пересчитать размер
            window.ContentRendered += (s, e) =>
            {
                var mode = window.SizeToContent;
                window.SizeToContent = SizeToContent.Manual;
                window.SizeToContent = mode;
            };
        }
    }

    // Названия стран, регионов и городов на языке виджета.
    // Сайт отдаёт их только латиницей: страны переводим по международному справочнику Windows (ICU/CLDR, Windows 10 1903+),
    // регионы и города стран СНГ — транслитерацией в кириллицу (на сайте это латинская запись русских/местных названий).
    static class Names
    {
        [DllImport("icu.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        static extern int uloc_getDisplayCountry([MarshalAs(UnmanagedType.LPStr)] string locale, [MarshalAs(UnmanagedType.LPStr)] string displayLocale,
                                                 [Out] char[] country, int capacity, ref int status);

        static bool icuMissing;
        static Dictionary<string, string> codeByName;

        // ICU: название страны по коду ISO на языке lang (ru, kk, ky, uz, en)
        static string IcuCountry(string code, string lang)
        {
            if (icuMissing) return null;
            try
            {
                var buf = new char[128];
                int status = 0;
                int n = uloc_getDisplayCountry("_" + code, lang, buf, buf.Length, ref status);
                return status > 0 || n <= 0 || n > buf.Length ? null : new string(buf, 0, n);
            }
            catch (DllNotFoundException) { icuMissing = true; }
            catch (EntryPointNotFoundException) { icuMissing = true; }
            return null;
        }

        static string Key(string name)
        {
            string s = (name ?? "").Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char ch in s)
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(char.ToLowerInvariant(ch));
            s = Regex.Replace(sb.ToString().Replace("&", " and ").Replace("saint", "st").Replace("st.", "st"), @"\(.*?\)|,.*$", "");
            return Regex.Replace(s, "[^a-z]", "");
        }

        // названия стран на сайте, которые не совпадают со справочником
        static readonly Dictionary<string, string> Aliases = new Dictionary<string, string> {
            { "bosniaherzegovina", "BA" }, { "burma", "MM" }, { "canaryislands", "IC" }, { "capeverde", "CV" },
            { "czechrepublic", "CZ" }, { "macau", "MO" }, { "macedonia", "MK" }, { "palestine", "PS" }, { "rusfed", "RU" },
            { "stvincentandthegrenadin", "VC" }, { "southgeorgia", "GS" }, { "swaziland", "SZ" }, { "turkey", "TR" },
            { "virginislands", "VG" }
        };

        public static string CountryCode(string englishName)
        {
            if (string.IsNullOrEmpty(englishName)) return null;
            string key = Key(englishName);
            if (englishName.Contains("To US") && key == "virginislands") return "VI";
            string code;
            if (Aliases.TryGetValue(key, out code)) return code;
            if (codeByName == null)
            {
                var map = new Dictionary<string, string>();
                for (char a = 'A'; a <= 'Z'; a++)
                    for (char b = 'A'; b <= 'Z'; b++)
                    {
                        string c = new string(new[] { a, b });
                        string en = IcuCountry(c, "en");
                        if (en != null && en != c && !map.ContainsKey(Key(en))) map[Key(en)] = c;
                    }
                codeByName = map;
            }
            return codeByName.TryGetValue(key, out code) ? code : null;
        }

        static string IcuLang(string lang) { return lang == "kz" ? "kk" : lang; }

        public static string Country(string englishName, string lang)
        {
            string code = CountryCode(englishName);
            if (code == null) return englishName;
            if (code == "KG" && lang == "ru") return "Кыргызстан";   // в CLDR — «Киргизия»
            string name = IcuCountry(code, IcuLang(lang));
            return name == null || name == code ? englishName : name;   // нет перевода — ICU возвращает сам код
        }

        static readonly HashSet<string> Cis = new HashSet<string> { "KZ", "KG", "RU", "TJ", "TM", "UZ", "BY", "UA" };

        static bool UseCyrillic(string countryCode, string lang)
        {
            return countryCode != null && Cis.Contains(countryCode) && (lang == "ru" || lang == "kz" || lang == "ky");
        }

        // город: «Almaty (Almatı)» → «Алматы», «Bishkek (Frunze)» → «Бишкек (Фрунзе)»
        public static string City(string latin, string countryCode, string lang)
        {
            if (!UseCyrillic(countryCode, lang) || string.IsNullOrEmpty(latin)) return latin;
            var m = Regex.Match(latin, @"^(.*?)\s*\((.*)\)\s*$");
            if (!m.Success) return ToCyrillic(latin, lang);
            string main = ToCyrillic(m.Groups[1].Value, lang), alt = ToCyrillic(m.Groups[2].Value, lang);
            return string.Equals(main, alt, StringComparison.CurrentCultureIgnoreCase) ? main : main + " (" + alt + ")";
        }

        // регион: пояснение в скобках убираем — «Aktyubinsk (Yuzhnyy Ural)» → «Актюбинск»
        public static string Region(string latin, string countryCode, string lang)
        {
            if (!UseCyrillic(countryCode, lang) || string.IsNullOrEmpty(latin)) return latin;
            string text = Regex.Replace(latin, @"\s*\(.*?\)", "").Trim();
            return ToCyrillic(text.Length > 0 ? text : latin, lang);
        }

        static readonly Dictionary<string, string[]> Words = new Dictionary<string, string[]> {
            // ru, kz, ky
            { "north", new[] { "Северный", "Солтүстік", "Түндүк" } }, { "kuzey", new[] { "Северный", "Солтүстік", "Түндүк" } },
            { "south", new[] { "Южный", "Оңтүстік", "Түштүк" } }, { "east", new[] { "Восточный", "Шығыс", "Чыгыш" } },
            { "west", new[] { "Западный", "Батыс", "Батыш" } }, { "region", new[] { "регион", "аймақ", "аймак" } },
            { "resp", new[] { "Респ", "Респ", "Респ" } }
        };

        const string Vowels = "аеёиоуыэюяәөүұіАЕЁИОУЫЭЮЯӘӨҮҰІ";

        public static string ToCyrillic(string text, string lang)
        {
            int li = lang == "kz" ? 1 : lang == "ky" ? 2 : 0;
            bool ru = li == 0, kz = li == 1;
            var output = new StringBuilder();
            foreach (Match word in Regex.Matches(text, @"[\p{L}'’\u0092]+|[^\p{L}'’\u0092]+"))
            {
                string w = word.Value;
                string[] tr;
                if (Words.TryGetValue(w.ToLowerInvariant().TrimEnd('.'), out tr)) { output.Append(tr[li]); continue; }
                if (!char.IsLetter(w[0])) { output.Append(w); continue; }
                string lower = w.ToLowerInvariant();
                var sb = new StringBuilder();
                int i = 0;
                while (i < lower.Length)
                {
                    string rep = null;
                    int len = 1;
                    string rest = lower.Substring(i);
                    char prev = sb.Length > 0 ? sb[sb.Length - 1] : ' ';
                    foreach (var pair in new[] {
                        new[] { "shch", "щ" }, new[] { "dzh", "дж" }, new[] { "sh", "ш" }, new[] { "ch", "ч" }, new[] { "zh", "ж" },
                        new[] { "kh", "х" }, new[] { "tsk", "тск" }, new[] { "ts", "ц" }, new[] { "gh", kz ? "ғ" : "г" }, new[] { "yu", "ю" }, new[] { "ya", "я" },
                        new[] { "yo", "ё" }, new[] { "ye", "е" } })
                        if (rest.StartsWith(pair[0], StringComparison.Ordinal)) { rep = pair[1]; len = pair[0].Length; break; }
                    if (rep == null)
                    {
                        char c = lower[i];
                        switch (c)
                        {
                            case 'a': case 'â': case 'á': rep = "а"; break;
                            case 'ä': rep = kz ? "ә" : "а"; break;
                            case 'b': rep = "б"; break;
                            case 'c': rep = "ц"; break;
                            case 'ç': rep = "ч"; break;
                            case 'd': rep = "д"; break;
                            case 'e': case 'é': rep = "е"; break;
                            case 'f': rep = "ф"; break;
                            case 'g': case 'ğ': rep = "г"; break;
                            case 'h': rep = "х"; break;
                            case 'i': case 'í': case 'î': rep = "и"; break;
                            case 'ı': rep = "ы"; break;
                            case 'j': rep = ru ? "дж" : "ж"; break;
                            case 'k': rep = "к"; break;
                            case 'l': rep = "л"; break;
                            case 'm': rep = "м"; break;
                            case 'n': rep = "н"; break;
                            case 'o': case 'ô': rep = "о"; break;
                            case 'ö': rep = ru ? "о" : "ө"; break;
                            case 'p': rep = "п"; break;
                            case 'q': rep = kz ? "қ" : "к"; break;
                            case 'r': rep = "р"; break;
                            case 's': rep = "с"; break;
                            case 'ş': rep = "ш"; break;
                            case 't': rep = "т"; break;
                            case 'u': case 'û': rep = "у"; break;
                            case 'ü': rep = ru ? "у" : "ү"; break;
                            case 'v': case 'w': rep = "в"; break;
                            case 'x': rep = "кс"; break;
                            case 'z': rep = "з"; break;
                            case 'y':
                                // после гласной — «й» (Altayskiy → Алтайский), иначе — «ы» (Kyzyl → Кызыл)
                                rep = Vowels.IndexOf(prev) >= 0 ? "й" : "ы"; break;
                            case '\'': case '’': case '\u0092':
                                // мягкий знак: Arkhangel'sk → Архангельск, Oblast' → Область
                                rep = sb.Length > 0 && Vowels.IndexOf(prev) < 0 ? "ь" : ""; break;
                            default: rep = c.ToString(); break;
                        }
                    }
                    if (rep.Length > 0 && char.IsUpper(w[i])) rep = char.ToUpperInvariant(rep[0]) + rep.Substring(1);
                    sb.Append(rep);
                    i += len;
                }
                output.Append(sb);
            }
            return output.ToString();
        }
    }

    sealed class Widget
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) NamazTimesWidget/2.0";
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string RunName = "NamazTimesWidget";
        const double EndWarningMinutes = 15;   // последние минуты намаза — полоса красная
        static readonly int[] BeforeOptions = { 3, 5, 10, 15 };
        static readonly int[] JamaatBeforeOptions = { 3, 5, 10, 15, 20, 30 };
        static readonly string[] JamaatKeys = { "bamdat", "besin", "juma", "ekindi", "aqsham", "quptan" };

        static readonly PrayerDef[] Prayers = {
            new PrayerDef("imsak", "Red", false, false),     new PrayerDef("bamdat", "Green", false, true),
            new PrayerDef("kun", "Red", false, true),        new PrayerDef("ishraq", "Orange", false, false),
            new PrayerDef("kerahat", "Red", false, false),   new PrayerDef("besin", "Green", false, true),
            new PrayerDef("asriauual", "Orange", true, false), new PrayerDef("ekindi", "Green", false, true),
            new PrayerDef("isfirar", "Red", false, false),   new PrayerDef("aqsham", "Green", false, true),
            new PrayerDef("ishtibaq", "Red", false, false),  new PrayerDef("quptan", "Green", false, true),
            new PrayerDef("ishaisani", "Green", true, false)
        };

        static readonly PeriodDef[] PeriodDefs = {
            new PeriodDef("imsak", "Red", "imsak", "bamdat"),     new PeriodDef("bamdat", "Green", "bamdat", "kun"),
            new PeriodDef("kun", "Red", "kun", "besin"),          new PeriodDef("ishraq", "Orange", "kun", "besin"),
            new PeriodDef("kerahat", "Red", "kun", "besin"),      new PeriodDef("besin", "Green", "besin", "ekindi"),
            new PeriodDef("ekindi", "Green", "ekindi", "aqsham"), new PeriodDef("isfirar", "Red", "ekindi", "aqsham"),
            new PeriodDef("aqsham", "Green", "aqsham", "quptan"), new PeriodDef("ishtibaq", "Red", "aqsham", "quptan"),
            new PeriodDef("quptan", "Green", "quptan", "imsak")
        };

        // Цвета прогресс-бара и таблицы namaztimes.kz (Bootstrap table-success / table-danger / table-warning)
        static readonly Dictionary<string, SolidColorBrush> Brushes = new Dictionary<string, SolidColorBrush> {
            { "Green", B("#30D158") }, { "Blue", B("#0A84FF") }, { "Red", B("#FF453A") }, { "Orange", B("#FF9F0A") },
            { "RowGreen", B("#3830D158") }, { "RowRed", B("#38FF453A") }, { "RowOrange", B("#38FF9F0A") },
            { "TextGreen", B("#4AE27A") }, { "TextRed", B("#FF7A72") }, { "TextOrange", B("#FFBE55") },
            { "Inactive", B("#98989D") },
            // тёмные варианты цветов для текста — контраст не ниже 4.5:1 на белом
            { "InkGreen", B("#30D158") }, { "InkBlue", B("#409CFF") }, { "InkRed", B("#FF6961") }, { "InkOrange", B("#FFB340") }
        };
        static readonly SolidColorBrush JamaatInk = B("#64B5FF");

        static SolidColorBrush B(string hex)
        {
            var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex);
            brush.Freeze();
            return brush;
        }

#pragma warning disable 649   // задаются только в самопроверке
        public DateTime? FakeNow;   // только для самопроверки
        public bool TestMode;       // самопроверка: окна не показываются, звук не играет
#pragma warning restore 649
        DateTime Now { get { return FakeNow.HasValue ? FakeNow.Value : DateTime.Now; } }

        readonly string appDir, settingsPath, cachePath;
        readonly Settings settings = new Settings();
        readonly JavaScriptSerializer json = new JavaScriptSerializer();
        readonly Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        Dictionary<DateTime, string[]> yearDays;   // расписание на год: дата → 14 времён
        int dataYear;          // за какой год скачано расписание
        bool serverOldYear;    // сайт ещё отдаёт прошлый год (1 января) — повторяем раз в час
        string yearXml, cityNameEn = "", stateEn = "", hijriFor = "";
        int countryId, hijriOffset;
        bool fetching, fetchError;
        int fetchToken;
        DateTime lastAttempt = DateTime.MinValue;
        readonly HashSet<string> notified = new HashSet<string>();
        string trayText = "";

        Window window;
        readonly Dictionary<string, RowUi> rows = new Dictionary<string, RowUi>();
        WinForms.NotifyIcon tray;
        Window popup;
        DispatcherTimer popupTimer, timer;
        SoundPlayer player;
        public Window LastPopup { get { return popup; } }
        public Window MainWindow { get { return window; } }
        public bool IsFetching { get { return fetching; } }

        public Widget(string appDir)
        {
            this.appDir = appDir;
            Directory.CreateDirectory(appDir);
            settingsPath = Path.Combine(appDir, "settings.json");
            cachePath = Path.Combine(appDir, "cache.json");
        }

        Lang L { get { return I18n.Find(settings.Lang) ?? I18n.All[1]; } }
        string T(string key) { string v; return L.Text.TryGetValue(key, out v) ? v : key; }

        CultureInfo LangCulture
        {
            get
            {
                try { return CultureInfo.GetCultureInfo(L.Culture); }
                catch (ArgumentException) { return CultureInfo.GetCultureInfo("en-US"); }
            }
        }

        string Capital(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Substring(0, 1).ToUpper(LangCulture) + text.Substring(1);
        }

        T2 Find<T2>(DependencyObject root, string name) where T2 : class
        {
            var fe = root as FrameworkElement;
            return fe == null ? null : fe.FindName(name) as T2;
        }

        // ---------- настройки и кэш ----------
        static object Get(Dictionary<string, object> d, string key)
        {
            object v;
            return d != null && d.TryGetValue(key, out v) ? v : null;
        }
        static string Str(Dictionary<string, object> d, string key) { var v = Get(d, key); return v == null ? null : Convert.ToString(v, Inv); }
        static Dictionary<string, object> Obj(Dictionary<string, object> d, string key) { return Get(d, key) as Dictionary<string, object>; }

        // «13:15», «13.15», «1315», «9 05» → «13:15» / «09:05»; пусто или ошибка → null
        static string NormalizeTime(string text)
        {
            var m = Regex.Match(text ?? "", @"^\s*(\d{1,2})\s*[:.,\s]?\s*(\d{2})\s*$");
            if (!m.Success) return null;
            int h = int.Parse(m.Groups[1].Value, Inv), min = int.Parse(m.Groups[2].Value, Inv);
            return h < 24 && min < 60 ? string.Format(Inv, "{0:00}:{1:00}", h, min) : null;
        }

        // время жамагата для намаза сегодня (в пятницу вместо Бесин — Жума, если задана)
        DateTime? JamaatTime(Ev ev)
        {
            if (!ev.Tracked || ev.Key == "kun") return null;
            string key = ev.Key, hm;
            if (key == "besin" && ev.Time.DayOfWeek == DayOfWeek.Friday && HasJamaat("juma")) key = "juma";
            if (settings.JamaatAzan.Contains(key)) return ev.Time;
            if (!settings.Jamaat.TryGetValue(key, out hm)) return null;
            return ev.Time.Date.AddHours(int.Parse(hm.Substring(0, 2), Inv)).AddMinutes(int.Parse(hm.Substring(3, 2), Inv));
        }

        bool HasJamaat(string key) { return settings.Jamaat.ContainsKey(key) || settings.JamaatAzan.Contains(key); }

        // намаз читают по азану (для Бесин в пятницу — смотрим Жума, если она задана)
        bool JamaatByAzan(Ev ev)
        {
            string key = ev.Key == "besin" && ev.Time.DayOfWeek == DayOfWeek.Friday && HasJamaat("juma") ? "juma" : ev.Key;
            return settings.JamaatAzan.Contains(key);
        }

        string JamaatName(Ev ev)
        {
            return ev.Key == "besin" && ev.Time.DayOfWeek == DayOfWeek.Friday && HasJamaat("juma")
                ? Regex.Replace(T("Juma"), @"\s*\(.*$", "") : ev.Name;
        }

        void LoadSettings()
        {
            try
            {
                if (File.Exists(settingsPath))
                {
                    var d = json.DeserializeObject(File.ReadAllText(settingsPath, Encoding.UTF8)) as Dictionary<string, object>;
                    if (d != null)
                    {
                        object v;
                        if ((v = Get(d, "CityId")) != null) settings.CityId = Convert.ToInt32(v, Inv);
                        if ((v = Get(d, "CountryId")) != null) settings.CountryId = Convert.ToInt32(v, Inv);
                        if ((v = Get(d, "CityName")) != null) settings.CityName = Convert.ToString(v, Inv);
                        if ((v = Get(d, "CityRegion")) != null) settings.CityRegion = Convert.ToString(v, Inv);
                        if ((v = Get(d, "CountryName")) != null) settings.CountryName = Convert.ToString(v, Inv);
                        else if (settings.CityRegion.Contains(", "))
                        {
                            // прежний формат: «Регион, Страна» в одном поле
                            int comma = settings.CityRegion.LastIndexOf(", ", StringComparison.Ordinal);
                            settings.CountryName = settings.CityRegion.Substring(comma + 2);
                            settings.CityRegion = settings.CityRegion.Substring(0, comma);
                        }
                        if ((v = Get(d, "TrayHints")) != null) settings.TrayHints = Convert.ToInt32(v, Inv);
                        if (Get(d, "CityChosen") is bool) settings.CityChosen = (bool)Get(d, "CityChosen");
                        if ((v = Get(d, "Lang")) != null) settings.Lang = Convert.ToString(v, Inv);
                        if ((v = Get(d, "Sound")) != null) settings.Sound = Convert.ToString(v, Inv);
                        if ((v = Get(d, "Left")) != null) settings.Left = Convert.ToDouble(v, Inv);
                        if ((v = Get(d, "Top")) != null) settings.Top = Convert.ToDouble(v, Inv);
                        if (Get(d, "Topmost") is bool) settings.Topmost = (bool)Get(d, "Topmost");
                        if (Get(d, "ShowAll") is bool) settings.ShowAll = (bool)Get(d, "ShowAll");
                        if (Get(d, "DockTopRight") is bool) settings.DockTopRight = (bool)Get(d, "DockTopRight");
                        if ((v = Get(d, "Position")) != null) settings.Position = Convert.ToString(v, Inv);
                        if ((v = Get(d, "Transparency")) != null) settings.Transparency = Math.Max(0, Math.Min(3, Convert.ToInt32(v, Inv)));
                        if (Get(d, "Notify") is bool) settings.Notify = (bool)Get(d, "Notify");
                        if (Get(d, "WinNotify") is bool) settings.WinNotify = (bool)Get(d, "WinNotify");
                        if ((v = Get(d, "NotifyBefore")) != null) settings.NotifyBefore = Convert.ToInt32(v, Inv);
                        if ((v = Get(d, "JamaatBefore")) != null) settings.JamaatBefore = Convert.ToInt32(v, Inv);
                        var azanKeys = Get(d, "JamaatAzan") as System.Collections.IEnumerable;
                        if (azanKeys != null)
                            foreach (var k in azanKeys)
                                if (JamaatKeys.Contains(Convert.ToString(k, Inv))) settings.JamaatAzan.Add(Convert.ToString(k, Inv));
                        var jamaat = Obj(d, "Jamaat");
                        if (jamaat != null)
                            foreach (var key in JamaatKeys)
                            {
                                string hm = NormalizeTime(Str(jamaat, key));
                                if (hm != null) settings.Jamaat[key] = hm;
                            }
                    }
                }
            }
            catch (Exception) { }
            if (I18n.Find(settings.Lang) == null) settings.Lang = I18n.DefaultCode();
            if (settings.NotifyBefore != 0 && !BeforeOptions.Contains(settings.NotifyBefore)) settings.NotifyBefore = 0;
            if (settings.Sound != "bell" && settings.Sound != "soft" && settings.Sound != "system") settings.Sound = "bell";
            if (settings.JamaatBefore != 0 && !JamaatBeforeOptions.Contains(settings.JamaatBefore)) settings.JamaatBefore = 10;
            if (!Positions.Contains(settings.Position)) settings.Position = "BR";

            // кэш: годовое расписание города — виджет работает без интернета до конца года
            try
            {
                if (!File.Exists(cachePath)) return;
                var c = json.DeserializeObject(File.ReadAllText(cachePath, Encoding.UTF8)) as Dictionary<string, object>;
                if (c == null || Str(c, "CityId") != settings.CityId.ToString(Inv)) return;
                string cachedXml = Str(c, "Xml");
                object cachedYear = Get(c, "Year");
                ParseYear(cachedXml, cachedYear != null ? Convert.ToInt32(cachedYear, Inv) : YearFromXml(cachedXml) ?? DateTime.Now.Year);
                hijriFor = Str(c, "HijriFor") ?? "";
                object off = Get(c, "HijriOffset");
                if (off != null) hijriOffset = Convert.ToInt32(off, Inv);
            }
            catch (Exception) { }
        }

        void SaveSettings()
        {
            try
            {
                var d = new Dictionary<string, object> {
                    { "CityId", settings.CityId }, { "CountryId", settings.CountryId }, { "CityName", settings.CityName },
                    { "CityRegion", settings.CityRegion }, { "CountryName", settings.CountryName }, { "TrayHints", settings.TrayHints }, { "CityChosen", settings.CityChosen },
                    { "Lang", settings.Lang }, { "Sound", settings.Sound },
                    { "Left", settings.Left }, { "Top", settings.Top }, { "Topmost", settings.Topmost },
                    { "ShowAll", settings.ShowAll }, { "DockTopRight", settings.DockTopRight }, { "Position", settings.Position }, { "Transparency", settings.Transparency }, { "Notify", settings.Notify }, { "WinNotify", settings.WinNotify },
                    { "NotifyBefore", settings.NotifyBefore }, { "JamaatBefore", settings.JamaatBefore },
                    { "Jamaat", settings.Jamaat }, { "JamaatAzan", settings.JamaatAzan.ToList() }
                };
                File.WriteAllText(settingsPath, json.Serialize(d), new UTF8Encoding(false));
            }
            catch (Exception) { }
        }

        void SaveCache()
        {
            try
            {
                var d = new Dictionary<string, object> {
                    { "CityId", settings.CityId }, { "Xml", yearXml }, { "Year", dataYear }, { "HijriFor", hijriFor }, { "HijriOffset", hijriOffset } };
                File.WriteAllText(cachePath, json.Serialize(d), new UTF8Encoding(false));
            }
            catch (Exception) { }
        }

        // ---------- данные (namazvakti.com) ----------
        // XML.php отдаёт времена на весь год: 14 значений в день через табуляцию.
        // Порядок столбцов на сайте: İmsâk, Sabâh, Güneş, İşrak, Kerâhet, Öğle, İkindi (асри аввал), Asr-ı sânî,
        // İsfirâr, Akşam, İştibâk, Yatsı, İşâ-i sânî, Kıble sâati.
        static readonly string[] XmlColumns = {
            "imsak", "bamdat", "kun", "ishraq", "kerahat", "besin", "asriauual", "ekindi",
            "isfirar", "aqsham", "ishtibaq", "quptan", "ishaisani" };

        const string SiteRoot = "https://namazvakti.com/";

        // год в комментарии файла (<!--2026-->) есть не у всех городов
        static int? YearFromXml(string xml)
        {
            var m = Regex.Match(xml ?? "", @"<!--\s*(\d{4})\s*-->");
            return m.Success ? int.Parse(m.Groups[1].Value, Inv) : (int?)null;
        }

        // Файл покрывает 31 декабря прошлого года (dayofyear=0), весь год и 1–2 января следующего
        bool ParseYear(string xml, int y)
        {
            if (string.IsNullOrEmpty(xml)) return false;
            var doc = new System.Xml.XmlDocument();
            doc.LoadXml(xml);
            var root = doc.DocumentElement;
            if (root == null || root.Name != "cityinfo") return false;
            var days = new Dictionary<DateTime, string[]>();
            foreach (System.Xml.XmlElement row in root.GetElementsByTagName("prayertimes"))
            {
                int day, month;
                if (!int.TryParse(row.GetAttribute("day"), NumberStyles.Integer, Inv, out day) ||
                    !int.TryParse(row.GetAttribute("month"), NumberStyles.Integer, Inv, out month)) continue;
                int dayOfYear;
                int.TryParse(row.GetAttribute("dayofyear"), NumberStyles.Integer, Inv, out dayOfYear);
                int rowYear = dayOfYear == 0 ? y - 1 : (month == 1 && dayOfYear > 300 ? y + 1 : y);
                try { days[new DateTime(rowYear, month, day)] = row.InnerText.Split('\t'); }
                catch (ArgumentOutOfRangeException) { }
            }
            if (days.Count == 0) return false;
            yearDays = days;
            yearXml = xml;
            dataYear = y;
            cityNameEn = root.GetAttribute("cityNameEN");
            stateEn = root.GetAttribute("cityStateEN");
            int country;
            if (int.TryParse(root.GetAttribute("countryID"), NumberStyles.Integer, Inv, out country)) countryId = country;
            return true;
        }

        // Строка расписания на дату. Если расписание нового года ещё не скачано (нет интернета 1 января),
        // берём тот же день из скачанного года: время намаза от года к году меняется на 1–2 минуты.
        string[] RowFor(DateTime date, out bool approximate)
        {
            approximate = false;
            string[] row;
            if (yearDays == null) return null;
            if (yearDays.TryGetValue(date, out row)) return row;
            if (dataYear <= 0) return null;
            int day = date.Month == 2 && date.Day == 29 && !DateTime.IsLeapYear(dataYear) ? 28 : date.Day;
            if (!yearDays.TryGetValue(new DateTime(dataYear, date.Month, day), out row)) return null;
            approximate = true;
            return row;
        }

        bool HasToday { get { bool approximate; return RowFor(Now.Date, out approximate) != null; } }
        bool TodayApproximate { get { bool approximate; return RowFor(Now.Date, out approximate) != null && approximate; } }
        bool YearStale { get { return dataYear != Now.Year; } }

        List<Ev> GetEvents()
        {
            var list = new List<Ev>();
            bool approximate;
            string[] row = RowFor(Now.Date, out approximate);
            if (row == null) return list;
            var times = new Dictionary<string, string>();
            for (int i = 0; i < XmlColumns.Length && i < row.Length; i++) times[XmlColumns[i]] = row[i].Trim();
            foreach (var p in Prayers)
            {
                string value;
                if (!times.TryGetValue(p.Key, out value)) continue;
                var m = Regex.Match(value, @"^(\d{1,2}):(\d{2})$");
                if (!m.Success) continue;
                list.Add(new Ev {
                    Key = p.Key, Name = L.Prayers[p.Key], Type = p.Type, Extra = p.Extra, Tracked = p.Tracked,
                    Time = Now.Date.AddHours(int.Parse(m.Groups[1].Value, Inv)).AddMinutes(int.Parse(m.Groups[2].Value, Inv))
                });
            }
            return list;
        }

        // В компактной карточке — только Имсак, Фаджр, Восход, Зухр, Аср, Магриб, Иша
        static readonly HashSet<string> ShownKeys = new HashSet<string> { "imsak", "bamdat", "kun", "besin", "ekindi", "aqsham", "quptan" };

        static string ShownKey(string key)
        {
            switch (key)
            {
                case "ishraq": case "kerahat": return "kun";
                case "isfirar": return "ekindi";
                case "ishtibaq": return "aqsham";
                default: return key;
            }
        }

        // Текущий период как на namaztimes.kz (граница — за секунду до начала минуты)
        Period GetPeriod(DateTime now)
        {
            var times = GetEvents().ToDictionary(e => e.Key, e => e.Time);
            if (PeriodDefs.Any(d => !times.ContainsKey(d.Start))) return null;

            PeriodDef current = null;
            foreach (var def in PeriodDefs)
                if (now > times[def.Start].AddSeconds(-1)) current = def;

            DateTime from, to;
            if (current != null)
            {
                from = times[current.From];
                to = current.Start == "quptan" ? times["imsak"].AddDays(1) : times[current.To];
            }
            else
            {
                // после полуночи до имсака — продолжается Иша прошлого дня
                current = PeriodDefs[PeriodDefs.Length - 1];
                from = times["quptan"].AddDays(-1);
                to = times["imsak"];
            }
            // показываем только семь времён: промежуточные периоды называются по намазу, чьё время продолжается
            // (цвет периода сохраняется — красный означает нежелательное время)
            // полный вид показывает все периоды как есть; карточка — только семь времён
            string key = current.Start;
            return new Period { Key = key, Name = L.Prayers[key], Color = current.Color, From = from, To = to,
                                NextName = L.Prayers[current.To], NextKey = current.To, CompactKey = ShownKey(current.Start) };
        }

        void Download(string url, Action<string, Exception> done)
        {
            var wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            wc.Headers[HttpRequestHeader.UserAgent] = UserAgent;
            wc.DownloadStringCompleted += (s, e) =>
            {
                Exception error = e.Error;
                string result = null;
                if (error == null && e.Cancelled) error = new OperationCanceledException();
                if (error == null) result = e.Result;
                wc.Dispose();
                dispatcher.BeginInvoke(new Action(() => done(result, error)));
            };
            wc.DownloadStringAsync(new Uri(url));
        }

        void RestartFetch()
        {
            fetchToken++;
            fetching = false;
            StartFetch();
        }

        string TodayKey { get { return DateTime.Now.ToString("yyyy-MM-dd", Inv); } }

        // 1) годовое расписание (если на сегодня его нет); 2) страница города — область, страна и дата хиджры
        void StartFetch()
        {
            if (fetching) return;
            fetching = true;
            lastAttempt = DateTime.Now;
            int token = ++fetchToken;
            int cityId = settings.CityId;
            Action loadPage = () => Download(SiteRoot + "Main.php?cityID=" + cityId.ToString(Inv) + "&WSLanguage=EN", (html, error) =>
            {
                if (token != fetchToken) return;
                fetching = false;
                if (error == null) ApplyCityPage(html);
                UpdateView();
            });
            if (HasToday && !YearStale) { loadPage(); return; }

            Download(SiteRoot + "XML.php?cityID=" + cityId.ToString(Inv), (text, error) =>
            {
                if (token != fetchToken) return;
                if (error != null || string.IsNullOrEmpty(text)) { fetchError = true; fetching = false; UpdateView(); return; }
                // год узнаём из файла, а если его там нет — из заголовка годовой таблицы сайта «YILLIK VAKİTLER (2026)»
                int? fileYear = YearFromXml(text);
                if (fileYear.HasValue) { ApplyYear(token, text, fileYear.Value, loadPage); return; }
                Download(SiteRoot + "Yearly.php?cityID=" + cityId.ToString(Inv), (html, error2) =>
                {
                    if (token != fetchToken) return;
                    var m = Regex.Match(html ?? "", @"\((20\d\d)\)");
                    int year = m.Success ? int.Parse(m.Groups[1].Value, Inv)
                             : (text == yearXml ? dataYear : DateTime.Now.Year);   // тот же файл — значит, тот же год
                    ApplyYear(token, text, year, loadPage);
                });
            });
        }

        void ApplyYear(int token, string text, int year, Action loadPage)
        {
            {
                bool ok = false;
                try { ok = ParseYear(text, year); } catch (Exception) { }
                if (!ok) { fetchError = true; fetching = false; UpdateView(); return; }
                // сервер в Турции: в полночь по местному времени он ещё может отдавать прошлый год
                serverOldYear = year < DateTime.Now.Year;
                fetchError = false;
                if (!string.IsNullOrEmpty(cityNameEn)) settings.CityName = cityNameEn;
                if (countryId > 0) settings.CountryId = countryId;
                if (string.IsNullOrEmpty(settings.CityRegion)) settings.CityRegion = stateEn;
                SaveSettings();
                SaveCache();
                BuildRows();
                UpdateView();
                loadPage();
            }
        }

        // Поле со страницы города: <div id='sehir'>, <div id='eyaletUlke'>, <span id="hicriTarih">
        static string PageField(string html, string id)
        {
            var m = Regex.Match(html ?? "", @"id=['""]" + id + @"['""][^>]*>([^<]*)<");
            return m.Success ? WebUtility.HtmlDecode(m.Groups[1].Value).Trim() : null;
        }

        // «Almaty / Kazakhstan» → «Almaty, Kazakhstan»
        static string PlaceText(string place)
        {
            return string.Join(", ", (place ?? "").Split('/').Select(p => p.Trim()).Where(p => p.Length > 0));
        }

        void ApplyCityPage(string html)
        {
            try
            {
                var place = (PageField(html, "eyaletUlke") ?? "").Split('/').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
                if (place.Count > 1)
                {
                    settings.CountryName = place[place.Count - 1];
                    settings.CityRegion = string.Join(", ", place.Take(place.Count - 1));
                }

                // день хиджры с сайта («8 Rabi’al-âkhir 1448») → сдвиг относительно календаря Умм аль-Кура
                var hijri = Regex.Match(PageField(html, "hicriTarih") ?? "", @"^(\d{1,2})\s+\D+?\s+(\d{4})$");
                if (hijri.Success)
                {
                    int siteDay = int.Parse(hijri.Groups[1].Value, Inv), siteYear = int.Parse(hijri.Groups[2].Value, Inv);
                    var cal = new UmAlQuraCalendar();
                    for (int k = -2; k <= 2; k++)
                    {
                        DateTime d = DateTime.Now.Date.AddDays(k);
                        if (cal.GetDayOfMonth(d) == siteDay && cal.GetYear(d) == siteYear)
                        {
                            hijriOffset = k;
                            hijriFor = TodayKey;
                            break;
                        }
                    }
                }
                SaveSettings();
                SaveCache();
            }
            catch (Exception) { }
        }

        string[] DateLines()   // день недели, милади, хиджри
        {
            var culture = LangCulture;
            DateTime date = Now.Date;
            string weekday = Capital(date.ToString("dddd", culture));
            string month = L.Code == "uz" ? culture.DateTimeFormat.MonthGenitiveNames[date.Month - 1]
                                          : Capital(culture.DateTimeFormat.MonthNames[date.Month - 1]);
            string miladi = string.Format(T("MiladiFormat"), date.Day, month, date.Year);
            string hijri = "";
            try
            {
                var cal = new UmAlQuraCalendar();
                DateTime h = date.AddDays(hijriOffset);
                hijri = string.Format("{0} {1} {2}", cal.GetDayOfMonth(h), L.Months[cal.GetMonth(h) - 1], cal.GetYear(h));
            }
            catch (ArgumentOutOfRangeException) { }
            return new[] { weekday, miladi, hijri };
        }

        // ---------- окно ----------
        const string MainXaml = @"
<Window xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        Title='namazvakti.com' SizeToContent='WidthAndHeight'
        WindowStyle='SingleBorderWindow' AllowsTransparency='False' Background='Transparent'
        ShowInTaskbar='False' ShowActivated='False' ResizeMode='NoResize' FontFamily='Segoe UI Variable Display, Segoe UI'
        UseLayoutRounding='True' TextOptions.TextFormattingMode='Ideal'>
  <WindowChrome.WindowChrome>
    <WindowChrome CaptionHeight='0' GlassFrameThickness='0' ResizeBorderThickness='0' CornerRadius='0' UseAeroCaptionButtons='False'/>
  </WindowChrome.WindowChrome>
  <Window.Resources>
    <!-- кромка стеклянной капсулы: блик сверху, отражение снизу -->
    <LinearGradientBrush x:Key='CapsuleRim' StartPoint='0,0' EndPoint='0,1'>
      <GradientStop Color='#8CFFFFFF' Offset='0'/>
      <GradientStop Color='#14FFFFFF' Offset='0.5'/>
      <GradientStop Color='#40FFFFFF' Offset='1'/>
    </LinearGradientBrush>
  </Window.Resources>
  <!-- Liquid Glass: прозрачное стекло поверх размытого фона; кромка-«линза» — блик сверху-слева и отражение снизу-справа -->
  <Border x:Name='Surface' CornerRadius='8' BorderThickness='1.5'>
    <Border.Resources>
      <!-- текст на прозрачном стекле: мягкая тень, чтобы читался на любом фоне (только внутри виджета, не в меню) -->
      <Style TargetType='TextBlock'>
        <Setter Property='Effect'>
          <Setter.Value><DropShadowEffect Color='Black' BlurRadius='6' ShadowDepth='0' Opacity='0.55'/></Setter.Value>
        </Setter>
      </Style>
    </Border.Resources>
    <Border.BorderBrush>
      <LinearGradientBrush StartPoint='0,0' EndPoint='1,1'>
        <GradientStop Color='#B3FFFFFF' Offset='0'/>
        <GradientStop Color='#26FFFFFF' Offset='0.3'/>
        <GradientStop Color='#0DFFFFFF' Offset='0.6'/>
        <GradientStop Color='#59FFFFFF' Offset='1'/>
      </LinearGradientBrush>
    </Border.BorderBrush>
    <Border.Background>
      <LinearGradientBrush StartPoint='0,0' EndPoint='0,1'>
        <GradientStop Color='#66303036' Offset='0'/>
        <GradientStop Color='#801C1C1E' Offset='1'/>
      </LinearGradientBrush>
    </Border.Background>
    <Grid>
      <Grid.LayoutTransform><ScaleTransform ScaleX='0.8' ScaleY='0.8'/></Grid.LayoutTransform>
      <Grid.RowDefinitions><RowDefinition Height='Auto'/><RowDefinition Height='Auto'/></Grid.RowDefinitions>
      <!-- блик стекла по верхнему краю -->
      <Border Grid.RowSpan='2' VerticalAlignment='Top' Height='90' IsHitTestVisible='False'>
        <Border.Background>
          <LinearGradientBrush StartPoint='0,0' EndPoint='0,1'>
            <GradientStop Color='#2EFFFFFF' Offset='0'/>
            <GradientStop Color='#00FFFFFF' Offset='1'/>
          </LinearGradientBrush>
        </Border.Background>
      </Border>
      <Grid Margin='18,14,14,4'>
        <Grid.ColumnDefinitions><ColumnDefinition Width='Auto'/><ColumnDefinition/><ColumnDefinition Width='Auto'/></Grid.ColumnDefinitions>
        <Path Width='15' Height='20' Stretch='Uniform' Fill='#30D158' VerticalAlignment='Center'
              Data='M12,0 C5.37,0 0,5.37 0,12 C0,21 12,32 12,32 C12,32 24,21 24,12 C24,5.37 18.63,0 12,0 Z M12,16.5 C9.51,16.5 7.5,14.49 7.5,12 C7.5,9.51 9.51,7.5 12,7.5 C14.49,7.5 16.5,9.51 16.5,12 C16.5,14.49 14.49,16.5 12,16.5 Z'/>
        <StackPanel Grid.Column='1' Margin='10,0,6,0' VerticalAlignment='Center'>
          <TextBlock x:Name='CityText' FontSize='17' FontWeight='SemiBold' Foreground='#F5F5F7' TextTrimming='CharacterEllipsis'/>
          <TextBlock x:Name='RegionText' FontSize='12.5' Foreground='#AEAEB2' TextTrimming='CharacterEllipsis'/>
        </StackPanel>
        <!-- справа в шапке: закрепить, свернуть, список времён; ⋯ появляется, когда список открыт -->
        <StackPanel Grid.Column='2' Orientation='Horizontal' VerticalAlignment='Center'>
          <Border x:Name='PinBtn' Width='32' Height='32' Background='Transparent' Cursor='Hand' Margin='0,0,4,0'>
            <Border x:Name='PinCircle' Width='30' Height='30' CornerRadius='15' BorderBrush='{StaticResource CapsuleRim}' BorderThickness='1'>
              <Path x:Name='PinIcon' Width='12' Height='15' Stretch='Uniform' StrokeThickness='1.5' StrokeLineJoin='Round'
                    StrokeStartLineCap='Round' StrokeEndLineCap='Round' HorizontalAlignment='Center' VerticalAlignment='Center'
                    Data='M4,1 L10,1 M5,1 L5,6 L2,9 L12,9 L9,6 L9,1 M7,9 L7,14' RenderTransformOrigin='0.5,0.5'>
                <Path.RenderTransform><RotateTransform x:Name='PinRotate' Angle='0'/></Path.RenderTransform>
              </Path>
            </Border>
          </Border>
          <Border x:Name='MinimizeBtn' Width='32' Height='32' Background='Transparent' Cursor='Hand' Margin='0,0,4,0'>
            <Border x:Name='MinimizeCircle' Width='30' Height='30' CornerRadius='15' BorderBrush='{StaticResource CapsuleRim}' BorderThickness='1'>
              <Path Data='M0,0 L10,0' Stroke='#D1D1D6' StrokeThickness='1.8' StrokeStartLineCap='Round' StrokeEndLineCap='Round'
                    HorizontalAlignment='Center' VerticalAlignment='Center' Margin='0,2,0,0'>
                <Path.RenderTransform><TranslateTransform x:Name='MinimizeShift' Y='0'/></Path.RenderTransform>
              </Path>
            </Border>
          </Border>
          <Border x:Name='MenuBtn' Width='32' Height='32' CornerRadius='16' Cursor='Hand' Margin='0'
                  BorderBrush='{StaticResource CapsuleRim}' BorderThickness='1'>
            <StackPanel Orientation='Horizontal' HorizontalAlignment='Center' VerticalAlignment='Center'>
              <Ellipse Width='4' Height='4' Fill='#D1D1D6' Margin='1.5,0'/>
              <Ellipse Width='4' Height='4' Fill='#D1D1D6' Margin='1.5,0'/>
              <Ellipse Width='4' Height='4' Fill='#D1D1D6' Margin='1.5,0'/>
            </StackPanel>
          </Border>
        </StackPanel>
      </Grid>
      <Grid Grid.Row='1' Margin='16,4,16,16'>
        <Grid.ColumnDefinitions><ColumnDefinition Width='292'/><ColumnDefinition Width='Auto'/></Grid.ColumnDefinitions>
        <StackPanel>
          <Grid Width='272' Height='228' HorizontalAlignment='Center'>
            <Path x:Name='ArcBg' Stroke='#2EFFFFFF' StrokeThickness='12' StrokeStartLineCap='Round' StrokeEndLineCap='Round'/>
            <Path x:Name='ArcFg' Stroke='#30D158' StrokeThickness='12' StrokeStartLineCap='Round' StrokeEndLineCap='Round'>
              <Path.Effect><DropShadowEffect x:Name='ArcGlow' Color='#30D158' BlurRadius='16' ShadowDepth='0' Opacity='0.75'/></Path.Effect>
            </Path>
            <StackPanel VerticalAlignment='Center' Margin='34,0,34,6'>
              <Viewbox StretchDirection='DownOnly' Height='40' MaxWidth='196'>
                <TextBlock x:Name='CurrentName' FontSize='28' FontWeight='SemiBold'/>
              </Viewbox>
              <TextBlock x:Name='LeftLabel' FontSize='13' Foreground='#AEAEB2' TextAlignment='Center' TextWrapping='Wrap' Margin='0,6,0,0' MaxWidth='186'/>
              <Viewbox StretchDirection='DownOnly' MaxWidth='196' Height='30'>
                <TextBlock x:Name='LeftValue' FontSize='22' FontWeight='SemiBold' Foreground='#F5F5F7'/>
              </Viewbox>
              <Viewbox StretchDirection='DownOnly' MaxWidth='186' Height='20' Margin='0,3,0,0'>
                <TextBlock x:Name='NextText' FontSize='14' Foreground='#AEAEB2'/>
              </Viewbox>
            </StackPanel>
          </Grid>
          <TextBlock x:Name='PercentText' HorizontalAlignment='Center' FontSize='13' FontWeight='SemiBold' Margin='0,-18,0,0'/>
          <TextBlock x:Name='WeekdayText' HorizontalAlignment='Center' FontSize='14' FontWeight='SemiBold' Foreground='#D1D1D6' Margin='0,8,0,0'/>
          <TextBlock x:Name='ClockText' HorizontalAlignment='Center' FontSize='46' FontWeight='Light' Foreground='#F5F5F7' Margin='0,-4,0,0'/>
          <Border x:Name='JamaatPill' HorizontalAlignment='Center' CornerRadius='12' Background='#380A84FF' Padding='12,3,14,4' Margin='0,4,0,0'
                  BorderBrush='{StaticResource CapsuleRim}' BorderThickness='1'
                  Visibility='Collapsed'>
            <TextBlock x:Name='JamaatText' FontSize='12.5' FontWeight='SemiBold' Foreground='#64B5FF'/>
          </Border>
          <Grid Margin='4,12,4,0'>
            <Grid.ColumnDefinitions><ColumnDefinition Width='Auto'/><ColumnDefinition/></Grid.ColumnDefinitions>
            <Grid.RowDefinitions><RowDefinition Height='Auto'/><RowDefinition Height='Auto'/><RowDefinition Height='Auto'/><RowDefinition Height='Auto'/></Grid.RowDefinitions>
            <Border Grid.ColumnSpan='2' Height='1' Background='#26FFFFFF'/>
            <TextBlock x:Name='MiladiLabel' Grid.Row='1' FontSize='12.5' Foreground='#AEAEB2' VerticalAlignment='Center' Margin='0,8,0,8'/>
            <TextBlock x:Name='MiladiText' Grid.Row='1' Grid.Column='1' FontSize='13.5' FontWeight='SemiBold' Foreground='#F5F5F7'
                       HorizontalAlignment='Right' VerticalAlignment='Center' Margin='10,8,0,8' TextTrimming='CharacterEllipsis'/>
            <Border Grid.Row='2' Grid.ColumnSpan='2' Height='1' Background='#26FFFFFF'/>
            <TextBlock x:Name='HijriLabel' Grid.Row='3' FontSize='12.5' Foreground='#AEAEB2' VerticalAlignment='Center' Margin='0,8,0,0'/>
            <TextBlock x:Name='HijriText' Grid.Row='3' Grid.Column='1' FontSize='13.5' FontWeight='SemiBold' Foreground='#30D158'
                       HorizontalAlignment='Right' VerticalAlignment='Center' Margin='10,8,0,0' TextTrimming='CharacterEllipsis'/>
          </Grid>
          <TextBlock x:Name='StatusText' Foreground='#FF6961' FontSize='12' TextWrapping='Wrap' Margin='0,6,0,0' Visibility='Collapsed'/>
        </StackPanel>
        <StackPanel x:Name='TimesPanel' Grid.Column='1' MinWidth='250' Margin='16,0,2,0'>
          <Grid Margin='12,4,12,8'>
            <TextBlock x:Name='TimesTitle' FontSize='13' FontWeight='SemiBold' Foreground='#AEAEB2'/>
            <TextBlock x:Name='JamaatHeader' FontSize='12.5' FontWeight='SemiBold' Foreground='#64B5FF' HorizontalAlignment='Right' Margin='16,0,0,0'
                       Cursor='Hand' VerticalAlignment='Bottom'/>
          </Grid>
          <StackPanel x:Name='Rows'/>
        </StackPanel>
      </Grid>
    </Grid>
  </Border>
</Window>";

        TextBlock Tb(string name) { return (TextBlock)window.FindName(name); }
        System.Windows.Shapes.Path Arc(string name) { return (System.Windows.Shapes.Path)window.FindName(name); }
        StackPanel RowsPanel { get { return (StackPanel)window.FindName("Rows"); } }

        public void Init(bool showWindow)
        {
            LoadSettings();
            MigrateAutostart();

            window = (Window)XamlReader.Parse(MainXaml);
            window.Topmost = settings.Topmost;
            Glass.Apply(window, (Border)window.FindName("Surface"), B("#F2262628"), settings.Transparency < 3);
            ApplyTint((Border)window.FindName("Surface"));
            PlaceWindow();
            Arc("ArcBg").Data = ArcGeometry(135, 270);

            var menuBtn = (Border)window.FindName("MenuBtn");
            menuBtn.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                mainMenu.PlacementTarget = menuBtn;
                mainMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                mainMenu.IsOpen = true;
            };
            HoverFeedback(menuBtn, "#1AFFFFFF", "#33FFFFFF", "#4DFFFFFF");
            var pinBtn = (Border)window.FindName("PinBtn");
            HoverFeedback(pinBtn, "#1AFFFFFF", "#33FFFFFF", "#4DFFFFFF", (Border)window.FindName("PinCircle"));
            pinBtn.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                settings.Topmost = !settings.Topmost;
                window.Topmost = settings.Topmost;
                if (card != null) card.Topmost = settings.Topmost;
                SaveSettings();
                UpdatePinButton();
            };
            UpdatePinButton();
            var minimizeBtn = (Border)window.FindName("MinimizeBtn");
            HoverFeedback(minimizeBtn, "#1AFFFFFF", "#33FFFFFF", "#4DFFFFFF", (Border)window.FindName("MinimizeCircle"));
            // чёрточка при наведении чуть опускается, при нажатии «уезжает» вниз — и виджет прячется
            var minimizeShift = (TranslateTransform)window.FindName("MinimizeShift");
            minimizeBtn.MouseEnter += (s, e) => Animate(minimizeShift, TranslateTransform.YProperty, 1.5, 160);
            minimizeBtn.MouseLeave += (s, e) => Animate(minimizeShift, TranslateTransform.YProperty, 0, 140);
            minimizeBtn.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                Animate(minimizeShift, TranslateTransform.YProperty, 5, 120, () => { HideToTray(); minimizeShift.BeginAnimation(TranslateTransform.YProperty, null); });
            };
            var jamaatHeader = Tb("JamaatHeader");
            jamaatHeader.MouseLeftButtonDown += (s, e) => { e.Handled = true; ShowJamaatDialog(); };
            jamaatHeader.MouseEnter += (s, e) => jamaatHeader.TextDecorations = TextDecorations.Underline;
            jamaatHeader.MouseLeave += (s, e) => jamaatHeader.TextDecorations = null;
            window.MouseLeftButtonDown += (s, e) =>
            {
                if (!FreePosition) return;   // в углу виджет стоит на месте
                double before = window.Left;
                try { window.DragMove(); } catch (InvalidOperationException) { }
                if (window.Left != before) expandShift = 0;
                settings.Left = Math.Round(window.Left + expandShift);
                settings.Top = Math.Round(window.Top);
                SaveSettings();
            };

            tray = new WinForms.NotifyIcon();
            tray.Icon = CrescentIcon();
            tray.ContextMenuStrip = new WinForms.ContextMenuStrip();
            tray.MouseClick += (s, e) => { if (e.Button == WinForms.MouseButtons.Left) ShowWidget(); };

            BuildMenus();
            UpdateTexts();
            tray.Visible = true;

            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (s, e) => Tick();

            BuildRows();
            StartFetch();
            UpdateView();
            // закреплённый в углу виджет растёт влево при раскрытии списка и остаётся в углу при смене экрана
            window.SizeChanged += (s, e) => DockToCorner();
            SystemEvents.DisplaySettingsChanged += (s, e) => dispatcher.BeginInvoke(new Action(DockToCorner));
            InitCard();
            UpdateView();
            if (showWindow && !settings.CityChosen && !TestMode)
            {
                // первый запуск: выбрать страну и город — его расписание скачается на год и будет работать без интернета
                var ask = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                ask.Tick += (s, e) =>
                {
                    ask.Stop();
                    settings.CityChosen = true;   // спрашиваем один раз; сменить город можно в меню
                    SaveSettings();
                    ShowCityDialog(true);
                };
                ask.Start();
            }
            if (showWindow)
            {
                timer.Start();
                window.Show();
                DockToCorner();
                window.ContentRendered += (s, e) => dispatcher.BeginInvoke(new Action(ShowCompact), DispatcherPriority.ApplicationIdle);
            }
        }

        void Tick()
        {
            try
            {
                // Расписание: 1 января в 00:00 начинается новый год — скачиваем его; без сети повторяем каждую минуту,
                // если сайт ещё отдаёт прошлый год — раз в час. Дата хиджры сверяется с сайтом раз в день.
                double sinceAttempt = (DateTime.Now - lastAttempt).TotalSeconds;
                bool needYear = !HasToday || YearStale;
                bool needFetch = (needYear && sinceAttempt >= (serverOldYear && HasToday ? 3600 : 60)) ||
                                 (hijriFor != TodayKey && sinceAttempt >= 600);
                if (needFetch && !fetching) StartFetch();
                UpdateView();
            }
            catch (Exception ex) { LogError(ex); }
        }

        void PlaceWindow()
        {
            double left = SystemParameters.VirtualScreenLeft, top = SystemParameters.VirtualScreenTop;
            if (settings.Left.HasValue && settings.Top.HasValue &&
                settings.Left.Value >= left && settings.Left.Value < left + SystemParameters.VirtualScreenWidth - 60 &&
                settings.Top.Value >= top && settings.Top.Value < top + SystemParameters.VirtualScreenHeight - 60)
            {
                window.Left = settings.Left.Value;
                window.Top = settings.Top.Value;
            }
            else
            {
                window.Left = SystemParameters.WorkArea.Right - 380;
                window.Top = SystemParameters.WorkArea.Top + 10;
            }
        }

        // тонировка стекла сверху/снизу для уровней прозрачности (альфа-канал): чем выше уровень, тем лучше виден фон;
        // «Максимальная» — без размытия: сквозь стекло видны окна, поэтому тонировка как у «Высокой»
        static readonly byte[][] TintLevels = { new byte[] { 0x99, 0xB3 }, new byte[] { 0x66, 0x80 }, new byte[] { 0x40, 0x59 }, new byte[] { 0x40, 0x59 } };

        // Одинаково для активного и неактивного окна: размытие Windows не зависит от фокуса, а тонировку задаём сами
        void ApplyTint(Border surface)
        {
            if (surface == null || "opaque".Equals(surface.Tag)) return;
            var level = TintLevels[Math.Max(0, Math.Min(3, settings.Transparency))];
            surface.Background = new LinearGradientBrush(Color.FromArgb(level[0], 0x30, 0x30, 0x36), Color.FromArgb(level[1], 0x1C, 0x1C, 0x1E), 90);
        }

        void SetTransparency(int level)
        {
            settings.Transparency = level;
            SaveSettings();
            ApplyTint((Border)window.FindName("Surface"));
            Glass.SetBlur(window, level < 3);
            if (popup != null) { ApplyTint((Border)popup.FindName("Surface")); Glass.SetBlur(popup, level < 3); }
            if (card != null) { ApplyTint((Border)card.FindName("Surface")); Glass.SetBlur(card, level < 3); }
            BuildMenus();
        }

        const double DockMargin = 12;

        // Виджет всегда в правом нижнем углу над панелью задач: и полный вид, и карточка.
        // Полный вид при раскрытии списка растёт от угла влево и вверх.
        static readonly string[] Positions = { "BR", "TR", "BL", "TL", "Free" };

        bool FreePosition { get { return settings.Position == "Free"; } }

        // Угол: окно прижато к выбранному углу и растёт от него. Свободное положение: окно там, куда его поставили,
        // но целиком на экране.
        void DockToCorner()
        {
            if (window == null) return;
            if (FreePosition) KeepInsideScreen(window);
            else AnchorCorner(window);
            expandShift = 0;
        }

        void AnchorCorner(Window w)
        {
            var work = SystemParameters.WorkArea;
            bool left = settings.Position == "BL" || settings.Position == "TL";
            bool top = settings.Position == "TR" || settings.Position == "TL";
            w.Left = left ? work.Left + DockMargin : work.Right - w.ActualWidth - DockMargin;
            w.Top = top ? work.Top + DockMargin : work.Bottom - w.ActualHeight - DockMargin;
        }

        // окно на экране, где сейчас стоит (с учётом нескольких мониторов)
        static void KeepInsideScreen(Window w)
        {
            var screen = WinForms.Screen.FromPoint(new Gdi.Point((int)w.Left, (int)w.Top));
            var source = PresentationSource.FromVisual(w);
            Rect work = SystemParameters.WorkArea;
            if (source != null)
            {
                var m = source.CompositionTarget.TransformFromDevice;
                var a = screen.WorkingArea;
                work = new Rect(m.Transform(new Point(a.Left, a.Top)), m.Transform(new Point(a.Right, a.Bottom)));
            }
            w.Left = Math.Max(work.Left, Math.Min(w.Left, work.Right - w.ActualWidth));
            w.Top = Math.Max(work.Top, Math.Min(w.Top, work.Bottom - w.ActualHeight));
        }

        // выбрали положение в меню — переставить то, что сейчас на экране
        void SetPosition(string position)
        {
            settings.Position = position;
            // переход на «свободное» — виджет остаётся там, где стоит сейчас
            Window shown = card != null && card.IsVisible ? card : window;
            if (position == "Free") { settings.Left = Math.Round(shown.Left); settings.Top = Math.Round(shown.Top); }
            if (window.IsVisible) DockToCorner();
            if (card != null && card.IsVisible) PlaceCard();
            SaveSettings();
            BuildMenus();
        }

        // анимации отключаются, если в Windows выключены эффекты анимации
        static Duration Motion(int ms) { return new Duration(TimeSpan.FromMilliseconds(SystemParameters.ClientAreaAnimation ? ms : 0)); }

        // плавная подсветка фона при наведении и нажатии (без изменения размеров)
        // visual — что подсвечивать, если зона нажатия больше самой кнопки
        // плавная анимация свойства (с «пружинкой», если spring); done — по окончании
        static void Animate(System.Windows.Media.Animation.Animatable target, DependencyProperty property, double to, int ms, Action done = null, bool spring = false)
        {
            var anim = new System.Windows.Media.Animation.DoubleAnimation(to, Motion(ms)) {
                EasingFunction = spring
                    ? (System.Windows.Media.Animation.IEasingFunction)new System.Windows.Media.Animation.BackEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut, Amplitude = 0.6 }
                    : new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut } };
            if (done != null) anim.Completed += (s, e) => done();
            target.BeginAnimation(property, anim);
        }

        // Кнопка «как на iPhone»: при наведении светлеет и чуть увеличивается (с пружинкой),
        // при нажатии — сжимается. Размеры в разметке не меняются — соседние элементы не сдвигаются.
        // visual — что подсвечивать, если зона нажатия больше самой кнопки
        static void HoverFeedback(Border target, string normal, string hover, string pressed, Border visual = null)
        {
            Func<string, Color> c = hex => (Color)ColorConverter.ConvertFromString(hex);
            var brush = new SolidColorBrush(c(normal));
            var element = visual ?? target;
            element.Background = brush;
            var scale = new ScaleTransform(1, 1);
            element.RenderTransformOrigin = new Point(0.5, 0.5);
            element.RenderTransform = scale;
            Action<string, int> to = (hex, ms) =>
                brush.BeginAnimation(SolidColorBrush.ColorProperty, new System.Windows.Media.Animation.ColorAnimation(c(hex), Motion(ms)));
            Action<double, int, bool> size = (k, ms, spring) =>
            {
                Animate(scale, ScaleTransform.ScaleXProperty, k, ms, null, spring);
                Animate(scale, ScaleTransform.ScaleYProperty, k, ms, null, spring);
            };
            target.MouseEnter += (s, e) => { to(hover, 150); size(1.08, 260, true); };
            target.MouseLeave += (s, e) => { to(normal, 200); size(1, 180, false); };
            target.PreviewMouseLeftButtonDown += (s, e) => { to(pressed, 80); size(0.9, 90, false); };
            target.PreviewMouseLeftButtonUp += (s, e) => { to(hover, 150); size(1.08, 260, true); };
        }

        double expandShift;

        // город, регион и страна на языке виджета (на сайте — латиница)
        string CityTitle { get { return Names.City(settings.CityName, Names.CountryCode(settings.CountryName), L.Code); } }

        string RegionTitle
        {
            get
            {
                string code = Names.CountryCode(settings.CountryName);
                var parts = new[] { Names.Region(settings.CityRegion, code, L.Code),
                                    string.IsNullOrEmpty(settings.CountryName) ? "" : Names.Country(settings.CountryName, L.Code) };
                return string.Join(", ", parts.Where(x => !string.IsNullOrEmpty(x)));
            }
        }

        // булавка: закреплён поверх всех окон — синяя и залитая, иначе — контур
        void UpdatePinButton()
        {
            var icon = (System.Windows.Shapes.Path)window.FindName("PinIcon");
            var pinBtn = (Border)window.FindName("PinBtn");
            icon.Stroke = settings.Topmost ? Brushes["Blue"] : B("#D1D1D6");
            Animate((RotateTransform)window.FindName("PinRotate"), RotateTransform.AngleProperty, settings.Topmost ? 0 : 40, 360, null, true);
            icon.Fill = settings.Topmost ? Brushes["Blue"] : System.Windows.Media.Brushes.Transparent;
            ((Border)window.FindName("PinCircle")).BorderBrush = settings.Topmost ? B("#B30A84FF") : (Brush)window.FindResource("CapsuleRim");
            pinBtn.ToolTip = T("Topmost") + (settings.Topmost ? " ✓" : "");
            System.Windows.Automation.AutomationProperties.SetName(pinBtn, pinBtn.ToolTip.ToString());
        }

        void UpdateTexts()
        {
            ((Border)window.FindName("MenuBtn")).ToolTip = T("Menu");
            ((Border)window.FindName("MinimizeBtn")).ToolTip = T("Minimize");
            UpdatePinButton();
            System.Windows.Automation.AutomationProperties.SetName((Border)window.FindName("MinimizeBtn"), T("Minimize"));
            Tb("MiladiLabel").Text = T("MiladiLabel");
            Tb("HijriLabel").Text = T("HijriLabel");
            Tb("TimesTitle").Text = T("TimesTitle");
            Tb("JamaatHeader").ToolTip = T("JamaatMenu");
            System.Windows.Automation.AutomationProperties.SetName((Border)window.FindName("MenuBtn"), T("Menu"));
            System.Windows.Automation.AutomationProperties.SetName(Tb("JamaatHeader"), T("JamaatMenu"));
        }

        void BuildRows()
        {
            RowsPanel.Children.Clear();
            rows.Clear();
            bool anyJamaat = settings.Jamaat.Count > 0 || settings.JamaatAzan.Count > 0;
            Tb("JamaatHeader").Text = anyJamaat ? T("Jamaat") : "+ " + T("Jamaat");
            foreach (var ev in GetEvents())
            {
                bool prayer = ev.Type == "Green";
                double size = prayer ? 15 : 13.5;
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var name = new TextBlock { Text = ev.Name, FontSize = size, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
                var time = new TextBlock { Text = ev.Time.ToString("HH:mm", Inv), FontSize = size, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(time, 1);
                grid.Children.Add(name);
                grid.Children.Add(time);
                TextBlock jamaat = null;
                if (anyJamaat)
                {
                    var jt = JamaatTime(ev);
                    jamaat = new TextBlock {
                        Text = jt.HasValue ? jt.Value.ToString("HH:mm", Inv) : "", FontSize = 13.5, Width = 46,
                        TextAlignment = TextAlignment.Right, Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(jamaat, 2);
                    grid.Children.Add(jamaat);
                }
                var border = new Border {
                    CornerRadius = new CornerRadius(12), Margin = new Thickness(0, 1, 0, 1), Child = grid, BorderThickness = new Thickness(1),
                    Padding = new Thickness(10, prayer ? 5 : 3, 10, prayer ? 5 : 3)
                };
                RowsPanel.Children.Add(border);
                rows[ev.Key] = new RowUi { Border = border, Name = name, Time = time, Jamaat = jamaat };
            }
        }

        static Geometry ArcGeometry(double startDeg, double sweepDeg) { return ArcGeometry(startDeg, sweepDeg, 136, 124, 116); }

        // как на сайте: зелёный период после 50% синий; в конце времени намаза — красный
        static string BarColor(Period period, DateTime now, out int percent)
        {
            double total = (period.To - period.From).TotalSeconds;
            percent = (int)Math.Max(0, Math.Min(100, Math.Floor((now - period.From).TotalSeconds * 100 / total)));
            if (period.Color != "Green") return period.Color;
            if ((period.To - now).TotalMinutes <= EndWarningMinutes) return "Red";
            return percent > 50 ? "Blue" : "Green";
        }

        static Geometry ArcGeometry(double startDeg, double sweepDeg, double cx, double cy, double r)
        {
            double a1 = startDeg * Math.PI / 180, a2 = (startDeg + sweepDeg) * Math.PI / 180;
            string path = string.Format(Inv, "M {0:0.##},{1:0.##} A {2},{2} 0 {3} 1 {4:0.##},{5:0.##}",
                cx + r * Math.Cos(a1), cy + r * Math.Sin(a1), r, sweepDeg > 180 ? 1 : 0, cx + r * Math.Cos(a2), cy + r * Math.Sin(a2));
            return Geometry.Parse(path);
        }

        string FormatLeft(TimeSpan span)
        {
            int mins = Math.Max(1, (int)Math.Ceiling(span.TotalMinutes));
            return mins >= 60 ? string.Format(T("HM"), mins / 60, mins % 60) : string.Format(T("M"), mins);
        }

        public void UpdateView()
        {
            DateTime now = Now;
            Tb("CityText").Text = CityTitle;
            Tb("RegionText").Text = RegionTitle;
            Tb("RegionText").Visibility = RegionTitle.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
            Tb("ClockText").Text = now.ToString("HH:mm:ss", Inv);

            var dates = DateLines();
            Tb("WeekdayText").Text = dates[0];
            Tb("MiladiText").Text = dates[1];
            Tb("MiladiText").ToolTip = dates[1];
            Tb("HijriText").Text = dates[2];
            Tb("HijriText").ToolTip = dates[2];

            var period = GetPeriod(now);
            string newTrayText;
            if (period != null)
            {
                int percent;
                string barColor = BarColor(period, now, out percent);
                SolidColorBrush bar = Brushes[barColor];
                Tb("CurrentName").Text = period.Name;
                Tb("CurrentName").Foreground = Brushes["Ink" + period.Color];
                Tb("LeftLabel").Text = string.Format(T("Until"), period.NextName);
                Tb("LeftValue").Text = FormatLeft(period.To - now);
                Tb("NextText").Text = period.NextName + " " + period.To.ToString("HH:mm", Inv);
                Arc("ArcFg").Stroke = bar;
                ((System.Windows.Media.Effects.DropShadowEffect)window.FindName("ArcGlow")).Color = bar.Color;
                Arc("ArcFg").Data = ArcGeometry(135, 270 * Math.Max(0.2, percent) / 100.0);
                Tb("PercentText").Text = percent + "%";
                Tb("PercentText").Foreground = Brushes["Ink" + barColor];
                newTrayText = string.Format(T("TrayTip"), period.NextName, period.To.ToString("HH:mm", Inv));
            }
            else
            {
                Tb("CurrentName").Text = "—";
                Tb("CurrentName").Foreground = Brushes["TextGreen"];
                Tb("LeftLabel").Text = T("Loading");
                Tb("LeftValue").Text = "";
                Tb("NextText").Text = "";
                Tb("PercentText").Text = "";
                Arc("ArcFg").Data = null;
                newTrayText = T("TrayDefault");
            }
            if (newTrayText.Length > 63) newTrayText = newTrayText.Substring(0, 63);
            if (newTrayText != trayText) { tray.Text = newTrayText; trayText = newTrayText; }

            // активная строка — цвет периода, остальные серые
            var events = GetEvents();
            foreach (var ev in events)
            {
                RowUi row;
                if (!rows.TryGetValue(ev.Key, out row)) continue;
                bool active = period != null && period.Key == ev.Key;
                row.Border.Background = active ? Brushes["Row" + ev.Type] : System.Windows.Media.Brushes.Transparent;
                row.Border.BorderBrush = active ? (Brush)window.FindResource("CapsuleRim") : System.Windows.Media.Brushes.Transparent;
                foreach (var tb in new[] { row.Name, row.Time })
                {
                    tb.Foreground = active ? Brushes["Text" + ev.Type] : Brushes["Inactive"];
                    tb.FontWeight = active ? FontWeights.Bold : FontWeights.Normal;
                }
                if (row.Jamaat != null)
                {
                    row.Jamaat.Foreground = JamaatInk;
                    row.Jamaat.FontWeight = active ? FontWeights.Bold : FontWeights.SemiBold;
                }
            }

            // ближайший жамагат сегодня — под часами (виден и при скрытом списке)
            var nextJamaat = events.Select(e => new { Ev = e, At = JamaatTime(e) })
                                   .Where(x => x.At.HasValue && x.At.Value > now).OrderBy(x => x.At.Value).FirstOrDefault();
            var jamaatText = Tb("JamaatText");
            if (nextJamaat != null && HasToday)
            {
                jamaatText.Text = string.Format(T("NextJamaat"), JamaatName(nextJamaat.Ev), nextJamaat.At.Value.ToString("HH:mm", Inv));
                ((FrameworkElement)window.FindName("JamaatPill")).Visibility = Visibility.Visible;
            }
            else ((FrameworkElement)window.FindName("JamaatPill")).Visibility = Visibility.Collapsed;
            UpdateCard(period, now, events);

            bool stale = !HasToday;
            var status = Tb("StatusText");
            if (fetchError && stale)
            {
                status.Text = T("OfflineNoData");
                status.Visibility = Visibility.Visible;
            }
            else if (TodayApproximate)
            {
                status.Text = T("OldYear");
                status.Visibility = Visibility.Visible;
            }
            else status.Visibility = Visibility.Collapsed;

            if (!stale) SendNotifications(events.Where(e => e.Tracked).ToList(), now);
        }

        // ---------- уведомления ----------
        void SendNotifications(List<Ev> tracked, DateTime now)
        {
            foreach (var ev in tracked)
            {
                string caption = CityTitle + " · " + ev.Time.ToString("HH:mm", Inv);
                var accent = Brushes[ev.Type];
                if (settings.NotifyBefore > 0)
                {
                    DateTime at = ev.Time.AddMinutes(-settings.NotifyBefore);
                    string id = "pre" + settings.NotifyBefore + "-" + ev.Key + "-" + ev.Time.ToString("yyyyMMdd", Inv);
                    if (now >= at && now < ev.Time && now < at.AddMinutes(2) && notified.Add(id))
                        ShowNotification(ev.Name, string.Format(T("NotifyBeforeMsg"), settings.NotifyBefore), caption, accent);
                }
                if (settings.Notify && ev.Key != "kun")
                {
                    string id = "at-" + ev.Key + "-" + ev.Time.ToString("yyyyMMdd", Inv);
                    if (now >= ev.Time && now < ev.Time.AddMinutes(2) && notified.Add(id))
                        ShowNotification(ev.Name, T("NotifyAtMsg"), caption, accent);
                }

                var jamaat = JamaatTime(ev);
                if (!jamaat.HasValue || JamaatByAzan(ev)) continue;   // по азану — хватает уведомлений о намазе
                string jt = jamaat.Value.ToString("HH:mm", Inv), day = jamaat.Value.ToString("yyyyMMdd", Inv);
                string jCaption = CityTitle + " · " + T("Jamaat") + " " + jt;
                if (settings.JamaatBefore > 0)
                {
                    DateTime at = jamaat.Value.AddMinutes(-settings.JamaatBefore);
                    string id = "jpre" + settings.JamaatBefore + "-" + ev.Key + "-" + jt + "-" + day;
                    if (now >= at && now < jamaat.Value && now < at.AddMinutes(2) && notified.Add(id))
                        ShowNotification(JamaatName(ev), string.Format(T("JamaatBeforeMsg"), settings.JamaatBefore), jCaption, Brushes["Blue"]);
                }
                string jid = "jat-" + ev.Key + "-" + jt + "-" + day;
                if (now >= jamaat.Value && now < jamaat.Value.AddMinutes(2) && notified.Add(jid))
                    ShowNotification(JamaatName(ev), T("JamaatMsg"), jCaption, Brushes["Blue"]);
            }
        }

        const string PopupXaml = @"
<Window xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        SizeToContent='WidthAndHeight' WindowStyle='SingleBorderWindow' AllowsTransparency='False' Background='Transparent'
        Topmost='True' ShowInTaskbar='False' ShowActivated='False' ResizeMode='NoResize'
        WindowStartupLocation='CenterScreen' FontFamily='Segoe UI Variable Display, Segoe UI' UseLayoutRounding='True'
        TextOptions.TextFormattingMode='Ideal'>
  <WindowChrome.WindowChrome>
    <WindowChrome CaptionHeight='0' GlassFrameThickness='0' ResizeBorderThickness='0' CornerRadius='0' UseAeroCaptionButtons='False'/>
  </WindowChrome.WindowChrome>
  <Window.Resources>
    <LinearGradientBrush x:Key='CapsuleRim' StartPoint='0,0' EndPoint='0,1'>
      <GradientStop Color='#8CFFFFFF' Offset='0'/>
      <GradientStop Color='#14FFFFFF' Offset='0.5'/>
      <GradientStop Color='#40FFFFFF' Offset='1'/>
    </LinearGradientBrush>
  </Window.Resources>
  <!-- тот же Liquid Glass, что у виджета: прозрачное стекло, кромка-«линза», блик сверху, тень под текстом -->
  <Border x:Name='Surface' CornerRadius='8' BorderThickness='1.5' Width='400'>
    <Border.Resources>
      <Style TargetType='TextBlock'>
        <Setter Property='Effect'>
          <Setter.Value><DropShadowEffect Color='Black' BlurRadius='6' ShadowDepth='0' Opacity='0.6'/></Setter.Value>
        </Setter>
      </Style>
    </Border.Resources>
    <Border.BorderBrush>
      <LinearGradientBrush StartPoint='0,0' EndPoint='1,1'>
        <GradientStop Color='#B3FFFFFF' Offset='0'/>
        <GradientStop Color='#26FFFFFF' Offset='0.3'/>
        <GradientStop Color='#0DFFFFFF' Offset='0.6'/>
        <GradientStop Color='#59FFFFFF' Offset='1'/>
      </LinearGradientBrush>
    </Border.BorderBrush>
    <Border.Background>
      <LinearGradientBrush StartPoint='0,0' EndPoint='0,1'>
        <GradientStop Color='#66303036' Offset='0'/>
        <GradientStop Color='#801C1C1E' Offset='1'/>
      </LinearGradientBrush>
    </Border.Background>
    <Grid>
      <Border VerticalAlignment='Top' Height='90' IsHitTestVisible='False'>
        <Border.Background>
          <LinearGradientBrush StartPoint='0,0' EndPoint='0,1'>
            <GradientStop Color='#2EFFFFFF' Offset='0'/>
            <GradientStop Color='#00FFFFFF' Offset='1'/>
          </LinearGradientBrush>
        </Border.Background>
      </Border>
      <StackPanel Margin='26,20,26,22'>
        <StackPanel Orientation='Horizontal' HorizontalAlignment='Center'>
          <Path x:Name='Pin' Width='12' Height='16' Stretch='Uniform' VerticalAlignment='Center' Margin='0,0,7,0'
                Data='M12,0 C5.37,0 0,5.37 0,12 C0,21 12,32 12,32 C12,32 24,21 24,12 C24,5.37 18.63,0 12,0 Z M12,16.5 C9.51,16.5 7.5,14.49 7.5,12 C7.5,9.51 9.51,7.5 12,7.5 C14.49,7.5 16.5,9.51 16.5,12 C16.5,14.49 14.49,16.5 12,16.5 Z'/>
          <TextBlock x:Name='Caption' FontSize='14' FontWeight='SemiBold' Foreground='#AEAEB2'/>
        </StackPanel>
        <!-- название намаза светится своим цветом, как кольцо прогресса в виджете -->
        <TextBlock x:Name='TitleText' FontSize='36' FontWeight='SemiBold' HorizontalAlignment='Center' Margin='0,8,0,0' TextWrapping='Wrap' TextAlignment='Center'>
          <TextBlock.Effect><DropShadowEffect x:Name='TitleGlow' BlurRadius='18' ShadowDepth='0' Opacity='0.8'/></TextBlock.Effect>
        </TextBlock>
        <TextBlock x:Name='MessageText' FontSize='17' Foreground='#F5F5F7' HorizontalAlignment='Center' Margin='0,4,0,18' TextWrapping='Wrap' TextAlignment='Center'/>
        <!-- кнопка — стеклянная капсула с оттенком цвета намаза -->
        <Border x:Name='CloseBtn' CornerRadius='20' Padding='0,10,0,11' Cursor='Hand' BorderBrush='{StaticResource CapsuleRim}' BorderThickness='1'>
          <TextBlock x:Name='CloseText' FontSize='15' FontWeight='SemiBold' Foreground='#F5F5F7' HorizontalAlignment='Center'/>
        </Border>
      </StackPanel>
    </Grid>
  </Border>
</Window>";

        public void ClosePopup()
        {
            if (popupTimer != null) { popupTimer.Stop(); popupTimer = null; }
            if (popup != null)
            {
                var w = popup;
                popup = null;
                w.Close();
            }
        }

        public void ShowNotification(string title, string message, string caption, SolidColorBrush accent)
        {
            ClosePopup();
            var w = (Window)XamlReader.Parse(PopupXaml);
            // заголовок — светлый вариант цвета для тёмного стекла
            string accentKey = Brushes.Where(p => p.Value == accent).Select(p => p.Key).FirstOrDefault();
            SolidColorBrush ink = accentKey != null && Brushes.ContainsKey("Ink" + accentKey) ? Brushes["Ink" + accentKey] : accent;
            Glass.Apply(w, (Border)w.FindName("Surface"), B("#F2262628"), settings.Transparency < 3);
            ApplyTint((Border)w.FindName("Surface"));
            ((System.Windows.Media.Effects.DropShadowEffect)w.FindName("TitleGlow")).Color = accent.Color;
            ((System.Windows.Shapes.Path)w.FindName("Pin")).Fill = accent;
            ((TextBlock)w.FindName("Caption")).Text = caption;
            ((TextBlock)w.FindName("TitleText")).Text = title;
            ((TextBlock)w.FindName("TitleText")).Foreground = ink;
            ((TextBlock)w.FindName("MessageText")).Text = message;
            ((TextBlock)w.FindName("CloseText")).Text = T("Close");
            var closeBtn = (Border)w.FindName("CloseBtn");
            // оттенок цвета намаза на стекле; при наведении — ярче и с «пружинкой», как кнопки виджета
            Func<byte, string> tint = a => string.Format(Inv, "#{0:X2}{1:X2}{2:X2}{3:X2}", a, accent.Color.R, accent.Color.G, accent.Color.B);
            HoverFeedback(closeBtn, tint(0x59), tint(0x80), tint(0xA6));
            // кнопка закрывает окно сразу по нажатию; перетаскивание окна — только вне кнопки
            closeBtn.PreviewMouseLeftButtonDown += (s, e) => { e.Handled = true; ClosePopup(); };
            w.MouseLeftButtonDown += (s, e) => { try { w.DragMove(); } catch (InvalidOperationException) { } };
            w.KeyDown += (s, e) => { if (e.Key == Key.Escape || e.Key == Key.Enter) ClosePopup(); };

            popup = w;
            popupTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(2) };
            popupTimer.Tick += (s, e) => ClosePopup();
            popupTimer.Start();
            if (!TestMode) w.Show();

            PlaySound();
            if (settings.WinNotify && tray != null)
                tray.ShowBalloonTip(10000, title + " — " + message, caption, WinForms.ToolTipIcon.Info);
        }

        // ---------- звук ----------
        void PlaySound()
        {
            if (TestMode) return;
            try
            {
                if (settings.Sound == "system") { SystemSounds.Asterisk.Play(); return; }
                player = new SoundPlayer(new MemoryStream(settings.Sound == "soft" ? SoftWav() : ChimeWav()));
                player.Play();
            }
            catch (Exception)
            {
                SystemSounds.Asterisk.Play();
            }
        }

        static byte[] cachedChime, cachedSoft;

        // Колокольчики: мажорное арпеджио C6–E6–G6–C7 с мягким затуханием
        public static byte[] ChimeWav()
        {
            if (cachedChime == null)
                cachedChime = Wav(2.6, t => Bell(t, 0.00, 1046.50, 0.55) + Bell(t, 0.16, 1318.51, 0.55) +
                                             Bell(t, 0.32, 1567.98, 0.65) + 0.8 * Bell(t, 0.52, 2093.00, 0.9));
            return cachedChime;
        }

        // Мягкий звон «динь-дон»: G5 → E5
        public static byte[] SoftWav()
        {
            if (cachedSoft == null)
                cachedSoft = Wav(3.0, t => Bell(t, 0.0, 783.99, 0.9) + Bell(t, 0.55, 659.25, 1.1));
            return cachedSoft;
        }

        static double Bell(double t, double start, double freq, double decay)
        {
            double x = t - start;
            if (x < 0) return 0;
            double envelope = Math.Min(1, x / 0.004) * Math.Exp(-x / decay);
            return envelope * (Math.Sin(2 * Math.PI * freq * x)
                + 0.30 * Math.Sin(2 * Math.PI * freq * 2.0 * x) * Math.Exp(-x / (decay * 0.5))
                + 0.10 * Math.Sin(2 * Math.PI * freq * 3.01 * x) * Math.Exp(-x / (decay * 0.3)));
        }

        static byte[] Wav(double seconds, Func<double, double> wave)
        {
            const int rate = 44100;
            int n = (int)(rate * seconds);
            var samples = new double[n];
            double peak = 1e-9;
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / rate;
                samples[i] = wave(t) * Math.Min(1, (seconds - t) / 0.08);
                peak = Math.Max(peak, Math.Abs(samples[i]));
            }
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                w.Write(Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + n * 2); w.Write(Encoding.ASCII.GetBytes("WAVE"));
                w.Write(Encoding.ASCII.GetBytes("fmt ")); w.Write(16); w.Write((short)1); w.Write((short)1);
                w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
                w.Write(Encoding.ASCII.GetBytes("data")); w.Write(n * 2);
                for (int i = 0; i < n; i++) w.Write((short)(samples[i] / peak * 0.7 * short.MaxValue));
                w.Flush();
                return ms.ToArray();
            }
        }

        // ---------- выбор города ----------
        const string CityXaml = @"
<Window xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        Width='480' Height='560' FontFamily='Segoe UI' FontSize='13'
        WindowStartupLocation='CenterScreen' ResizeMode='CanResize' MinWidth='400' MinHeight='420' Topmost='True' ShowInTaskbar='True'>
  <Grid Margin='14'>
    <Grid.RowDefinitions>
      <RowDefinition Height='Auto'/><RowDefinition Height='Auto'/><RowDefinition Height='Auto'/>
      <RowDefinition Height='Auto'/><RowDefinition Height='*'/><RowDefinition Height='Auto'/>
    </Grid.RowDefinitions>
    <TextBlock x:Name='Prompt' Margin='0,0,0,6'/>
    <Grid Grid.Row='1'>
      <Grid.ColumnDefinitions><ColumnDefinition/><ColumnDefinition Width='Auto'/></Grid.ColumnDefinitions>
      <TextBox x:Name='Query' Padding='4,3'/>
      <Button x:Name='SearchBtn' Grid.Column='1' Padding='14,3' Margin='6,0,0,0'/>
    </Grid>
    <TextBlock x:Name='BrowseText' Grid.Row='2' Margin='0,14,0,6'/>
    <Grid Grid.Row='3'>
      <Grid.ColumnDefinitions><ColumnDefinition/><ColumnDefinition Width='8'/><ColumnDefinition/></Grid.ColumnDefinitions>
      <Grid.RowDefinitions><RowDefinition Height='Auto'/><RowDefinition Height='Auto'/></Grid.RowDefinitions>
      <TextBlock x:Name='CountryLabel' Foreground='#555' FontSize='12' Margin='0,0,0,2'/>
      <TextBlock x:Name='RegionLabel' Grid.Column='2' Foreground='#555' FontSize='12' Margin='0,0,0,2'/>
      <ComboBox x:Name='CountryBox' Grid.Row='1' IsTextSearchEnabled='True' MaxDropDownHeight='360' VirtualizingStackPanel.IsVirtualizing='False'/>
      <ComboBox x:Name='RegionBox' Grid.Row='1' Grid.Column='2' IsTextSearchEnabled='True' MaxDropDownHeight='360' IsEnabled='False' VirtualizingStackPanel.IsVirtualizing='False'/>
    </Grid>
    <ListBox x:Name='Results' Grid.Row='4' Margin='0,8,0,8'/>
    <DockPanel Grid.Row='5'>
      <StackPanel DockPanel.Dock='Right' Orientation='Horizontal'>
        <Button x:Name='OkBtn' Padding='16,4' Margin='0,0,6,0' IsEnabled='False'/>
        <Button x:Name='CancelBtn' Padding='16,4' IsCancel='True'/>
      </StackPanel>
      <TextBlock x:Name='Info' Foreground='Gray' VerticalAlignment='Center' TextTrimming='CharacterEllipsis'/>
    </DockPanel>
  </Grid>
</Window>";

        sealed class CityChoice { public int Id; public string Name, Region, Country, Display; }

        static List<KeyValuePair<int, string>> countryCache;

        // ссылки вида «› Название» со страниц-списков сайта
        static List<KeyValuePair<string, string>> SiteLinks(string html, string hrefPattern)
        {
            var list = new List<KeyValuePair<string, string>>();
            foreach (Match m in Regex.Matches(html ?? "", @"<a[^>]+href=[""']([^""']*" + hrefPattern + @"[^""']*)[""'][^>]*>(.*?)</a>", RegexOptions.Singleline))
            {
                string text = WebUtility.HtmlDecode(Regex.Replace(m.Groups[2].Value, @"<[^>]+>", ""));
                if (!text.Contains("›")) continue;   // избранные города в шапке сайта — без «›»
                text = Regex.Replace(text.Replace("›", ""), @"\s+", " ").Trim();
                list.Add(new KeyValuePair<string, string>(WebUtility.HtmlDecode(m.Groups[1].Value), text));
            }
            return list;
        }

        // Сайт ищет только латиницей — переводим кириллицу (рус., каз., кырг.) в латиницу
        static string ToLatin(string text)
        {
            var map = new Dictionary<char, string> {
                {'а',"a"},{'ә',"a"},{'б',"b"},{'в',"v"},{'г',"g"},{'ғ',"g"},{'д',"d"},{'е',"e"},{'ё',"yo"},{'ж',"zh"},{'з',"z"},
                {'и',"i"},{'і',"i"},{'й',"y"},{'к',"k"},{'қ',"k"},{'л',"l"},{'м',"m"},{'н',"n"},{'ң',"n"},{'о',"o"},{'ө',"o"},
                {'п',"p"},{'р',"r"},{'с',"s"},{'т',"t"},{'у',"u"},{'ұ',"u"},{'ү',"u"},{'ў',"u"},{'ф',"f"},{'х',"kh"},{'һ',"h"},
                {'ц',"ts"},{'ч',"ch"},{'ш',"sh"},{'щ',"shch"},{'ъ',""},{'ы',"y"},{'ь',""},{'э',"e"},{'ю',"yu"},{'я',"ya"}
            };
            var sb = new StringBuilder();
            foreach (char ch in text)
            {
                string s;
                char lower = char.ToLowerInvariant(ch);
                if (map.TryGetValue(lower, out s))
                    sb.Append(char.IsUpper(ch) && s.Length > 0 ? char.ToUpperInvariant(s[0]) + s.Substring(1) : s);
                else sb.Append(ch);
            }
            return sb.ToString();
        }

        void ShowCityDialog() { ShowCityDialog(false); }

        void ShowCityDialog(bool firstRun)
        {
            var dlg = (Window)XamlReader.Parse(CityXaml);
            var query = (TextBox)dlg.FindName("Query");
            var results = (ListBox)dlg.FindName("Results");
            var okBtn = (Button)dlg.FindName("OkBtn");
            var info = (TextBlock)dlg.FindName("Info");
            var searchBtn = (Button)dlg.FindName("SearchBtn");
            var countryBox = (ComboBox)dlg.FindName("CountryBox");
            var regionBox = (ComboBox)dlg.FindName("RegionBox");
            dlg.Title = firstRun ? T("FirstRunTitle") : T("DlgTitle");
            ((TextBlock)dlg.FindName("Prompt")).Text = T("DlgPrompt");
            ((TextBlock)dlg.FindName("BrowseText")).Text = T("Browse");
            ((TextBlock)dlg.FindName("CountryLabel")).Text = T("Country");
            ((TextBlock)dlg.FindName("RegionLabel")).Text = T("Region");
            searchBtn.Content = T("Search");
            okBtn.Content = T("Choose");
            ((Button)dlg.FindName("CancelBtn")).Content = T("Cancel");
            CityChoice choice = null;
            // отменяют устаревшие ответы, если пользователь успел выбрать другое (у списка регионов — свой счётчик)
            int listToken = 0, regionToken = 0;

            Action<IEnumerable<CityChoice>, bool> showCities = (cities, withRegion) =>
            {
                results.Items.Clear();
                foreach (var c in cities)
                    results.Items.Add(new ListBoxItem { Content = withRegion && c.Region.Length > 0 ? c.Name + "  —  " + c.Region : (c.Display ?? c.Name), Tag = c });
                info.Text = results.Items.Count > 0 ? string.Format(T("Found"), results.Items.Count) : T("NotFound");
                if (results.Items.Count > 0) results.SelectedIndex = 0;
            };

            Action search = () =>
            {
                string q = ToLatin(query.Text.Trim());
                if (q.Length < 2) { info.Text = T("Min2"); return; }
                int token = ++listToken;
                results.Items.Clear();
                info.Text = T("Searching");
                Download(SiteRoot + "CitySearch.php?WSLanguage=EN&SearchText=" + Uri.EscapeDataString(q), (html, error) =>
                {
                    if (token != listToken) return;
                    if (error != null) { info.Text = T("NetError"); return; }
                    try
                    {
                        var found = new List<CityChoice>();
                        foreach (var link in SiteLinks(html, @"cityID=\d+"))
                        {
                            var parts = link.Value.Split(new[] { " / " }, 2, StringSplitOptions.None);
                            found.Add(new CityChoice {
                                Id = int.Parse(Regex.Match(link.Key, @"cityID=(\d+)").Groups[1].Value, Inv),
                                Name = parts[0].Trim(), Region = parts.Length > 1 ? parts[1].Trim() : "" });
                        }
                        // единственный результат — сайт сразу открывает страницу города
                        if (found.Count == 0 && PageField(html, "sehir") != null)
                        {
                            var id = Regex.Match(html, @"\?cityID=(\d+)&WSLanguage");
                            if (id.Success)
                                found.Add(new CityChoice { Id = int.Parse(id.Groups[1].Value, Inv), Name = PageField(html, "sehir"),
                                                           Region = PlaceText(PageField(html, "eyaletUlke")) });
                        }
                        // сайт ищет подстроку («ош» находит Moshi, Goshen…) — точные совпадения и начало названия ставим первыми
                        string ql = q.ToLowerInvariant();
                        Func<CityChoice, int> rank = c =>
                        {
                            string name = Regex.Replace(c.Name.ToLowerInvariant(), @"\s*\(.*$", "");
                            if (name == ql) return 0;
                            if (name.StartsWith(ql, StringComparison.Ordinal)) return 1;
                            if (Regex.IsMatch(c.Name.ToLowerInvariant(), @"(^|[\s(\-])" + Regex.Escape(ql))) return 2;
                            return 3;
                        };
                        showCities(found.OrderBy(rank).ToList(), true);
                    }
                    catch (Exception) { info.Text = T("NetError"); }
                });
            };

            Action loadCountries = () =>
            {
                Action<List<KeyValuePair<int, string>>> fill = list =>
                {
                    countryBox.Items.Clear();
                    var culture = LangCulture;
                    foreach (var c in list.Select(c => new { c.Key, English = c.Value, Name = Names.Country(c.Value, L.Code) })
                                          .OrderBy(c => L.Code != "en" && c.Name == c.English ? 1 : 0)   // без перевода — в конец
                                          .ThenBy(c => c.Name, StringComparer.Create(culture, true)))
                        countryBox.Items.Add(new ComboBoxItem { Content = c.Name, Tag = new object[] { c.Key, c.English } });
                    var current = countryBox.Items.Cast<ComboBoxItem>().FirstOrDefault(i => (int)((object[])i.Tag)[0] == settings.CountryId);
                    if (current != null) countryBox.SelectedItem = current;
                };
                if (countryCache != null) { fill(countryCache); return; }
                info.Text = T("ListLoading");
                Download(SiteRoot + "CountryList.php?WSLanguage=EN", (html, error) =>
                {
                    if (error != null) { info.Text = T("NetError"); return; }
                    var list = SiteLinks(html, @"StateList\.php\?countryID=\d+")
                        .Select(l => new KeyValuePair<int, string>(int.Parse(Regex.Match(l.Key, @"countryID=(\d+)").Groups[1].Value, Inv), l.Value))
                        .OrderBy(l => l.Value, StringComparer.CurrentCulture).ToList();
                    if (list.Count > 0) countryCache = list;
                    info.Text = "";
                    fill(list);
                });
            };

            countryBox.SelectionChanged += (s, e) =>
            {
                var item = countryBox.SelectedItem as ComboBoxItem;
                regionBox.Items.Clear();
                regionBox.IsEnabled = false;
                if (item == null) return;
                int token = ++regionToken;
                int countryId = (int)((object[])item.Tag)[0];
                string countryCode = Names.CountryCode((string)((object[])item.Tag)[1]);
                info.Text = T("ListLoading");
                Download(SiteRoot + "StateList.php?WSLanguage=EN&countryID=" + countryId.ToString(Inv), (html, error) =>
                {
                    if (token != regionToken) return;
                    if (error != null) { info.Text = T("NetError"); return; }
                    foreach (var link in SiteLinks(html, @"CityList\.php\?"))
                    {
                        var state = Regex.Match(link.Key, @"[?&]state=([^&]*)");
                        if (state.Success)
                            regionBox.Items.Add(new ComboBoxItem { Content = Names.Region(link.Value, countryCode, L.Code),
                                                                   Tag = new[] { countryId.ToString(Inv), state.Groups[1].Value, link.Value } });
                    }
                    regionBox.IsEnabled = regionBox.Items.Count > 0;
                    info.Text = "";
                    if (regionBox.Items.Count == 1) regionBox.SelectedIndex = 0;
                });
            };

            regionBox.SelectionChanged += (s, e) =>
            {
                var item = regionBox.SelectedItem as ComboBoxItem;
                if (item == null) return;
                var tag = (string[])item.Tag;
                string regionName = tag[2];
                string countryName = countryBox.SelectedItem is ComboBoxItem ? (string)((object[])((ComboBoxItem)countryBox.SelectedItem).Tag)[1] : "";
                string countryCode = Names.CountryCode(countryName);
                int token = ++listToken;
                results.Items.Clear();
                info.Text = T("ListLoading");
                Download(SiteRoot + "CityList.php?WSLanguage=EN&countryID=" + tag[0] + "&state=" + Uri.EscapeDataString(tag[1]), (html, error) =>
                {
                    if (token != listToken) return;
                    if (error != null) { info.Text = T("NetError"); return; }
                    showCities(SiteLinks(html, @"cityID=\d+").Select(l => new CityChoice {
                        Id = int.Parse(Regex.Match(l.Key, @"cityID=(\d+)").Groups[1].Value, Inv),
                        Name = l.Value, Region = regionName, Country = countryName,
                        Display = Names.City(l.Value, countryCode, L.Code) }).ToList(), false);
                });
            };

            Action accept = () =>
            {
                var item = results.SelectedItem as ListBoxItem;
                if (item == null || choice != null) return;   // двойной щелчок и Enter не должны закрыть окно дважды
                choice = (CityChoice)item.Tag;
                dlg.Close();
            };
            searchBtn.Click += (s, e) => search();
            query.KeyDown += (s, e) => { if (e.Key == Key.Enter) { search(); e.Handled = true; } };
            results.SelectionChanged += (s, e) => okBtn.IsEnabled = results.SelectedItem != null;
            results.MouseDoubleClick += (s, e) => accept();
            results.KeyDown += (s, e) => { if (e.Key == Key.Enter) accept(); };
            okBtn.Click += (s, e) => accept();
            dlg.ContentRendered += (s, e) => { query.Focus(); loadCountries(); };

            dialogDepth++;
            bool? picked;
            try { picked = dlg.ShowDialog(); } finally { dialogDepth--; window.Activate(); }
            if (choice != null)
            {
                settings.CityId = choice.Id;
                settings.CityName = choice.Name;
                settings.CityRegion = choice.Region;
                settings.CountryName = choice.Country ?? "";   // при поиске страну уточнит страница города
                SaveSettings();
                yearDays = null;
                yearXml = null;
                dataYear = 0;
                serverOldYear = false;
                hijriFor = "";
                hijriOffset = 0;
                fetchError = false;
                BuildRows();
                RestartFetch();
                UpdateView();
            }
        }

        // ---------- время жамагата ----------
        const string JamaatXaml = @"
<Window xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        Width='500' SizeToContent='Height' FontFamily='Segoe UI' FontSize='13'
        WindowStartupLocation='CenterScreen' ResizeMode='NoResize' Topmost='True' ShowInTaskbar='True'>
  <StackPanel Margin='16'>
    <TextBlock x:Name='Hint' TextWrapping='Wrap' Foreground='#495057' Margin='0,0,0,12'/>
    <Grid x:Name='Fields'>
      <Grid.ColumnDefinitions><ColumnDefinition/><ColumnDefinition Width='Auto'/><ColumnDefinition Width='Auto'/><ColumnDefinition Width='80'/></Grid.ColumnDefinitions>
    </Grid>
    <Grid Margin='0,12,0,0'>
      <Grid.ColumnDefinitions><ColumnDefinition/><ColumnDefinition Width='Auto'/></Grid.ColumnDefinitions>
      <TextBlock x:Name='BeforeLabel' VerticalAlignment='Center'/>
      <ComboBox x:Name='BeforeBox' Grid.Column='1' MinWidth='180'/>
    </Grid>
    <TextBlock x:Name='Error' Foreground='#DC3545' TextWrapping='Wrap' Margin='0,10,0,0' Visibility='Collapsed'/>
    <StackPanel Orientation='Horizontal' HorizontalAlignment='Right' Margin='0,14,0,0'>
      <Button x:Name='OkBtn' Padding='16,4' Margin='0,0,6,0' IsDefault='True'/>
      <Button x:Name='CancelBtn' Padding='16,4' IsCancel='True'/>
    </StackPanel>
  </StackPanel>
</Window>";

        void ShowJamaatDialog()
        {
            var dlg = (Window)XamlReader.Parse(JamaatXaml);
            dlg.Title = T("JamaatTitle");
            ((TextBlock)dlg.FindName("Hint")).Text = T("JamaatHint");
            ((TextBlock)dlg.FindName("BeforeLabel")).Text = T("JamaatBefore");
            ((Button)dlg.FindName("OkBtn")).Content = T("Save");
            ((Button)dlg.FindName("CancelBtn")).Content = T("Cancel");
            var fields = (Grid)dlg.FindName("Fields");
            var error = (TextBlock)dlg.FindName("Error");

            var azan = GetEvents().ToDictionary(e => e.Key, e => e.Time.ToString("HH:mm", Inv));
            var boxes = new Dictionary<string, TextBox>();
            var azanChecks = new Dictionary<string, CheckBox>();
            foreach (var key in JamaatKeys)
            {
                int row = fields.RowDefinitions.Count;
                fields.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var label = new TextBlock { Text = key == "juma" ? T("Juma") : L.Prayers[key], VerticalAlignment = VerticalAlignment.Center };
                string azanTime = azan.ContainsKey(key == "juma" ? "besin" : key) ? azan[key == "juma" ? "besin" : key] : null;
                var hint = new TextBlock {
                    Text = azanTime == null ? "" : string.Format(T("Azan"), azanTime), Foreground = Brushes["Inactive"],
                    VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 10, 0)
                };
                string current;
                var box = new TextBox {
                    Text = settings.Jamaat.TryGetValue(key, out current) ? current : "", Padding = new Thickness(4, 3, 4, 3),
                    Margin = new Thickness(0, 3, 0, 3), MaxLength = 5, HorizontalContentAlignment = HorizontalAlignment.Center
                };
                box.GotKeyboardFocus += (s, e) => box.SelectAll();
                // «по азану» — жамагат в момент азана, своё время не вводится
                var byAzan = new CheckBox {
                    Content = T("ByAzan"), IsChecked = settings.JamaatAzan.Contains(key),
                    VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0)
                };
                box.IsEnabled = byAzan.IsChecked != true;
                byAzan.Checked += (s, e) => box.IsEnabled = false;
                byAzan.Unchecked += (s, e) => box.IsEnabled = true;
                Grid.SetRow(label, row); Grid.SetRow(hint, row); Grid.SetRow(byAzan, row); Grid.SetRow(box, row);
                Grid.SetColumn(hint, 1); Grid.SetColumn(byAzan, 2); Grid.SetColumn(box, 3);
                fields.Children.Add(label); fields.Children.Add(hint); fields.Children.Add(byAzan); fields.Children.Add(box);
                boxes[key] = box;
                azanChecks[key] = byAzan;
            }

            var beforeBox = (ComboBox)dlg.FindName("BeforeBox");
            beforeBox.Items.Add(new ComboBoxItem { Content = T("JamaatNoBefore"), Tag = 0 });
            foreach (int m in JamaatBeforeOptions)
                beforeBox.Items.Add(new ComboBoxItem { Content = string.Format(T("JamaatBeforeItem"), m), Tag = m });
            beforeBox.SelectedItem = beforeBox.Items.Cast<ComboBoxItem>().FirstOrDefault(i => (int)i.Tag == settings.JamaatBefore) ?? beforeBox.Items[0];

            ((Button)dlg.FindName("OkBtn")).Click += (s, e) =>
            {
                var result = new Dictionary<string, string>();
                var azanKeys = new HashSet<string>(azanChecks.Where(p => p.Value.IsChecked == true).Select(p => p.Key));
                foreach (var pair in boxes)
                {
                    if (azanKeys.Contains(pair.Key)) continue;
                    string text = pair.Value.Text.Trim();
                    if (text.Length == 0) continue;
                    string hm = NormalizeTime(text);
                    if (hm == null)
                    {
                        error.Text = string.Format(T("BadTime"), text);
                        error.Visibility = Visibility.Visible;
                        pair.Value.Focus();
                        return;
                    }
                    result[pair.Key] = hm;
                }
                settings.Jamaat = result;
                settings.JamaatAzan = azanKeys;
                settings.JamaatBefore = (int)((ComboBoxItem)beforeBox.SelectedItem).Tag;
                dlg.DialogResult = true;
            };
            dlg.ContentRendered += (s, e) => boxes["bamdat"].Focus();

            dialogDepth++;
            bool? saved;
            try { saved = dlg.ShowDialog(); } finally { dialogDepth--; window.Activate(); }
            if (saved == true)
            {
                SaveSettings();
                BuildRows();
                UpdateView();
            }
        }

        // ---------- автозагрузка ----------
        static string ExePath { get { return System.Reflection.Assembly.GetExecutingAssembly().Location; } }

        static bool AutostartEnabled
        {
            get
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKey))
                    return key != null && key.GetValue(RunName) != null;
            }
        }

        static void SetAutostart(bool enable)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(RunKey))
                {
                    if (enable) key.SetValue(RunName, "\"" + ExePath + "\"");
                    else key.DeleteValue(RunName, false);
                }
            }
            catch (Exception) { }
        }

        // старая версия (PowerShell) запускалась ярлыком из папки «Автозагрузка»
        static void MigrateAutostart()
        {
            try
            {
                string lnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "NamazWidget.lnk");
                if (File.Exists(lnk)) { File.Delete(lnk); SetAutostart(true); }
                else if (AutostartEnabled) SetAutostart(true);   // обновить путь, если exe перенесли
            }
            catch (Exception) { }
        }

        // ---------- трей и меню ----------
        static Gdi.Icon CrescentIcon()
        {
            var bmp = new Gdi.Bitmap(32, 32);
            using (var g = Gdi.Graphics.FromImage(bmp))
            using (var green = new Gdi.SolidBrush(Gdi.Color.FromArgb(84, 188, 117)))
            {
                g.SmoothingMode = Gdi.Drawing2D.SmoothingMode.AntiAlias;
                g.FillEllipse(green, 0, 0, 31, 31);
                g.FillEllipse(Gdi.Brushes.White, 6, 6, 20, 20);
                g.FillEllipse(green, 11, 3, 19, 19);
            }
            return Gdi.Icon.FromHandle(bmp.GetHicon());
        }

        // Полный вид — когда пользователь работает с виджетом; стоит переключиться на другую программу —
        // остаётся компактная карточка: кольцо с обратным отсчётом, список намазов, город и жамагат.
        const string CardXaml = @"
<Window xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        Title='namazvakti.com card' SizeToContent='WidthAndHeight'
        WindowStyle='SingleBorderWindow' AllowsTransparency='False' Background='Transparent'
        ShowInTaskbar='False' ShowActivated='False' ResizeMode='NoResize' Cursor='Hand'
        FontFamily='Segoe UI Variable Display, Segoe UI' UseLayoutRounding='True' TextOptions.TextFormattingMode='Ideal'>
  <WindowChrome.WindowChrome>
    <WindowChrome CaptionHeight='0' GlassFrameThickness='0' ResizeBorderThickness='0' CornerRadius='0' UseAeroCaptionButtons='False'/>
  </WindowChrome.WindowChrome>
  <Window.Resources>
    <LinearGradientBrush x:Key='CapsuleRim' StartPoint='0,0' EndPoint='0,1'>
      <GradientStop Color='#8CFFFFFF' Offset='0'/>
      <GradientStop Color='#14FFFFFF' Offset='0.5'/>
      <GradientStop Color='#40FFFFFF' Offset='1'/>
    </LinearGradientBrush>
  </Window.Resources>
  <Border x:Name='Surface' CornerRadius='8' BorderThickness='1.5'>
    <Border.Resources>
      <Style TargetType='TextBlock'>
        <Setter Property='Foreground' Value='#F5F5F7'/>
        <Setter Property='Effect'>
          <Setter.Value><DropShadowEffect Color='Black' BlurRadius='6' ShadowDepth='0' Opacity='0.55'/></Setter.Value>
        </Setter>
      </Style>
    </Border.Resources>
    <Border.BorderBrush>
      <LinearGradientBrush StartPoint='0,0' EndPoint='1,1'>
        <GradientStop Color='#B3FFFFFF' Offset='0'/>
        <GradientStop Color='#26FFFFFF' Offset='0.3'/>
        <GradientStop Color='#0DFFFFFF' Offset='0.6'/>
        <GradientStop Color='#59FFFFFF' Offset='1'/>
      </LinearGradientBrush>
    </Border.BorderBrush>
    <Grid>
      <Grid.LayoutTransform><ScaleTransform ScaleX='0.8' ScaleY='0.8'/></Grid.LayoutTransform>
      <Border VerticalAlignment='Top' Height='80' IsHitTestVisible='False'>
        <Border.Background>
          <LinearGradientBrush StartPoint='0,0' EndPoint='0,1'>
            <GradientStop Color='#2EFFFFFF' Offset='0'/>
            <GradientStop Color='#00FFFFFF' Offset='1'/>
          </LinearGradientBrush>
        </Border.Background>
      </Border>
      <Grid Margin='22,16,24,16'>
        <Grid.ColumnDefinitions><ColumnDefinition Width='Auto'/><ColumnDefinition Width='Auto'/><ColumnDefinition Width='Auto'/></Grid.ColumnDefinitions>

        <!-- слева: сколько осталось до следующего намаза -->
        <StackPanel VerticalAlignment='Center' MinWidth='150'>
          <TextBlock x:Name='CardCountdown' FontSize='40' FontWeight='SemiBold' HorizontalAlignment='Center'/>
          <TextBlock x:Name='CardUntil' FontSize='14' Foreground='#AEAEB2' HorizontalAlignment='Center' TextAlignment='Center'
                     TextWrapping='Wrap' MaxWidth='170' Margin='0,2,0,0'/>
        </StackPanel>

        <Border Grid.Column='1' Width='1' Background='#26FFFFFF' Margin='20,4,20,4'/>

        <!-- справа: текущий и следующий намаз, время азана и жамагата -->
        <Grid Grid.Column='2' VerticalAlignment='Center'>
          <Grid.ColumnDefinitions><ColumnDefinition Width='Auto' MinWidth='80'/><ColumnDefinition Width='Auto'/><ColumnDefinition Width='Auto'/></Grid.ColumnDefinitions>
          <Grid.RowDefinitions><RowDefinition Height='Auto'/><RowDefinition Height='Auto'/><RowDefinition Height='Auto'/></Grid.RowDefinitions>
          <TextBlock x:Name='CardJamaatHead' Grid.Column='2' FontSize='12' FontWeight='SemiBold' Foreground='#64B5FF' HorizontalAlignment='Right'
                     Margin='18,0,0,2'/>
          <TextBlock x:Name='CardCurName' Grid.Row='1' FontSize='18' FontWeight='Bold' VerticalAlignment='Center' Margin='0,3,16,3'/>
          <TextBlock x:Name='CardCurTime' Grid.Row='1' Grid.Column='1' FontSize='20' FontWeight='Bold' HorizontalAlignment='Right' VerticalAlignment='Center'/>
          <TextBlock x:Name='CardCurJamaat' Grid.Row='1' Grid.Column='2' FontSize='18' FontWeight='SemiBold' Foreground='#64B5FF'
                     HorizontalAlignment='Right' VerticalAlignment='Center' Margin='18,0,0,0'/>
          <TextBlock x:Name='CardNextName' Grid.Row='2' FontSize='18' VerticalAlignment='Center' Margin='0,3,16,3'/>
          <TextBlock x:Name='CardNextTime' Grid.Row='2' Grid.Column='1' FontSize='20' FontWeight='SemiBold' HorizontalAlignment='Right' VerticalAlignment='Center'/>
          <TextBlock x:Name='CardNextJamaat' Grid.Row='2' Grid.Column='2' FontSize='18' FontWeight='SemiBold' Foreground='#64B5FF'
                     HorizontalAlignment='Right' VerticalAlignment='Center' Margin='18,0,0,0'/>
        </Grid>
      </Grid>
    </Grid>
  </Border>
</Window>";

        Window card;
        ContextMenu mainMenu;
        int dialogDepth;   // пока открыт диалог (город, жамагат) — полный вид не сворачиваем

        T2 CardEl<T2>(string name) where T2 : class { return card.FindName(name) as T2; }

        void InitCard()
        {
            card = (Window)XamlReader.Parse(CardXaml);
            var surface = CardEl<Border>("Surface");
            Glass.Apply(card, surface, B("#F2262628"), settings.Transparency < 3);
            Glass.NoActivate(card);
            ApplyTint(surface);
            card.SizeChanged += (s, e) => { if (card.IsVisible) PlaceCard(); };
            // нажатие — полный вид с раскрытым списком времён
            card.MouseLeftButtonDown += (s, e) =>
            {
                if (!FreePosition) { ShowFull(); return; }
                double left = card.Left, top = card.Top;
                try { card.DragMove(); } catch (InvalidOperationException) { }
                if (Math.Abs(card.Left - left) < 1 && Math.Abs(card.Top - top) < 1) { ShowFull(); return; }
                KeepInsideScreen(card);
                settings.Left = Math.Round(card.Left);
                settings.Top = Math.Round(card.Top);
                SaveSettings();
            };
            window.Deactivated += (s, e) => dispatcher.BeginInvoke(new Action(ShowCompact), DispatcherPriority.Background);
        }

        void UpdateCard(Period period, DateTime now, List<Ev> events)
        {
            if (card == null) return;
            // время жамагата намаза по ключу (null — не задан)
            Func<string, string> jamaatOf = key =>
            {
                var ev = events.FirstOrDefault(e => e.Key == key);
                var at = ev == null ? null : JamaatTime(ev);
                return at.HasValue ? at.Value.ToString("HH:mm", Inv) : "";
            };
            string curJamaat = "", nextJamaat = "";
            if (period != null)
            {
                TimeSpan left = period.To - now;
                if (left < TimeSpan.Zero) left = TimeSpan.Zero;
                CardEl<TextBlock>("CardCountdown").Text = string.Format(Inv, "{0:00}:{1:00}:{2:00}", (int)left.TotalHours, left.Minutes, left.Seconds);
                CardEl<TextBlock>("CardUntil").Text = string.Format(T("Until"), period.NextName);
                string curKey = period.CompactKey;
                var ink = Brushes["Ink" + period.Color];
                var start = events.FirstOrDefault(e => e.Key == curKey);
                CardEl<TextBlock>("CardCurName").Text = L.Prayers[curKey];
                CardEl<TextBlock>("CardCurTime").Text = (start != null ? start.Time : period.From).ToString("HH:mm", Inv);
                CardEl<TextBlock>("CardCurName").Foreground = ink;
                CardEl<TextBlock>("CardCurTime").Foreground = ink;
                CardEl<TextBlock>("CardNextName").Text = period.NextName;
                CardEl<TextBlock>("CardNextTime").Text = period.To.ToString("HH:mm", Inv);
                curJamaat = jamaatOf(curKey);
                nextJamaat = jamaatOf(period.NextKey);
            }
            else
            {
                CardEl<TextBlock>("CardCountdown").Text = "—";
                CardEl<TextBlock>("CardUntil").Text = T("Loading");
                foreach (var n in new[] { "CardCurName", "CardCurTime", "CardNextName", "CardNextTime" }) CardEl<TextBlock>(n).Text = "";
            }
            CardEl<TextBlock>("CardCurJamaat").Text = curJamaat;
            CardEl<TextBlock>("CardNextJamaat").Text = nextJamaat;
            CardEl<TextBlock>("CardJamaatHead").Text = curJamaat.Length + nextJamaat.Length > 0 ? T("Jamaat") : "";
        }

        DateTime fullShownAt;

        public void ShowFull()
        {
            fullShownAt = DateTime.Now;
            if (FreePosition && card != null && card.IsVisible) { window.Left = card.Left; window.Top = card.Top; }
            window.Show();
            window.UpdateLayout();
            DockToCorner();
            window.Activate();
            if (card != null) card.Hide();
        }

        void ShowCompact()
        {
            if (card == null || !window.IsVisible || window.IsActive || dialogDepth > 0 || (mainMenu != null && mainMenu.IsOpen)) return;
            // только что открыли полный вид — не сворачиваем от мгновенной смены фокуса
            if ((DateTime.Now - fullShownAt).TotalMilliseconds < 500) return;
            card.Topmost = settings.Topmost;   // булавка: поверх всех окон или вместе с остальными
            if (FreePosition) { settings.Left = Math.Round(window.Left); settings.Top = Math.Round(window.Top); SaveSettings(); }
            card.Show();
            card.UpdateLayout();
            PlaceCard();
            window.Hide();
        }

        // карточка — в выбранном углу или на своём месте (свободное положение)
        void PlaceCard()
        {
            if (!FreePosition) { AnchorCorner(card); return; }
            card.Left = settings.Left ?? window.Left;
            card.Top = settings.Top ?? window.Top;
            KeepInsideScreen(card);
        }

        public void ShowWidget() { ShowFull(); }

        // Свернуть в трей. Значок в Windows 11 часто спрятан за «˄», поэтому первые разы подсказываем, где его искать
        // (и что виджет откроется, если снова запустить программу).
        void HideToTray()
        {
            window.Hide();
            card.Hide();
            if (settings.TrayHints >= 3 || TestMode) return;
            settings.TrayHints++;
            SaveSettings();
            tray.ShowBalloonTip(10000, T("TrayHintTitle"), T("TrayHint"), WinForms.ToolTipIcon.Info);
        }

        void ExitWidget()
        {
            timer.Stop();
            ClosePopup();
            tray.Visible = false;
            tray.Dispose();
            Application.Current.Shutdown();
        }

        void OpenSite()
        {
            try { System.Diagnostics.Process.Start(SiteRoot + "Main.php?cityID=" + settings.CityId.ToString(Inv)); }
            catch (Exception) { }
        }

        public void SetLanguage(string code)
        {
            if (I18n.Find(code) == null || code == settings.Lang) return;
            settings.Lang = code;
            SaveSettings();
            BuildMenus();
            UpdateTexts();
            BuildRows();
            trayText = "";
            UpdateView();
        }

        public void TestNotification()
        {
            int minutes = Math.Max(3, settings.NotifyBefore);
            ShowNotification(L.Prayers["besin"], string.Format(T("NotifyBeforeMsg"), minutes), CityTitle + " · 12:00", Brushes["Green"]);
        }

        static MenuItem Item(string header, Action onClick)
        {
            var item = new MenuItem { Header = header };
            if (onClick != null) item.Click += (s, e) => onClick();
            return item;
        }

        static MenuItem Check(string header, bool isChecked, Action<bool> onClick)
        {
            var item = new MenuItem { Header = header, IsCheckable = true, IsChecked = isChecked };
            item.Click += (s, e) => onClick(item.IsChecked);
            return item;
        }

        static MenuItem Radio(string header, bool isChecked, Action onClick)
        {
            var item = new MenuItem { Header = header, IsCheckable = true, IsChecked = isChecked };
            item.Click += (s, e) => onClick();
            return item;
        }

        void BuildMenus()
        {
            var menu = new ContextMenu();
            menu.Items.Add(Item(T("ChangeCity"), ShowCityDialog));
            menu.Items.Add(Item(T("JamaatMenu"), ShowJamaatDialog));

            var langMenu = Item(T("Language"), null);
            foreach (var lang in I18n.All)
            {
                string code = lang.Code;
                langMenu.Items.Add(Radio(lang.Label, code == settings.Lang, () => SetLanguage(code)));
            }
            menu.Items.Add(langMenu);

            var posMenu = Item(T("Position"), null);
            string[] posNames = { "PosBR", "PosTR", "PosBL", "PosTL", "PosFree" };
            for (int i = 0; i < Positions.Length; i++)
            {
                string position = Positions[i];
                if (position == "Free") posMenu.Items.Add(new Separator());
                posMenu.Items.Add(Radio(T(posNames[i]), settings.Position == position, () => SetPosition(position)));
            }
            menu.Items.Add(posMenu);

            var transMenu = Item(T("Transparency"), null);
            string[] levels = { "TransLow", "TransMid", "TransHigh", "TransMax" };
            for (int i = 0; i < levels.Length; i++)
            {
                int level = i;
                transMenu.Items.Add(Radio(T(levels[i]), settings.Transparency == level, () => SetTransparency(level)));
            }
            menu.Items.Add(transMenu);

            var notifyMenu = Item(T("Notifications"), null);
            notifyMenu.Items.Add(Check(T("NotifyAtTime"), settings.Notify, v => { settings.Notify = v; SaveSettings(); }));
            notifyMenu.Items.Add(Check(T("WinNotify"), settings.WinNotify, v => { settings.WinNotify = v; SaveSettings(); }));
            notifyMenu.Items.Add(new Separator());
            notifyMenu.Items.Add(Radio(T("NoBefore"), settings.NotifyBefore == 0, () => { settings.NotifyBefore = 0; SaveSettings(); BuildMenus(); }));
            foreach (int minutes in BeforeOptions)
            {
                int m = minutes;
                notifyMenu.Items.Add(Radio(string.Format(T("Before"), m), settings.NotifyBefore == m,
                    () => { settings.NotifyBefore = m; SaveSettings(); BuildMenus(); }));
            }
            notifyMenu.Items.Add(new Separator());
            var soundMenu = Item(T("SoundMenu"), null);
            foreach (var sound in new[] { new[] { "bell", T("SoundBell") }, new[] { "soft", T("SoundSoft") }, new[] { "system", T("SoundSystem") } })
            {
                string code = sound[0];
                soundMenu.Items.Add(Radio(sound[1], settings.Sound == code, () => { settings.Sound = code; SaveSettings(); BuildMenus(); PlaySound(); }));
            }
            notifyMenu.Items.Add(soundMenu);
            notifyMenu.Items.Add(new Separator());
            notifyMenu.Items.Add(Item(T("TestNotify"), TestNotification));
            menu.Items.Add(notifyMenu);
            menu.Items.Add(new Separator());

            menu.Items.Add(Check(T("Autostart"), AutostartEnabled, SetAutostart));
            menu.Items.Add(new Separator());
            menu.Items.Add(Item(T("Refresh"), RestartFetch));
            menu.Items.Add(Item(T("OpenSite"), OpenSite));
            menu.Items.Add(Item(T("Exit"), ExitWidget));
            mainMenu = menu;   // не ContextMenu окна: правый клик по виджету меню не открывает

            var strip = tray.ContextMenuStrip;
            strip.Items.Clear();
            strip.Items.Add(T("TrayToggle"), null, (s, e) => { if (window.IsVisible || card.IsVisible) { window.Hide(); card.Hide(); } else ShowFull(); });
            strip.Items.Add(T("ChangeCity"), null, (s, e) => ShowCityDialog());
            strip.Items.Add(T("JamaatMenu"), null, (s, e) => ShowJamaatDialog());
            var langStrip = new WinForms.ToolStripMenuItem(T("Language"));
            foreach (var lang in I18n.All)
            {
                string code = lang.Code;
                var li = new WinForms.ToolStripMenuItem(lang.Label, null, (s, e) => SetLanguage(code));
                li.Checked = code == settings.Lang;
                langStrip.DropDownItems.Add(li);
            }
            strip.Items.Add(langStrip);
            strip.Items.Add(T("TestNotify"), null, (s, e) => TestNotification());
            strip.Items.Add(T("OpenSite"), null, (s, e) => OpenSite());
            strip.Items.Add(new WinForms.ToolStripSeparator());
            strip.Items.Add(T("Exit"), null, (s, e) => ExitWidget());
        }

        public void LogError(Exception ex)
        {
            try { File.AppendAllText(Path.Combine(appDir, "error.log"), DateTime.Now.ToString("s", Inv) + " " + ex + Environment.NewLine); }
            catch (Exception) { }
        }

#if SELFTEST
        public static void DoEvents()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }

        public void SelfTestSetup(int cityId, string lang)
        {
            settings.CityId = cityId; settings.CityName = ""; settings.CityRegion = "";
            settings.Lang = lang; settings.NotifyBefore = 5; settings.WinNotify = false;
            yearDays = null; hijriFor = "";
            BuildMenus(); UpdateTexts(); RestartFetch();
        }

        public string Snapshot()
        {
            var sb = new StringBuilder();
            sb.Append(Tb("CurrentName").Text).Append(" | ").Append(Tb("LeftLabel").Text).Append(' ').Append(Tb("LeftValue").Text)
              .Append(" | ").Append(Tb("NextText").Text).Append(" | bar=").Append(Arc("ArcFg").Stroke).Append(' ').Append(Tb("PercentText").Text)
              .Append(" | ").Append(Tb("MiladiText").Text).Append(" | ").Append(Tb("HijriText").Text).Append(" | ").Append(Tb("RegionText").Text)
              .Append(" | active=");
            foreach (var r in rows) if (r.Value.Border.Background != System.Windows.Media.Brushes.Transparent) sb.Append(r.Key).Append(':').Append(r.Value.Border.Background);
            return sb.ToString();
        }

        public void SetLanguageForTest(string code) { SetLanguage(code); }
        public void BuildRowsForTest() { BuildRows(); }
#endif
    }

    static class Program
    {
        const string ShowEventName = @"Local\NamazTimesKzWidget.Show";

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                // TLS 1.2 (на .NET 4.0 без обновления значение не поддерживается — тогда TLS 1.0)
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | (SecurityProtocolType)768 | SecurityProtocolType.Tls;
            }
            catch (NotSupportedException)
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
            }

#if SELFTEST
            SelfTest.Run(args[0]);
            return;
#elif CARDTEST
            // проверка карточки в заданное время: args[0] — папка настроек, args[1] — «ЧЧ:ММ» или «ГГГГ-ММ-ДД ЧЧ:ММ»
            var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            var widget = new Widget(args[0]);
            widget.FakeNow = args[1].Contains("-") ? DateTime.Parse(args[1], CultureInfo.InvariantCulture)
                                                   : DateTime.Today.Add(TimeSpan.Parse(args[1], CultureInfo.InvariantCulture));
            widget.Init(true);
            app.Run();
#elif POPUPTEST
            // проверка вида уведомления: отдельная папка настроек, окно виджета не показывается
            var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            var widget = new Widget(args[0]);
            widget.Init(false);
            widget.TestNotification();
            app.Run();
#else
            bool created;
            using (var mutex = new Mutex(true, @"Local\NamazTimesKzWidget", out created))
            {
                if (!created)
                {
                    try { using (var show = EventWaitHandle.OpenExisting(ShowEventName)) show.Set(); }
                    catch (Exception) { }
                    return;
                }
                string appDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NamazWidget");
                var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                var widget = new Widget(appDir);
                app.DispatcherUnhandledException += (s, e) => { widget.LogError(e.Exception); e.Handled = true; };
                widget.Init(true);
                var showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
                ThreadPool.RegisterWaitForSingleObject(showEvent,
                    (state, timedOut) => app.Dispatcher.BeginInvoke(new Action(widget.ShowWidget)), null, -1, false);
                app.Run();
            }
#endif
        }
    }
}

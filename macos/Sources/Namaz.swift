// Виджет «Время намаза» для macOS — данные с https://namazvakti.com
// Порт NamazWidget.cs (Windows): стеклянное окно на рабочем столе (полный вид и компактная карточка),
// значок-полумесяц в строке меню, уведомления о намазе и жамагате, работа без интернета до конца года.

import AppKit
import SwiftUI

// Type — цвет как на namaztimes.kz: Green — намаз, Red — карахат (нежелательное время), Orange — ишрак.
struct PrayerDef { let key, type: String; let tracked: Bool }
// Периоды прогресс-кольца: start — начало периода, from/to — границы кольца и отсчёта.
struct PeriodDef { let start, color, from, to: String }

struct Ev { let key, name, type: String; let tracked: Bool; let time: Date }

struct Period {
    let key, name, color, nextName, nextKey: String
    let compactKey: String   // для карточки: только семь времён
    let from, to: Date
}

struct NSettings {
    var cityId = 8408, countryId = 99
    var cityName = "Almaty", cityRegion = "Almaty", countryName = "Kazakhstan", lang = "", sound = "bell"
    var trayHints = 0
    var cityChosen = false       // первый запуск: страну и город выбирают один раз, дальше всё работает без интернета
    var transparency = 2         // 0 — низкая … 3 — максимальная
    var notify = true, winNotify = true
    var notifyBefore = 5
    var jamaat: [String: String] = [:]   // время жамагата: ключ намаза (или "juma") → "HH:mm" или Namaz.atAzan
    var jamaatBefore = 10
}

struct RowState: Identifiable {
    let id: String
    let name, time: String
    let jamaat: String?
    let type: String
    let prayer, active: Bool
}

// всё, что показывают окна, — пересчитывается раз в секунду
struct ViewState {
    var transparency = 2
    var city = "", region = "", clock = "", weekday = "", miladi = "", hijri = ""
    var miladiLabel = "", hijriLabel = "", timesTitle = "", jamaatHeader = ""
    var menuTip = "", jamaatTip = ""
    var currentName = "—", currentInk = "TextGreen", leftLabel = "", leftValue = "", nextText = ""
    var hasArc = false, arc = 0.0, bar = "Green", percentText = "", percentInk = "InkGreen"
    var jamaatPill: String?
    var status: String?
    var rows: [RowState] = []
    var cardCountdown = "—", cardUntil = "", cardCurName = "", cardCurTime = "", cardNextName = "", cardNextTime = ""
    var cardInk = "InkGreen", cardCurJamaat = "", cardNextJamaat = "", cardJamaatHead = ""
}

struct PopupState {
    var caption = "", title = "", message = "", close = ""
    var accent = "Green"
}

// ---------- регулярные выражения ----------
func rx(_ text: String, _ pattern: String, dotAll: Bool = false) -> [[String]] {
    guard let re = try? NSRegularExpression(pattern: pattern, options: dotAll ? [.dotMatchesLineSeparators] : []) else { return [] }
    return re.matches(in: text, range: NSRange(text.startIndex..., in: text)).map { m in
        (0..<m.numberOfRanges).map { i in Range(m.range(at: i), in: text).map { String(text[$0]) } ?? "" }
    }
}

func rxFirst(_ text: String, _ pattern: String) -> [String]? { rx(text, pattern).first }

func htmlDecode(_ s: String) -> String {
    var out = s.replacingOccurrences(of: "&nbsp;", with: " ").replacingOccurrences(of: "&quot;", with: "\"")
        .replacingOccurrences(of: "&#39;", with: "'").replacingOccurrences(of: "&apos;", with: "'")
        .replacingOccurrences(of: "&lt;", with: "<").replacingOccurrences(of: "&gt;", with: ">")
        .replacingOccurrences(of: "&rsaquo;", with: "›").replacingOccurrences(of: "&rsquo;", with: "’")
    for m in rx(out, "&#(x?)([0-9a-fA-F]+);").reversed() {
        if let code = UInt32(m[2], radix: m[1].isEmpty ? 10 : 16), let u = Unicode.Scalar(code) {
            out = out.replacingOccurrences(of: m[0], with: String(Character(u)))
        }
    }
    return out.replacingOccurrences(of: "&amp;", with: "&")
}

final class Namaz: NSObject, ObservableObject, NSPopoverDelegate {
    static let siteRoot = "https://namazvakti.com/"
    static let userAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_0) NamazTimesWidget/2.0"
    static let launchLabel = "com.asror.NamazWidget"
    static let endWarningMinutes = 15.0   // последние минуты намаза — кольцо красное
    static let beforeOptions = [3, 5, 10, 15]
    static let jamaatBeforeOptions = [3, 5, 10, 15, 20, 30]
    static let atAzan = "azan"   // жамагат в момент азана: время берётся из расписания
    static let jamaatKeys = ["bamdat", "besin", "juma", "ekindi", "aqsham", "quptan"]

    static let prayers = [
        PrayerDef(key: "imsak", type: "Red", tracked: false), PrayerDef(key: "bamdat", type: "Green", tracked: true),
        PrayerDef(key: "kun", type: "Red", tracked: true), PrayerDef(key: "ishraq", type: "Orange", tracked: false),
        PrayerDef(key: "kerahat", type: "Red", tracked: false), PrayerDef(key: "besin", type: "Green", tracked: true),
        PrayerDef(key: "asriauual", type: "Orange", tracked: false), PrayerDef(key: "ekindi", type: "Green", tracked: true),
        PrayerDef(key: "isfirar", type: "Red", tracked: false), PrayerDef(key: "aqsham", type: "Green", tracked: true),
        PrayerDef(key: "ishtibaq", type: "Red", tracked: false), PrayerDef(key: "quptan", type: "Green", tracked: true),
        PrayerDef(key: "ishaisani", type: "Green", tracked: false)
    ]

    static let periodDefs = [
        PeriodDef(start: "imsak", color: "Red", from: "imsak", to: "bamdat"), PeriodDef(start: "bamdat", color: "Green", from: "bamdat", to: "kun"),
        PeriodDef(start: "kun", color: "Red", from: "kun", to: "besin"), PeriodDef(start: "ishraq", color: "Orange", from: "kun", to: "besin"),
        PeriodDef(start: "kerahat", color: "Red", from: "kun", to: "besin"), PeriodDef(start: "besin", color: "Green", from: "besin", to: "ekindi"),
        PeriodDef(start: "ekindi", color: "Green", from: "ekindi", to: "aqsham"), PeriodDef(start: "isfirar", color: "Red", from: "ekindi", to: "aqsham"),
        PeriodDef(start: "aqsham", color: "Green", from: "aqsham", to: "quptan"), PeriodDef(start: "ishtibaq", color: "Red", from: "aqsham", to: "quptan"),
        PeriodDef(start: "quptan", color: "Green", from: "quptan", to: "imsak")
    ]

    // XML.php отдаёт времена на весь год: 14 значений в день через табуляцию.
    // Порядок столбцов на сайте: İmsâk, Sabâh, Güneş, İşrak, Kerâhet, Öğle, İkindi (асри аввал), Asr-ı sânî,
    // İsfirâr, Akşam, İştibâk, Yatsı, İşâ-i sânî, Kıble sâati.
    static let xmlColumns = ["imsak", "bamdat", "kun", "ishraq", "kerahat", "besin", "asriauual", "ekindi",
                             "isfirar", "aqsham", "ishtibaq", "quptan", "ishaisani"]

    let dir: String
    var s = NSettings()
    let cal: Calendar = {
        var c = Calendar(identifier: .gregorian)
        c.timeZone = TimeZone.current
        return c
    }()

    var yearDays: [String: [String]]?   // расписание на год: «гггг-м-д» → 14 времён
    var dataYear = 0                    // за какой год скачано расписание
    var serverOldYear = false           // сайт ещё отдаёт прошлый год (1 января) — повторяем раз в час
    var yearXml: String?
    var cityNameEn = "", stateEn = "", hijriFor = ""
    var xmlCountryId = 0, hijriOffset = 0
    var fetching = false, fetchError = false
    var fetchToken = 0
    var lastAttempt = Date.distantPast
    var notified = Set<String>()

    @Published var vs = ViewState()
    @Published var popupState = PopupState()

    let popover = NSPopover()
    var hosting: NSHostingController<PopoverRoot>!
    @Published var preview = false      // панель открыта наведением: компактная карточка, без фокуса
    var hiddenAt = Date.distantPast
    var popup: GlassPanel?
    var popupTimer: Timer?
    var statusItem: NSStatusItem!
    var timer: Timer?
    var player: NSSound?
    var dialogDepth = 0                 // пока открыт диалог (город, жамагат) — карточка по наведению не всплывает
    var menuOpen = false
    var dragStart: (mouse: NSPoint, origin: NSPoint)?
    var hoverSince: Date?

    init(dir: String) {
        self.dir = dir
        super.init()
        try? FileManager.default.createDirectory(atPath: dir, withIntermediateDirectories: true)
    }

    var settingsPath: String { dir + "/settings.json" }
    var cachePath: String { dir + "/cache.json" }

    var L: Lang { I18n.find(s.lang) ?? I18n.all[1] }

    func T(_ key: String, _ args: String...) -> String {
        let t = L.text[key] ?? key
        return args.isEmpty ? t : String(format: t, arguments: args.map { $0 as CVarArg })
    }

    func hm(_ d: Date) -> String {
        let c = cal.dateComponents([.hour, .minute], from: d)
        return String(format: "%02d:%02d", c.hour ?? 0, c.minute ?? 0)
    }

    static func parseHM(_ text: String) -> (Int, Int)? {
        guard let m = rxFirst(text, "^(\\d{1,2}):(\\d{2})$"), let h = Int(m[1]), let mm = Int(m[2]) else { return nil }
        return (h, mm)
    }

    // «13:15», «13.15», «1315», «9 05» → «13:15» / «09:05»; пусто или ошибка → nil
    static func normalizeTime(_ text: String?) -> String? {
        guard let m = rxFirst(text ?? "", "^\\s*(\\d{1,2})\\s*[:.,\\s]?\\s*(\\d{2})\\s*$"), let h = Int(m[1]), let mm = Int(m[2]), h < 24, mm < 60 else { return nil }
        return String(format: "%02d:%02d", h, mm)
    }

    func addDays(_ d: Date, _ n: Int) -> Date { cal.date(byAdding: .day, value: n, to: d) ?? d.addingTimeInterval(Double(n) * 86400) }

    func capital(_ text: String) -> String {
        guard let f = text.first else { return text }
        return String(f).uppercased(with: Locale(identifier: L.culture)) + String(text.dropFirst())
    }

    // ---------- запуск ----------
    func start() {
        loadSettings()
        Notify.setup()

        hosting = NSHostingController(rootView: PopoverRoot(n: self))
        if #available(macOS 13.0, *) { hosting.sizingOptions = [.preferredContentSize] }
        popover.contentViewController = hosting
        popover.appearance = NSAppearance(named: .vibrantDark)
        popover.delegate = self
        applyWindowStyle()

        statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.squareLength)
        if let b = statusItem.button {
            b.image = Namaz.crescent()
            b.target = self
            b.action = #selector(statusClicked)
            b.sendAction(on: [.leftMouseUp, .rightMouseUp])
        }

        // нажали в другой программе или на рабочем столе — панель закрывается
        NSEvent.addGlobalMonitorForEvents(matching: [.leftMouseDown, .rightMouseDown, .otherMouseDown]) { [weak self] _ in
            guard let self = self, self.popover.isShown, !self.menuOpen else { return }
            self.hidePopover()
        }
        // перешли в другое пространство или в полноэкранную программу — панель закрывается
        NSWorkspace.shared.notificationCenter.addObserver(forName: NSWorkspace.activeSpaceDidChangeNotification, object: nil, queue: .main) { [weak self] _ in
            guard let self = self, self.popover.isShown else { return }
            self.hidePopover()
        }
        // правый клик по полумесяцу или по панели — меню виджета, системное меню не открывается
        NSEvent.addLocalMonitorForEvents(matching: [.rightMouseDown, .rightMouseUp]) { [weak self] e in
            guard let self = self else { return e }
            let inStatus = e.window != nil && e.window === self.statusItem.button?.window
            let inPanel = self.popover.isShown && e.window === self.popover.contentViewController?.view.window
            if !inStatus && !inPanel { return e }
            if e.type == .rightMouseDown {
                DispatchQueue.main.async { if inStatus { self.showStatusMenu() } else { self.showMainMenu() } }
            }
            return nil
        }
        // нажали в карточке, открытой наведением, — открывается полный вид
        NSEvent.addLocalMonitorForEvents(matching: .leftMouseDown) { [weak self] e in
            if let self = self, self.preview, self.popover.isShown, e.window === self.popover.contentViewController?.view.window {
                DispatchQueue.main.async { self.showFull() }
            }
            return e
        }

        if Autostart.isOn(Namaz.launchLabel) { Autostart.set(Namaz.launchLabel, true) }   // приложение перенесли — обновить путь

        startFetch()
        updateView()
        let t = Timer(timeInterval: 1, repeats: true) { [weak self] _ in self?.tick() }
        RunLoop.main.add(t, forMode: .common)
        timer = t
        let hover = Timer(timeInterval: 0.12, repeats: true) { [weak self] _ in self?.checkHover() }
        RunLoop.main.add(hover, forMode: .common)

        // при запуске — только полумесяц: наведение — карточка, нажатие — полный вид

        if !s.cityChosen {
            // первый запуск: выбрать страну и город — его расписание скачается на год и будет работать без интернета
            DispatchQueue.main.asyncAfter(deadline: .now() + 3) { [weak self] in
                guard let self = self else { return }
                self.s.cityChosen = true   // спрашиваем один раз; сменить город можно в меню
                self.saveSettings()
                self.showCityDialog(firstRun: true)
            }
        }
    }

    func tick() {
        // Расписание: 1 января начинается новый год — скачиваем его; без сети повторяем каждую минуту,
        // если сайт ещё отдаёт прошлый год — раз в час. Дата хиджры сверяется с сайтом раз в день.
        let since = Date().timeIntervalSince(lastAttempt)
        let needYear = !hasToday || yearStale
        let needFetch = (needYear && since >= (serverOldYear && hasToday ? 3600 : 60)) || (hijriFor != todayKey && since >= 600)
        if needFetch && !fetching { startFetch() }
        updateView()
    }

    // ---------- настройки и кэш ----------
    func loadSettings() {
        if let d = J.file(settingsPath) {
            if let v = J.double(d["CityId"]) { s.cityId = Int(v) }
            if let v = J.double(d["CountryId"]) { s.countryId = Int(v) }
            if let v = J.string(d["CityName"]) { s.cityName = v }
            if let v = J.string(d["CityRegion"]) { s.cityRegion = v }
            if let v = J.string(d["CountryName"]) { s.countryName = v }
            if let v = J.double(d["TrayHints"]) { s.trayHints = Int(v) }
            if let v = J.bool(d["CityChosen"]) { s.cityChosen = v }
            if let v = J.string(d["Lang"]) { s.lang = v }
            if let v = J.string(d["Sound"]) { s.sound = v }
            if let v = J.double(d["Transparency"]) { s.transparency = max(0, min(3, Int(v))) }
            if let v = J.bool(d["Notify"]) { s.notify = v }
            if let v = J.bool(d["WinNotify"]) { s.winNotify = v }
            if let v = J.double(d["NotifyBefore"]) { s.notifyBefore = Int(v) }
            if let v = J.double(d["JamaatBefore"]) { s.jamaatBefore = Int(v) }
            if let j = d["Jamaat"] as? JSON {
                for k in Namaz.jamaatKeys {
                    if J.string(j[k]) == Namaz.atAzan { s.jamaat[k] = Namaz.atAzan } else if let t = Namaz.normalizeTime(J.string(j[k])) { s.jamaat[k] = t }
                }
            }
        }
        if I18n.find(s.lang) == nil { s.lang = I18n.defaultCode() }
        if s.notifyBefore != 0 && !Namaz.beforeOptions.contains(s.notifyBefore) { s.notifyBefore = 0 }
        if !["bell", "soft", "system"].contains(s.sound) { s.sound = "bell" }
        if s.jamaatBefore != 0 && !Namaz.jamaatBeforeOptions.contains(s.jamaatBefore) { s.jamaatBefore = 10 }

        // кэш: годовое расписание города — виджет работает без интернета до конца года
        guard let c = J.file(cachePath), J.string(c["CityId"]) == String(s.cityId), let xml = J.string(c["Xml"]) else { return }
        let year = J.double(c["Year"]).map { Int($0) } ?? Namaz.yearFromXml(xml) ?? cal.component(.year, from: Date())
        _ = parseYear(xml, year)
        hijriFor = J.string(c["HijriFor"]) ?? ""
        if let off = J.double(c["HijriOffset"]) { hijriOffset = Int(off) }
    }

    func saveSettings() {
        let d: [String: Any] = [
            "CityId": s.cityId, "CountryId": s.countryId, "CityName": s.cityName, "CityRegion": s.cityRegion,
            "CountryName": s.countryName, "TrayHints": s.trayHints, "CityChosen": s.cityChosen, "Lang": s.lang, "Sound": s.sound,
            "Transparency": s.transparency, "Notify": s.notify,
            "WinNotify": s.winNotify, "NotifyBefore": s.notifyBefore, "JamaatBefore": s.jamaatBefore, "Jamaat": s.jamaat]
        J.write(d, settingsPath)
    }

    func saveCache() {
        J.write(["CityId": s.cityId, "Xml": yearXml ?? "", "Year": dataYear, "HijriFor": hijriFor, "HijriOffset": hijriOffset] as [String: Any], cachePath)
    }

    // ---------- расписание ----------
    func dayKey(_ y: Int, _ m: Int, _ d: Int) -> String { "\(y)-\(m)-\(d)" }

    // год в комментарии файла (<!--2026-->) есть не у всех городов
    static func yearFromXml(_ xml: String?) -> Int? {
        rxFirst(xml ?? "", "<!--\\s*(\\d{4})\\s*-->").flatMap { Int($0[1]) }
    }

    static func attributes(_ text: String) -> [String: String] {
        var d: [String: String] = [:]
        for m in rx(text, "(\\w+)\\s*=\\s*[\"']([^\"']*)[\"']") { d[m[1]] = htmlDecode(m[2]) }
        return d
    }

    // Файл покрывает 31 декабря прошлого года (dayofyear=0), весь год и 1–2 января следующего
    @discardableResult
    func parseYear(_ xml: String?, _ y: Int) -> Bool {
        guard let xml = xml, !xml.isEmpty, let rootTag = rxFirst(xml, "<cityinfo([^>]*)>") else { return false }
        var days: [String: [String]] = [:]
        for m in rx(xml, "<prayertimes([^>]*)>([^<]*)</prayertimes>") {
            let a = Namaz.attributes(m[1])
            guard let day = Int(a["day"] ?? ""), let month = Int(a["month"] ?? ""), (1...12).contains(month), (1...31).contains(day) else { continue }
            let doy = Int(a["dayofyear"] ?? "") ?? 0
            let rowYear = doy == 0 ? y - 1 : (month == 1 && doy > 300 ? y + 1 : y)
            days[dayKey(rowYear, month, day)] = m[2].components(separatedBy: "\t")
        }
        if days.isEmpty { return false }
        yearDays = days
        yearXml = xml
        dataYear = y
        let root = Namaz.attributes(rootTag[1])
        cityNameEn = root["cityNameEN"] ?? ""
        stateEn = root["cityStateEN"] ?? ""
        if let c = Int(root["countryID"] ?? "") { xmlCountryId = c }
        return true
    }

    // Строка расписания на дату. Если расписание нового года ещё не скачано (нет интернета 1 января),
    // берём тот же день из скачанного года: время намаза от года к году меняется на 1–2 минуты.
    func rowFor(_ date: Date) -> (row: [String], approximate: Bool)? {
        guard let days = yearDays else { return nil }
        let c = cal.dateComponents([.year, .month, .day], from: date)
        guard let y = c.year, let m = c.month, let d = c.day else { return nil }
        if let r = days[dayKey(y, m, d)] { return (r, false) }
        guard dataYear > 0 else { return nil }
        let leap = (dataYear % 4 == 0 && dataYear % 100 != 0) || dataYear % 400 == 0
        if let r = days[dayKey(dataYear, m, m == 2 && d == 29 && !leap ? 28 : d)] { return (r, true) }
        return nil
    }

    var hasToday: Bool { rowFor(Date()) != nil }
    var todayApproximate: Bool { rowFor(Date())?.approximate ?? false }
    var yearStale: Bool { dataYear != cal.component(.year, from: Date()) }
    var todayKey: String {
        let c = cal.dateComponents([.year, .month, .day], from: Date())
        return String(format: "%04d-%02d-%02d", c.year ?? 0, c.month ?? 0, c.day ?? 0)
    }

    func events(_ now: Date) -> [Ev] {
        guard let row = rowFor(now)?.row else { return [] }
        let day = cal.startOfDay(for: now)
        var times: [String: String] = [:]
        for (i, k) in Namaz.xmlColumns.enumerated() where i < row.count { times[k] = row[i].trimmingCharacters(in: .whitespacesAndNewlines) }
        var list: [Ev] = []
        for p in Namaz.prayers {
            guard let v = times[p.key], let t = Namaz.parseHM(v) else { continue }
            let time = cal.date(bySettingHour: t.0, minute: t.1, second: 0, of: day) ?? day
            list.append(Ev(key: p.key, name: L.prayers[p.key] ?? p.key, type: p.type, tracked: p.tracked, time: time))
        }
        return list
    }

    static func shownKey(_ key: String) -> String {
        switch key {
        case "ishraq", "kerahat": return "kun"
        case "isfirar": return "ekindi"
        case "ishtibaq": return "aqsham"
        default: return key
        }
    }

    // Текущий период как на namaztimes.kz (граница — за секунду до начала минуты)
    func period(_ now: Date, _ evs: [Ev]) -> Period? {
        var times: [String: Date] = [:]
        for e in evs { times[e.key] = e.time }
        if Namaz.periodDefs.contains(where: { times[$0.start] == nil }) || times["imsak"] == nil { return nil }
        var current: PeriodDef?
        for d in Namaz.periodDefs where now > times[d.start]!.addingTimeInterval(-1) { current = d }
        let from: Date, to: Date
        if let c = current {
            from = times[c.from]!
            to = c.start == "quptan" ? addDays(times["imsak"]!, 1) : times[c.to]!
        } else {
            // после полуночи до имсака — продолжается Иша прошлого дня
            current = Namaz.periodDefs.last
            from = addDays(times["quptan"]!, -1)
            to = times["imsak"]!
        }
        let c = current!
        return Period(key: c.start, name: L.prayers[c.start] ?? c.start, color: c.color, nextName: L.prayers[c.to] ?? c.to,
                      nextKey: c.to, compactKey: Namaz.shownKey(c.start), from: from, to: to)
    }

    // как на сайте: зелёный период после 50% синий; в конце времени намаза — красный
    func barColor(_ p: Period, _ now: Date) -> (String, Int) {
        let total = max(1, p.to.timeIntervalSince(p.from))
        let percent = Int(max(0, min(100, floor(now.timeIntervalSince(p.from) * 100 / total))))
        if p.color != "Green" { return (p.color, percent) }
        if p.to.timeIntervalSince(now) / 60 <= Namaz.endWarningMinutes { return ("Red", percent) }
        return (percent > 50 ? "Blue" : "Green", percent)
    }

    func formatLeft(_ span: TimeInterval) -> String {
        let mins = max(1, Int(ceil(span / 60)))
        return mins >= 60 ? T("HM", "\(mins / 60)", "\(mins % 60)") : T("M", "\(mins)")
    }

    // время жамагата для намаза сегодня (в пятницу вместо Зухра — Джума, если задана)
    func isFriday(_ d: Date) -> Bool { cal.component(.weekday, from: d) == 6 }

    func jamaatTime(_ e: Ev) -> Date? {
        if !e.tracked || e.key == "kun" { return nil }
        var key = e.key
        if key == "besin" && isFriday(e.time) && s.jamaat["juma"] != nil { key = "juma" }
        guard let text = s.jamaat[key] else { return nil }
        if text == Namaz.atAzan { return e.time }   // Джума по азану — время Зухра этого дня
        guard let t = Namaz.parseHM(text) else { return nil }
        return cal.date(bySettingHour: t.0, minute: t.1, second: 0, of: e.time)
    }

    func jamaatName(_ e: Ev) -> String {
        e.key == "besin" && isFriday(e.time) && s.jamaat["juma"] != nil
            ? T("Juma").replacingOccurrences(of: "\\s*\\(.*$", with: "", options: .regularExpression) : e.name
    }

    // город, регион и страна на языке виджета (на сайте — латиница)
    var cityTitle: String { Names.city(s.cityName, Names.countryCode(s.countryName), L.code) }
    var regionTitle: String {
        let code = Names.countryCode(s.countryName)
        return [Names.region(s.cityRegion, code, L.code), s.countryName.isEmpty ? "" : Names.country(s.countryName, L.code)]
            .filter { !$0.isEmpty }.joined(separator: ", ")
    }

    func dateLines(_ now: Date) -> [String] {   // день недели, милади, хиджри
        let f = DateFormatter()
        f.locale = Locale(identifier: L.culture)
        f.dateFormat = "EEEE"
        let weekday = capital(f.string(from: now))
        let c = cal.dateComponents([.year, .month, .day], from: now)
        let mi = (c.month ?? 1) - 1
        let month = L.code == "uz" ? f.monthSymbols[mi] : capital(f.standaloneMonthSymbols[mi])
        let miladi = T("MiladiFormat", "\(c.day ?? 0)", month, "\(c.year ?? 0)")
        var hijri = ""
        let hc = Calendar(identifier: .islamicUmmAlQura)
        let h = hc.dateComponents([.year, .month, .day], from: addDays(now, hijriOffset))
        if let d = h.day, let m = h.month, let y = h.year, (1...12).contains(m) { hijri = "\(d) \(L.months[m - 1]) \(y)" }
        return [weekday, miladi, hijri]
    }

    // ---------- загрузка (namazvakti.com) ----------
    func download(_ url: String, _ done: @escaping (String?) -> Void) {
        guard let u = URL(string: url) else { done(nil); return }
        var req = URLRequest(url: u, timeoutInterval: 30)
        req.setValue(Namaz.userAgent, forHTTPHeaderField: "User-Agent")
        URLSession.shared.dataTask(with: req) { data, resp, err in
            let code = (resp as? HTTPURLResponse)?.statusCode ?? 0
            let ok = err == nil && (200..<300).contains(code)
            let text = ok ? data.flatMap { String(data: $0, encoding: .utf8) ?? String(data: $0, encoding: .windowsCP1254) } : nil
            DispatchQueue.main.async { done(text) }
        }.resume()
    }

    func restartFetch() {
        fetchToken += 1
        fetching = false
        startFetch()
    }

    // 1) годовое расписание (если на сегодня его нет); 2) страница города — область, страна и дата хиджры
    func startFetch() {
        if fetching { return }
        fetching = true
        lastAttempt = Date()
        fetchToken += 1
        let token = fetchToken
        let cityId = s.cityId
        let loadPage: () -> Void = { [weak self] in
            guard let self = self else { return }
            self.download(Namaz.siteRoot + "Main.php?cityID=\(cityId)&WSLanguage=EN") { html in
                guard token == self.fetchToken else { return }
                self.fetching = false
                if let html = html { self.applyCityPage(html) }
                self.updateView()
            }
        }
        if hasToday && !yearStale { loadPage(); return }

        download(Namaz.siteRoot + "XML.php?cityID=\(cityId)") { [weak self] text in
            guard let self = self, token == self.fetchToken else { return }
            guard let text = text, !text.isEmpty else { self.fetchError = true; self.fetching = false; self.updateView(); return }
            // год узнаём из файла, а если его там нет — из заголовка годовой таблицы сайта «YILLIK VAKİTLER (2026)»
            if let y = Namaz.yearFromXml(text) { self.applyYear(text, y, loadPage); return }
            self.download(Namaz.siteRoot + "Yearly.php?cityID=\(cityId)") { html in
                guard token == self.fetchToken else { return }
                let year = rxFirst(html ?? "", "\\((20\\d\\d)\\)").flatMap { Int($0[1]) }
                    ?? (text == self.yearXml ? self.dataYear : self.cal.component(.year, from: Date()))   // тот же файл — значит, тот же год
                self.applyYear(text, year, loadPage)
            }
        }
    }

    func applyYear(_ text: String, _ year: Int, _ loadPage: () -> Void) {
        if !parseYear(text, year) { fetchError = true; fetching = false; updateView(); return }
        // сервер в Турции: в полночь по местному времени он ещё может отдавать прошлый год
        serverOldYear = year < cal.component(.year, from: Date())
        fetchError = false
        if !cityNameEn.isEmpty { s.cityName = cityNameEn }
        if xmlCountryId > 0 { s.countryId = xmlCountryId }
        if s.cityRegion.isEmpty { s.cityRegion = stateEn }
        saveSettings()
        saveCache()
        updateView()
        loadPage()
    }

    // Поле со страницы города: <div id='sehir'>, <div id='eyaletUlke'>, <span id="hicriTarih">
    static func pageField(_ html: String?, _ id: String) -> String? {
        rxFirst(html ?? "", "id=['\"]" + id + "['\"][^>]*>([^<]*)<").map { htmlDecode($0[1]).trimmingCharacters(in: .whitespacesAndNewlines) }
    }

    // «Almaty / Kazakhstan» → «Almaty, Kazakhstan»
    static func placeText(_ place: String?) -> String {
        (place ?? "").split(separator: "/").map { $0.trimmingCharacters(in: .whitespaces) }.filter { !$0.isEmpty }.joined(separator: ", ")
    }

    func applyCityPage(_ html: String) {
        let place = (Namaz.pageField(html, "eyaletUlke") ?? "").split(separator: "/").map { $0.trimmingCharacters(in: .whitespaces) }.filter { !$0.isEmpty }
        if place.count > 1 {
            s.countryName = place[place.count - 1]
            s.cityRegion = place.dropLast().joined(separator: ", ")
        }
        // день хиджры с сайта («8 Rabi’al-âkhir 1448») → сдвиг относительно календаря Умм аль-Кура
        if let m = rxFirst(Namaz.pageField(html, "hicriTarih") ?? "", "^(\\d{1,2})\\s+\\D+?\\s+(\\d{4})$"), let siteDay = Int(m[1]), let siteYear = Int(m[2]) {
            let hc = Calendar(identifier: .islamicUmmAlQura)
            for k in -2...2 {
                let c = hc.dateComponents([.year, .day], from: addDays(Date(), k))
                if c.day == siteDay && c.year == siteYear { hijriOffset = k; hijriFor = todayKey; break }
            }
        }
        saveSettings()
        saveCache()
    }

    // ---------- вид ----------
    func updateView() {
        let now = Date()
        var v = ViewState()
        v.transparency = s.transparency
        v.city = cityTitle
        v.region = regionTitle
        let c = cal.dateComponents([.hour, .minute, .second], from: now)
        v.clock = String(format: "%02d:%02d:%02d", c.hour ?? 0, c.minute ?? 0, c.second ?? 0)
        let dates = dateLines(now)
        v.weekday = dates[0]; v.miladi = dates[1]; v.hijri = dates[2]
        v.miladiLabel = T("MiladiLabel"); v.hijriLabel = T("HijriLabel"); v.timesTitle = T("TimesTitle")
        v.menuTip = T("Menu"); v.jamaatTip = T("JamaatMenu")

        let evs = events(now)
        let p = period(now, evs)
        if let p = p {
            let (bar, percent) = barColor(p, now)
            v.currentName = p.name
            v.currentInk = "Ink" + p.color
            v.leftLabel = T("Until", p.nextName)
            v.leftValue = formatLeft(p.to.timeIntervalSince(now))
            v.nextText = p.nextName + " " + hm(p.to)
            v.hasArc = true
            v.bar = bar
            v.arc = max(0.2, Double(percent)) / 100
            v.percentText = "\(percent)%"
            v.percentInk = "Ink" + bar
            statusItem?.button?.toolTip = T("TrayTip", p.nextName, hm(p.to))
        } else {
            v.leftLabel = T("Loading")
            statusItem?.button?.toolTip = T("TrayDefault")
        }

        // активная строка — цвет периода, остальные серые
        let anyJamaat = !s.jamaat.isEmpty
        v.jamaatHeader = anyJamaat ? T("Jamaat") : "+ " + T("Jamaat")
        v.rows = evs.map { e in
            RowState(id: e.key, name: e.name, time: hm(e.time), jamaat: anyJamaat ? (jamaatTime(e).map { hm($0) } ?? "") : nil,
                     type: e.type, prayer: e.type == "Green", active: p?.key == e.key)
        }

        // ближайший жамагат сегодня — под часами
        if hasToday, let next = evs.compactMap({ e in jamaatTime(e).map { (e, $0) } }).filter({ $0.1 > now }).min(by: { $0.1 < $1.1 }) {
            v.jamaatPill = T("NextJamaat", jamaatName(next.0), hm(next.1))
        }

        // карточка: сколько осталось, текущий и следующий намаз, жамагат
        if let p = p {
            let left = max(0, Int(p.to.timeIntervalSince(now)))
            v.cardCountdown = String(format: "%02d:%02d:%02d", left / 3600, (left % 3600) / 60, left % 60)
            v.cardUntil = T("Until", p.nextName)
            let start = evs.first { $0.key == p.compactKey }
            v.cardCurName = L.prayers[p.compactKey] ?? ""
            v.cardCurTime = hm(start?.time ?? p.from)
            v.cardInk = "Ink" + p.color
            v.cardNextName = p.nextName
            v.cardNextTime = hm(p.to)
            let jamaatOf = { (key: String) -> String in
                guard let e = evs.first(where: { $0.key == key }), let at = self.jamaatTime(e) else { return "" }
                return self.hm(at)
            }
            v.cardCurJamaat = jamaatOf(p.compactKey)
            v.cardNextJamaat = jamaatOf(p.nextKey)
            v.cardJamaatHead = v.cardCurJamaat.isEmpty && v.cardNextJamaat.isEmpty ? "" : T("Jamaat")
        } else {
            v.cardUntil = T("Loading")
        }

        let stale = !hasToday
        if fetchError && stale { v.status = T("OfflineNoData") } else if todayApproximate { v.status = T("OldYear") }
        vs = v
        fitPopover()

        if !stale { sendNotifications(evs.filter { $0.tracked }, now) }
    }

    // ---------- уведомления ----------
    func sendNotifications(_ tracked: [Ev], _ now: Date) {
        let dayFmt = { (d: Date) -> String in
            let c = self.cal.dateComponents([.year, .month, .day], from: d)
            return String(format: "%04d%02d%02d", c.year ?? 0, c.month ?? 0, c.day ?? 0)
        }
        for e in tracked {
            let caption = cityTitle + " · " + hm(e.time)
            let day = dayFmt(e.time)
            // жамагат по азану: одно уведомление о жамагате вместо двух одновременных
            let jamaatAtAzan = jamaatTime(e).map { abs($0.timeIntervalSince(e.time)) < 60 } ?? false
            if s.notifyBefore > 0 && !(jamaatAtAzan && s.notifyBefore == s.jamaatBefore) {
                let at = e.time.addingTimeInterval(Double(-s.notifyBefore * 60))
                let id = "pre\(s.notifyBefore)-\(e.key)-\(day)"
                if now >= at && now < e.time && now < at.addingTimeInterval(120) && notified.insert(id).inserted {
                    showNotification(e.name, T("NotifyBeforeMsg", "\(s.notifyBefore)"), caption, e.type)
                }
            }
            if s.notify && e.key != "kun" && !jamaatAtAzan {
                let id = "at-\(e.key)-\(day)"
                if now >= e.time && now < e.time.addingTimeInterval(120) && notified.insert(id).inserted {
                    showNotification(e.name, T("NotifyAtMsg"), caption, e.type)
                }
            }
            guard let jamaat = jamaatTime(e) else { continue }
            let jt = hm(jamaat)
            let jCaption = cityTitle + " · " + T("Jamaat") + " " + jt
            if s.jamaatBefore > 0 {
                let at = jamaat.addingTimeInterval(Double(-s.jamaatBefore * 60))
                let id = "jpre\(s.jamaatBefore)-\(e.key)-\(jt)-\(day)"
                if now >= at && now < jamaat && now < at.addingTimeInterval(120) && notified.insert(id).inserted {
                    showNotification(jamaatName(e), T("JamaatBeforeMsg", "\(s.jamaatBefore)"), jCaption, "Blue")
                }
            }
            let jid = "jat-\(e.key)-\(jt)-\(day)"
            if now >= jamaat && now < jamaat.addingTimeInterval(120) && notified.insert(jid).inserted {
                showNotification(jamaatName(e), T("JamaatMsg"), jCaption, "Blue")
            }
        }
    }

    func closePopup() {
        popupTimer?.invalidate()
        popupTimer = nil
        popup?.orderOut(nil)
    }

    // окно-уведомление в центре экрана (то же стекло, что у виджета) + звук + уведомление macOS
    func showNotification(_ title: String, _ message: String, _ caption: String, _ accent: String) {
        closePopup()
        popupState = PopupState(caption: caption, title: title, message: message, close: T("Close"), accent: accent)
        if popup == nil {
            popup = GlassPanel(PopupView(n: self), keyable: false)
            popup?.level = .statusBar
        }
        let fullScreen = FullScreen.isActive()   // поверх полноэкранной программы окно не показываем
        if let w = popup, !fullScreen {
            w.setBlur(s.transparency < 3)
            DispatchQueue.main.async {
                w.setContentSize(w.contentView?.subviews.last?.fittingSize ?? w.frame.size)
                w.center()
                w.orderFrontRegardless()
            }
        }
        let t = Timer(timeInterval: 120, repeats: false) { [weak self] _ in self?.closePopup() }
        RunLoop.main.add(t, forMode: .common)
        popupTimer = t
        playSound()
        if s.winNotify || fullScreen { Notify.post(title + " — " + message, caption, sound: false) }
    }

    func testNotification() {
        showNotification(L.prayers["besin"] ?? "", T("NotifyBeforeMsg", "\(max(3, s.notifyBefore))"), cityTitle + " · 12:00", "Green")
    }

    // ---------- звук ----------
    func playSound() {
        if s.sound == "system" { NSSound(named: "Glass")?.play(); return }
        player?.stop()
        player = NSSound(data: s.sound == "soft" ? Namaz.softWav : Namaz.chimeWav)
        if player?.play() != true { NSSound(named: "Glass")?.play() }
    }

    // Колокольчики: мажорное арпеджио C6–E6–G6–C7 с мягким затуханием
    static let chimeWav = wav(2.6) { (t: Double) -> Double in
        let a = bell(t, 0.00, 1046.50, 0.55) + bell(t, 0.16, 1318.51, 0.55)
        return a + bell(t, 0.32, 1567.98, 0.65) + 0.8 * bell(t, 0.52, 2093.00, 0.9)
    }
    // Мягкий звон «динь-дон»: G5 → E5
    static let softWav = wav(3.0) { (t: Double) -> Double in bell(t, 0.0, 783.99, 0.9) + bell(t, 0.55, 659.25, 1.1) }

    static func bell(_ t: Double, _ start: Double, _ freq: Double, _ decay: Double) -> Double {
        let x = t - start
        if x < 0 { return 0 }
        let envelope: Double = min(1, x / 0.004) * exp(-x / decay)
        let w: Double = 2 * Double.pi * freq * x
        let h1: Double = sin(w)
        let h2: Double = 0.30 * sin(w * 2.0) * exp(-x / (decay * 0.5))
        let h3: Double = 0.10 * sin(w * 3.01) * exp(-x / (decay * 0.3))
        return envelope * (h1 + h2 + h3)
    }

    static func wav(_ seconds: Double, _ wave: (Double) -> Double) -> Data {
        let rate = 44100
        let n = Int(Double(rate) * seconds)
        var samples = [Double](repeating: 0, count: n)
        var peak = 1e-9
        for i in 0..<n {
            let t = Double(i) / Double(rate)
            samples[i] = wave(t) * min(1, (seconds - t) / 0.08)
            peak = max(peak, abs(samples[i]))
        }
        var d = Data()
        func u32(_ v: Int) { var x = UInt32(v).littleEndian; d.append(Data(bytes: &x, count: 4)) }
        func u16(_ v: Int) { var x = UInt16(v).littleEndian; d.append(Data(bytes: &x, count: 2)) }
        d.append(contentsOf: Array("RIFF".utf8)); u32(36 + n * 2); d.append(contentsOf: Array("WAVE".utf8))
        d.append(contentsOf: Array("fmt ".utf8)); u32(16); u16(1); u16(1); u32(rate); u32(rate * 2); u16(2); u16(16)
        d.append(contentsOf: Array("data".utf8)); u32(n * 2)
        var pcm = samples.map { Int16(max(-32767, min(32767, $0 / peak * 0.7 * 32767))).littleEndian }
        d.append(Data(bytes: &pcm, count: n * 2))
        return d
    }

    // ---------- окна ----------
    func applyWindowStyle() {
        popup?.setBlur(s.transparency < 3)
    }

    // окно-уведомление поменяло размер — остаётся по центру экрана
    func resized(_ w: NSWindow?, _ size: CGSize) {
        DispatchQueue.main.async {
            guard let w = w, size.width > 1, size.height > 1 else { return }
            if abs(w.frame.width - size.width) < 0.5 && abs(w.frame.height - size.height) < 0.5 { return }
            w.setContentSize(size)
            w.center()
        }
    }

    func dragPopup(_ ended: Bool) {
        guard let w = popup else { return }
        let m = NSEvent.mouseLocation
        if dragStart == nil { dragStart = (m, w.frame.origin) }
        if let st = dragStart { w.setFrameOrigin(NSPoint(x: st.origin.x + m.x - st.mouse.x, y: st.origin.y + m.y - st.mouse.y)) }
        if ended { dragStart = nil }
    }

    // Панель со стрелкой под полумесяцем, как в «Лимитах ИИ»: наведение — компактная карточка,
    // нажатие — полный вид; нажали мимо — панель закрывается.
    func showFull() {
        preview = false
        hoverSince = nil
        NSApp.activate(ignoringOtherApps: true)
        present()
        popover.contentViewController?.view.window?.makeKey()
    }

    func present() {
        popover.behavior = preview ? .applicationDefined : .transient
        updateView()
        if !popover.isShown, let b = statusItem.button {
            if #available(macOS 13.0, *) {} else { popover.contentSize = hosting.view.fittingSize }
            popover.show(relativeTo: b.bounds, of: b, preferredEdge: .minY)
        }
    }

    func hidePopover() {
        preview = false
        popover.performClose(nil)
    }

    // до macOS 13 размер панели по содержимому выставляем сами
    func fitPopover() {
        if #available(macOS 13.0, *) { return }
        DispatchQueue.main.async {
            guard self.popover.isShown else { return }
            let size = self.hosting.view.fittingSize
            if size.width > 1 && size != self.popover.contentSize { self.popover.contentSize = size }
        }
    }

    func popoverDidClose(_ notification: Notification) {
        hiddenAt = Date()
        preview = false
    }

    // Наведение на полумесяц — карточка; увели курсор с полумесяца и карточки — она закрывается.
    func checkHover() {
        guard let b = statusItem?.button, let bw = b.window else { hoverSince = nil; return }
        let pt = NSEvent.mouseLocation
        let overIcon = bw.frame.insetBy(dx: -2, dy: -4).contains(pt)
        if preview {
            let overPanel = popover.isShown && (popover.contentViewController?.view.window?.frame.insetBy(dx: -2, dy: -8).contains(pt) ?? false)
            if !overIcon && !overPanel { hidePopover() }
            return
        }
        if !overIcon || popover.isShown || menuOpen || dialogDepth > 0 { hoverSince = nil; return }
        if hoverSince == nil { hoverSince = Date() }
        if Date().timeIntervalSince(hoverSince!) < 0.25 { return }
        hoverSince = nil
        // полноэкранная программа — строка меню выезжает от курсора у края, карточку не показываем
        if FullScreen.isActive() { return }
        preview = true
        present()
    }

    func setTransparency(_ level: Int) {
        s.transparency = level
        saveSettings()
        applyWindowStyle()
        updateView()
    }

    func setLanguage(_ code: String) {
        if I18n.find(code) == nil || code == s.lang { return }
        s.lang = code
        saveSettings()
        updateView()
    }

    // ---------- строка меню и меню ----------
    // Полумесяц со звездой одним цветом (шаблон): macOS сама красит его белым или чёрным, как значки других программ.
    static func crescent() -> NSImage {
        let image = NSImage(size: NSSize(width: 18, height: 18), flipped: true) { _ in
            // полумесяц: круг, из которого вырезан смещённый вправо круг
            NSColor.black.setFill()
            NSBezierPath(ovalIn: NSRect(x: 0.5, y: 1.5, width: 15, height: 15)).fill()
            NSGraphicsContext.current?.compositingOperation = .clear
            NSBezierPath(ovalIn: NSRect(x: 4.2, y: 0.6, width: 14, height: 14)).fill()
            NSGraphicsContext.current?.compositingOperation = .sourceOver
            // пятиконечная звезда справа внутри полумесяца
            let star = NSBezierPath()
            let c = NSPoint(x: 12.6, y: 7.8), outer: CGFloat = 3.2, inner: CGFloat = 1.3
            for i in 0..<10 {
                let r = i % 2 == 0 ? outer : inner
                let a = -CGFloat.pi / 2 + CGFloat(i) * .pi / 5
                let pt = NSPoint(x: c.x + r * cos(a), y: c.y + r * sin(a))
                if i == 0 { star.move(to: pt) } else { star.line(to: pt) }
            }
            star.close()
            star.fill()
            return true
        }
        image.isTemplate = true
        return image
    }

    @objc func statusClicked() {
        let e = NSApp.currentEvent
        if e?.type == .rightMouseUp || e?.modifierFlags.contains(.control) == true { showStatusMenu(); return }
        if popover.isShown {
            if preview { showFull() } else { hidePopover() }
        }
        // нажатие по значку сначала закрывает панель (нажали мимо неё), затем приходит само нажатие —
        // не открываем панель заново сразу после такого закрытия
        else if Date().timeIntervalSince(hiddenAt) > 0.3 { showFull() }
    }

    func showStatusMenu() {
        if preview { hidePopover() }
        menuOpen = true
        if let b = statusItem.button { buildStatusMenu().popUp(positioning: nil, at: NSPoint(x: 0, y: b.bounds.height + 5), in: b) }
        menuOpen = false
    }

    func showMainMenu() {
        menuOpen = true
        buildMainMenu().popUp(positioning: nil, at: NSEvent.mouseLocation, in: nil)
        menuOpen = false
    }

    func languageMenu() -> NSMenuItem {
        submenu(T("Language"), I18n.all.map { lang in
            ClosureItem(lang.label, checked: lang.code == s.lang) { [unowned self] in self.setLanguage(lang.code) }
        })
    }

    func buildMainMenu() -> NSMenu {
        let menu = NSMenu()
        menu.autoenablesItems = false
        menu.addItem(ClosureItem(T("ChangeCity")) { [unowned self] in self.showCityDialog(firstRun: false) })
        menu.addItem(ClosureItem(T("JamaatMenu")) { [unowned self] in self.showJamaatDialog() })
        menu.addItem(languageMenu())

        let levels = ["TransLow", "TransMid", "TransHigh", "TransMax"]
        menu.addItem(submenu(T("Transparency"), levels.indices.map { i in
            ClosureItem(T(levels[i]), checked: s.transparency == i) { [unowned self] in self.setTransparency(i) }
        }))

        var notify: [NSMenuItem] = [
            ClosureItem(T("NotifyAtTime"), checked: s.notify) { [unowned self] in self.s.notify.toggle(); self.saveSettings() },
            ClosureItem(T("WinNotify"), checked: s.winNotify) { [unowned self] in self.s.winNotify.toggle(); self.saveSettings() },
            .separator(),
            ClosureItem(T("NoBefore"), checked: s.notifyBefore == 0) { [unowned self] in self.s.notifyBefore = 0; self.saveSettings() }
        ]
        for m in Namaz.beforeOptions {
            notify.append(ClosureItem(T("Before", "\(m)"), checked: s.notifyBefore == m) { [unowned self] in self.s.notifyBefore = m; self.saveSettings() })
        }
        notify.append(.separator())
        let sounds = [("bell", T("SoundBell")), ("soft", T("SoundSoft")), ("system", T("SoundSystem"))]
        notify.append(submenu(T("SoundMenu"), sounds.map { snd in
            ClosureItem(snd.1, checked: s.sound == snd.0) { [unowned self] in self.s.sound = snd.0; self.saveSettings(); self.playSound() }
        }))
        notify.append(.separator())
        notify.append(ClosureItem(T("TestNotify")) { [unowned self] in self.testNotification() })
        menu.addItem(submenu(T("Notifications"), notify))
        menu.addItem(.separator())

        menu.addItem(ClosureItem(T("Autostart"), checked: Autostart.isOn(Namaz.launchLabel)) {
            Autostart.set(Namaz.launchLabel, !Autostart.isOn(Namaz.launchLabel))
        })
        menu.addItem(.separator())
        menu.addItem(ClosureItem(T("Refresh")) { [unowned self] in self.restartFetch() })
        menu.addItem(ClosureItem(T("OpenSite")) { [unowned self] in self.openSite() })
        menu.addItem(ClosureItem(T("Exit")) { NSApp.terminate(nil) })
        return menu
    }

    func buildStatusMenu() -> NSMenu {
        let menu = NSMenu()
        menu.autoenablesItems = false
        menu.addItem(ClosureItem(T("TrayToggle")) { [unowned self] in
            if self.popover.isShown && !self.preview { self.hidePopover() } else { self.showFull() }
        })
        menu.addItem(ClosureItem(T("ChangeCity")) { [unowned self] in self.showCityDialog(firstRun: false) })
        menu.addItem(ClosureItem(T("JamaatMenu")) { [unowned self] in self.showJamaatDialog() })
        menu.addItem(languageMenu())
        menu.addItem(ClosureItem(T("TestNotify")) { [unowned self] in self.testNotification() })
        menu.addItem(ClosureItem(T("OpenSite")) { [unowned self] in self.openSite() })
        menu.addItem(.separator())
        menu.addItem(ClosureItem(T("Exit")) { NSApp.terminate(nil) })
        return menu
    }

    func openSite() { Shell.open(Namaz.siteRoot + "Main.php?cityID=\(s.cityId)") }

    // ---------- диалоги ----------
    // обычное окно с заголовком поверх остальных
    @discardableResult
    func openDialog<V: View>(_ title: String, _ view: V, _ size: NSSize, resizable: Bool) -> NSWindow {
        let w = NSWindow(contentRect: NSRect(origin: .zero, size: size),
                         styleMask: resizable ? [.titled, .closable, .resizable] : [.titled, .closable], backing: .buffered, defer: false)
        w.title = title
        w.isReleasedWhenClosed = false
        w.level = .floating
        let host = NSHostingView(rootView: view)
        w.contentView = host
        if resizable { w.contentMinSize = NSSize(width: 400, height: 420) } else { w.setContentSize(host.fittingSize) }
        w.center()
        dialogDepth += 1
        var token: NSObjectProtocol?
        token = NotificationCenter.default.addObserver(forName: NSWindow.willCloseNotification, object: w, queue: .main) { [weak self] _ in
            if let t = token { NotificationCenter.default.removeObserver(t) }
            guard let self = self else { return }
            self.dialogDepth -= 1
        }
        NSApp.activate(ignoringOtherApps: true)
        w.makeKeyAndOrderFront(nil)
        return w
    }

    func showCityDialog(firstRun: Bool) {
        let model = CityModel(n: self)
        let w = openDialog(firstRun ? T("FirstRunTitle") : T("DlgTitle"), CityDialog(m: model), NSSize(width: 480, height: 560), resizable: true)
        model.close = { [weak w] in w?.close() }
        model.onChoose = { [weak self] c in self?.applyCity(c) }
        model.start()
    }

    func applyCity(_ c: CityChoice) {
        s.cityId = c.id
        s.cityName = c.name
        s.cityRegion = c.region
        s.countryName = c.country ?? ""   // при поиске страну уточнит страница города
        saveSettings()
        yearDays = nil
        yearXml = nil
        dataYear = 0
        serverOldYear = false
        hijriFor = ""
        hijriOffset = 0
        fetchError = false
        restartFetch()
        updateView()
    }

    func showJamaatDialog() {
        var azan: [String: String] = [:]
        for e in events(Date()) { azan[e.key] = hm(e.time) }
        let model = JamaatModel(n: self, values: s.jamaat, before: s.jamaatBefore, azan: azan)
        let w = openDialog(T("JamaatTitle"), JamaatDialog(m: model), NSSize(width: 520, height: 380), resizable: false)
        model.close = { [weak w] in w?.close() }
        model.onSave = { [weak self] values, before in
            guard let self = self else { return }
            self.s.jamaat = values
            self.s.jamaatBefore = before
            self.saveSettings()
            self.updateView()
        }
    }
}

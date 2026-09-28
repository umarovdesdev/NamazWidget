// Названия стран, регионов и городов на языке виджета.
// Сайт отдаёт их только латиницей: страны переводим по справочнику macOS (Locale, данные CLDR),
// регионы и города стран СНГ — транслитерацией в кириллицу (на сайте это латинская запись русских/местных названий).

import Foundation

enum Names {
    private static var codeByName: [String: String]?

    static func key(_ name: String) -> String {
        var s = name.folding(options: [.diacriticInsensitive, .caseInsensitive], locale: Locale(identifier: "en_US_POSIX")).lowercased()
        s = s.replacingOccurrences(of: "&", with: " and ").replacingOccurrences(of: "saint", with: "st").replacingOccurrences(of: "st.", with: "st")
        s = s.replacingOccurrences(of: "\\(.*?\\)|,.*$", with: "", options: .regularExpression)
        return s.replacingOccurrences(of: "[^a-z]", with: "", options: .regularExpression)
    }

    // названия стран на сайте, которые не совпадают со справочником
    private static let aliases: [String: String] = [
        "bosniaherzegovina": "BA", "burma": "MM", "canaryislands": "IC", "capeverde": "CV",
        "czechrepublic": "CZ", "macau": "MO", "macedonia": "MK", "palestine": "PS", "rusfed": "RU",
        "stvincentandthegrenadin": "VC", "southgeorgia": "GS", "swaziland": "SZ", "turkey": "TR",
        "virginislands": "VG"
    ]

    static func countryCode(_ englishName: String?) -> String? {
        guard let englishName = englishName, !englishName.isEmpty else { return nil }
        let k = key(englishName)
        if englishName.contains("To US") && k == "virginislands" { return "VI" }
        if let c = aliases[k] { return c }
        if codeByName == nil {
            var map: [String: String] = [:]
            let en = Locale(identifier: "en_US")
            for code in Locale.isoRegionCodes {
                if let name = en.localizedString(forRegionCode: code), name != code, map[key(name)] == nil { map[key(name)] = code }
            }
            codeByName = map
        }
        return codeByName?[k]
    }

    static func localeId(_ lang: String) -> String { lang == "kz" ? "kk" : lang }

    static func country(_ englishName: String, _ lang: String) -> String {
        guard let code = countryCode(englishName) else { return englishName }
        if code == "KG" && lang == "ru" { return "Кыргызстан" }   // в CLDR — «Киргизия»
        let name = Locale(identifier: localeId(lang)).localizedString(forRegionCode: code)
        return name == nil || name == code ? englishName : name!
    }

    private static let cis: Set<String> = ["KZ", "KG", "RU", "TJ", "TM", "UZ", "BY", "UA"]

    private static func useCyrillic(_ countryCode: String?, _ lang: String) -> Bool {
        guard let c = countryCode else { return false }
        return cis.contains(c) && (lang == "ru" || lang == "kz" || lang == "ky")
    }

    // город: «Almaty (Almatı)» → «Алматы», «Bishkek (Frunze)» → «Бишкек (Фрунзе)»
    static func city(_ latin: String, _ countryCode: String?, _ lang: String) -> String {
        if !useCyrillic(countryCode, lang) || latin.isEmpty { return latin }
        guard let re = try? NSRegularExpression(pattern: "^(.*?)\\s*\\((.*)\\)\\s*$"),
              let m = re.firstMatch(in: latin, range: NSRange(latin.startIndex..., in: latin)),
              let r1 = Range(m.range(at: 1), in: latin), let r2 = Range(m.range(at: 2), in: latin) else { return toCyrillic(latin, lang) }
        let main = toCyrillic(String(latin[r1]), lang), alt = toCyrillic(String(latin[r2]), lang)
        return main.caseInsensitiveCompare(alt) == .orderedSame ? main : main + " (" + alt + ")"
    }

    // регион: пояснение в скобках убираем — «Aktyubinsk (Yuzhnyy Ural)» → «Актюбинск»
    static func region(_ latin: String, _ countryCode: String?, _ lang: String) -> String {
        if !useCyrillic(countryCode, lang) || latin.isEmpty { return latin }
        let text = latin.replacingOccurrences(of: "\\s*\\(.*?\\)", with: "", options: .regularExpression).trimmingCharacters(in: .whitespaces)
        return toCyrillic(text.isEmpty ? latin : text, lang)
    }

    // ru, kz, ky
    private static let words: [String: [String]] = [
        "north": ["Северный", "Солтүстік", "Түндүк"], "kuzey": ["Северный", "Солтүстік", "Түндүк"],
        "south": ["Южный", "Оңтүстік", "Түштүк"], "east": ["Восточный", "Шығыс", "Чыгыш"],
        "west": ["Западный", "Батыс", "Батыш"], "region": ["регион", "аймақ", "аймак"],
        "resp": ["Респ", "Респ", "Респ"]
    ]

    private static let vowels = Set("аеёиоуыэюяәөүұіАЕЁИОУЫЭЮЯӘӨҮҰІ")

    static func toCyrillic(_ text: String, _ lang: String) -> String {
        let li = lang == "kz" ? 1 : lang == "ky" ? 2 : 0
        let ru = li == 0, kz = li == 1
        let pairs: [(String, String)] = [
            ("shch", "щ"), ("dzh", "дж"), ("sh", "ш"), ("ch", "ч"), ("zh", "ж"),
            ("kh", "х"), ("tsk", "тск"), ("ts", "ц"), ("gh", kz ? "ғ" : "г"), ("yu", "ю"), ("ya", "я"),
            ("yo", "ё"), ("ye", "е")]
        guard let re = try? NSRegularExpression(pattern: "[\\p{L}'’\\u0092]+|[^\\p{L}'’\\u0092]+") else { return text }
        var output = ""
        for m in re.matches(in: text, range: NSRange(text.startIndex..., in: text)) {
            guard let r = Range(m.range, in: text) else { continue }
            let w = String(text[r])
            var trimmed = w.lowercased()
            while trimmed.hasSuffix(".") { trimmed.removeLast() }
            if let tr = words[trimmed] { output += tr[li]; continue }
            guard let first = w.first, first.isLetter else { output += w; continue }
            let orig = Array(w)
            let lower = orig.map { String($0).lowercased().first ?? $0 }
            var sb = ""
            var i = 0
            while i < lower.count {
                var rep: String?
                var len = 1
                let rest = String(lower[i...])
                let prev: Character = sb.last ?? " "
                for p in pairs where rest.hasPrefix(p.0) { rep = p.1; len = p.0.count; break }
                if rep == nil {
                    switch lower[i] {
                    case "a", "â", "á": rep = "а"
                    case "ä": rep = kz ? "ә" : "а"
                    case "b": rep = "б"
                    case "c": rep = "ц"
                    case "ç": rep = "ч"
                    case "d": rep = "д"
                    case "e", "é": rep = "е"
                    case "f": rep = "ф"
                    case "g", "ğ": rep = "г"
                    case "h": rep = "х"
                    case "i", "í", "î": rep = "и"
                    case "ı": rep = "ы"
                    case "j": rep = ru ? "дж" : "ж"
                    case "k": rep = "к"
                    case "l": rep = "л"
                    case "m": rep = "м"
                    case "n": rep = "н"
                    case "o", "ô": rep = "о"
                    case "ö": rep = ru ? "о" : "ө"
                    case "p": rep = "п"
                    case "q": rep = kz ? "қ" : "к"
                    case "r": rep = "р"
                    case "s": rep = "с"
                    case "ş": rep = "ш"
                    case "t": rep = "т"
                    case "u", "û": rep = "у"
                    case "ü": rep = ru ? "у" : "ү"
                    case "v", "w": rep = "в"
                    case "x": rep = "кс"
                    case "z": rep = "з"
                    // после гласной — «й» (Altayskiy → Алтайский), иначе — «ы» (Kyzyl → Кызыл)
                    case "y": rep = vowels.contains(prev) ? "й" : "ы"
                    // мягкий знак: Arkhangel'sk → Архангельск, Oblast' → Область
                    case "'", "’", "\u{0092}": rep = !sb.isEmpty && !vowels.contains(prev) ? "ь" : ""
                    default: rep = String(lower[i])
                    }
                }
                var piece = rep ?? ""
                if !piece.isEmpty && i < orig.count && orig[i].isUppercase { piece = piece.prefix(1).uppercased() + String(piece.dropFirst()) }
                sb += piece
                i += len
            }
            output += sb
        }
        return output
    }

    // Сайт ищет только латиницей — переводим кириллицу (рус., каз., кырг.) в латиницу
    static func toLatin(_ text: String) -> String {
        let map: [Character: String] = [
            "а": "a", "ә": "a", "б": "b", "в": "v", "г": "g", "ғ": "g", "д": "d", "е": "e", "ё": "yo", "ж": "zh", "з": "z",
            "и": "i", "і": "i", "й": "y", "к": "k", "қ": "k", "л": "l", "м": "m", "н": "n", "ң": "n", "о": "o", "ө": "o",
            "п": "p", "р": "r", "с": "s", "т": "t", "у": "u", "ұ": "u", "ү": "u", "ў": "u", "ф": "f", "х": "kh", "һ": "h",
            "ц": "ts", "ч": "ch", "ш": "sh", "щ": "shch", "ъ": "", "ы": "y", "ь": "", "э": "e", "ю": "yu", "я": "ya"]
        var out = ""
        for ch in text {
            let lower = String(ch).lowercased().first ?? ch
            if let s = map[lower] {
                out += ch.isUppercase && !s.isEmpty ? s.prefix(1).uppercased() + String(s.dropFirst()) : s
            } else { out.append(ch) }
        }
        return out
    }
}

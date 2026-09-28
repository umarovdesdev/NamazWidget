// Диалоги: выбор города (поиск или список страна → регион → город) и время жамагата.

import AppKit
import SwiftUI

struct CityChoice: Identifiable {
    let id: Int
    let name, region: String
    var country: String?
    var display: String?
}

// ссылки вида «› Название» со страниц-списков сайта
func siteLinks(_ html: String?, _ hrefPattern: String) -> [(href: String, text: String)] {
    var list: [(href: String, text: String)] = []
    for m in rx(html ?? "", "<a[^>]+href=[\"']([^\"']*" + hrefPattern + "[^\"']*)[\"'][^>]*>(.*?)</a>", dotAll: true) {
        var text = htmlDecode(m[2].replacingOccurrences(of: "<[^>]+>", with: "", options: .regularExpression))
        if !text.contains("›") { continue }   // избранные города в шапке сайта — без «›»
        text = text.replacingOccurrences(of: "›", with: "").replacingOccurrences(of: "\\s+", with: " ", options: .regularExpression)
            .trimmingCharacters(in: .whitespaces)
        list.append((htmlDecode(m[1]), text))
    }
    return list
}

final class CityModel: ObservableObject {
    struct Country { let id: Int; let english: String; let name: String }
    struct Region { let countryId: Int; let state: String; let english: String; let name: String }

    static var countryCache: [(Int, String)]?

    unowned let n: Namaz
    var close: () -> Void = {}
    var onChoose: (CityChoice) -> Void = { _ in }
    private var chosen = false
    // отменяют устаревшие ответы, если пользователь успел выбрать другое (у списка регионов — свой счётчик)
    private var listToken = 0, regionToken = 0

    @Published var query = ""
    @Published var info = ""
    @Published var countries: [Country] = []
    @Published var regions: [Region] = []
    @Published var results: [CityChoice] = []
    @Published var selected: Int?
    @Published var countryIndex = -1 { didSet { if countryIndex != oldValue { loadRegions() } } }
    @Published var regionIndex = -1 { didSet { if regionIndex != oldValue { loadCities() } } }

    init(n: Namaz) { self.n = n }

    func T(_ key: String, _ args: String...) -> String {
        let t = n.L.text[key] ?? key
        return args.isEmpty ? t : String(format: t, arguments: args.map { $0 as CVarArg })
    }

    func start() { loadCountries() }

    private func show(_ list: [CityChoice], withRegion: Bool) {
        results = list.map { c in
            var c = c
            if withRegion && !c.region.isEmpty { c.display = c.name + "  —  " + c.region } else if c.display == nil { c.display = c.name }
            return c
        }
        info = results.isEmpty ? T("NotFound") : T("Found", "\(results.count)")
        selected = results.isEmpty ? nil : 0
    }

    func search() {
        let q = Names.toLatin(query.trimmingCharacters(in: .whitespaces))
        if q.count < 2 { info = T("Min2"); return }
        listToken += 1
        let token = listToken
        results = []
        info = T("Searching")
        let encoded = q.addingPercentEncoding(withAllowedCharacters: .alphanumerics) ?? q
        n.download(Namaz.siteRoot + "CitySearch.php?WSLanguage=EN&SearchText=" + encoded) { [weak self] html in
            guard let self = self, token == self.listToken else { return }
            guard let html = html else { self.info = self.T("NetError"); return }
            var found: [CityChoice] = []
            for link in siteLinks(html, "cityID=\\d+") {
                guard let id = rxFirst(link.href, "cityID=(\\d+)").flatMap({ Int($0[1]) }) else { continue }
                let parts = link.text.components(separatedBy: " / ")
                found.append(CityChoice(id: id, name: parts[0].trimmingCharacters(in: .whitespaces),
                                        region: parts.count > 1 ? parts[1...].joined(separator: " / ").trimmingCharacters(in: .whitespaces) : ""))
            }
            // единственный результат — сайт сразу открывает страницу города
            if found.isEmpty, let city = Namaz.pageField(html, "sehir"), let id = rxFirst(html, "\\?cityID=(\\d+)&WSLanguage").flatMap({ Int($0[1]) }) {
                found.append(CityChoice(id: id, name: city, region: Namaz.placeText(Namaz.pageField(html, "eyaletUlke"))))
            }
            // сайт ищет подстроку («ош» находит Moshi, Goshen…) — точные совпадения и начало названия ставим первыми
            let ql = q.lowercased()
            let rank = { (c: CityChoice) -> Int in
                let name = c.name.lowercased().replacingOccurrences(of: "\\s*\\(.*$", with: "", options: .regularExpression)
                if name == ql { return 0 }
                if name.hasPrefix(ql) { return 1 }
                let escaped = NSRegularExpression.escapedPattern(for: ql)
                if c.name.lowercased().range(of: "(^|[\\s(\\-])" + escaped, options: .regularExpression) != nil { return 2 }
                return 3
            }
            let ordered = found.enumerated().sorted { (rank($0.element), $0.offset) < (rank($1.element), $1.offset) }.map { $0.element }
            self.show(ordered, withRegion: true)
        }
    }

    func loadCountries() {
        let fill = { [weak self] (list: [(Int, String)]) in
            guard let self = self else { return }
            let lang = self.n.L.code
            let loc = Locale(identifier: self.n.L.culture)
            let items = list.map { Country(id: $0.0, english: $0.1, name: Names.country($0.1, lang)) }
            // без перевода — в конец
            self.countries = items.sorted { a, b in
                let ua = lang != "en" && a.name == a.english, ub = lang != "en" && b.name == b.english
                if ua != ub { return !ua }
                return a.name.compare(b.name, options: [.caseInsensitive], range: nil, locale: loc) == .orderedAscending
            }
            if let i = self.countries.firstIndex(where: { $0.id == self.n.s.countryId }) { self.countryIndex = i }
        }
        if let cached = CityModel.countryCache { fill(cached); return }
        info = T("ListLoading")
        n.download(Namaz.siteRoot + "CountryList.php?WSLanguage=EN") { [weak self] html in
            guard let self = self else { return }
            guard let html = html else { self.info = self.T("NetError"); return }
            let list = siteLinks(html, "StateList\\.php\\?countryID=\\d+").compactMap { l -> (Int, String)? in
                guard let id = rxFirst(l.href, "countryID=(\\d+)").flatMap({ Int($0[1]) }) else { return nil }
                return (id, l.text)
            }
            if !list.isEmpty { CityModel.countryCache = list }
            self.info = ""
            fill(list)
        }
    }

    func loadRegions() {
        regions = []
        regionIndex = -1
        guard countries.indices.contains(countryIndex) else { return }
        regionToken += 1
        let token = regionToken
        let country = countries[countryIndex]
        let code = Names.countryCode(country.english)
        info = T("ListLoading")
        n.download(Namaz.siteRoot + "StateList.php?WSLanguage=EN&countryID=\(country.id)") { [weak self] html in
            guard let self = self, token == self.regionToken else { return }
            guard let html = html else { self.info = self.T("NetError"); return }
            self.regions = siteLinks(html, "CityList\\.php\\?").compactMap { l -> Region? in
                guard let state = rxFirst(l.href, "[?&]state=([^&]*)")?[1] else { return nil }
                return Region(countryId: country.id, state: state, english: l.text, name: Names.region(l.text, code, self.n.L.code))
            }
            self.info = ""
            if self.regions.count == 1 { self.regionIndex = 0 }
        }
    }

    func loadCities() {
        guard regions.indices.contains(regionIndex) else { return }
        let region = regions[regionIndex]
        let countryName = countries.indices.contains(countryIndex) ? countries[countryIndex].english : ""
        let code = Names.countryCode(countryName)
        listToken += 1
        let token = listToken
        results = []
        info = T("ListLoading")
        // state на сайте уже закодирован для адреса — передаём как есть
        let state = region.state.removingPercentEncoding ?? region.state
        let encoded = state.addingPercentEncoding(withAllowedCharacters: .alphanumerics) ?? state
        n.download(Namaz.siteRoot + "CityList.php?WSLanguage=EN&countryID=\(region.countryId)&state=" + encoded) { [weak self] html in
            guard let self = self, token == self.listToken else { return }
            guard let html = html else { self.info = self.T("NetError"); return }
            let lang = self.n.L.code
            let list = siteLinks(html, "cityID=\\d+").compactMap { l -> CityChoice? in
                guard let id = rxFirst(l.href, "cityID=(\\d+)").flatMap({ Int($0[1]) }) else { return nil }
                return CityChoice(id: id, name: l.text, region: region.english, country: countryName, display: Names.city(l.text, code, lang))
            }
            self.show(list, withRegion: false)
        }
    }

    func accept() {
        guard let i = selected, results.indices.contains(i), !chosen else { return }   // двойной щелчок и Enter не должны сработать дважды
        chosen = true
        onChoose(results[i])
        close()
    }
}

struct CityDialog: View {
    @ObservedObject var m: CityModel

    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            Text(m.T("DlgPrompt")).padding(.bottom, 6)
            HStack(spacing: 6) {
                TextField("", text: $m.query).textFieldStyle(.roundedBorder).onSubmit { m.search() }
                Button(m.T("Search")) { m.search() }
            }
            Text(m.T("Browse")).padding(.top, 14).padding(.bottom, 6)
            HStack(alignment: .top, spacing: 8) {
                VStack(alignment: .leading, spacing: 2) {
                    Text(m.T("Country")).font(.system(size: 11)).foregroundColor(.secondary)
                    Picker("", selection: $m.countryIndex) {
                        Text("—").tag(-1)
                        ForEach(m.countries.indices, id: \.self) { i in Text(m.countries[i].name).tag(i) }
                    }
                    .labelsHidden()
                }
                VStack(alignment: .leading, spacing: 2) {
                    Text(m.T("Region")).font(.system(size: 11)).foregroundColor(.secondary)
                    Picker("", selection: $m.regionIndex) {
                        Text("—").tag(-1)
                        ForEach(m.regions.indices, id: \.self) { i in Text(m.regions[i].name).tag(i) }
                    }
                    .labelsHidden()
                    .disabled(m.regions.isEmpty)
                }
            }
            List(selection: $m.selected) {
                ForEach(m.results.indices, id: \.self) { i in
                    Text(m.results[i].display ?? m.results[i].name)
                        .frame(maxWidth: .infinity, alignment: .leading)
                        .contentShape(Rectangle())
                        .simultaneousGesture(TapGesture(count: 2).onEnded { m.selected = i; m.accept() })
                }
            }
            .padding(.vertical, 8)
            HStack {
                Text(m.info).foregroundColor(.secondary).lineLimit(1)
                Spacer()
                Button(m.T("Choose")) { m.accept() }.keyboardShortcut(.defaultAction).disabled(m.selected == nil)
                Button(m.T("Cancel")) { m.close() }.keyboardShortcut(.cancelAction)
            }
        }
        .padding(14)
        .frame(minWidth: 400, minHeight: 420)
    }
}

final class JamaatModel: ObservableObject {
    unowned let n: Namaz
    let azan: [String: String]
    var close: () -> Void = {}
    var onSave: ([String: String], Int) -> Void = { _, _ in }
    @Published var values: [String: String]
    @Published var before: Int
    @Published var error: String?

    init(n: Namaz, values: [String: String], before: Int, azan: [String: String]) {
        self.n = n
        self.values = values
        self.before = before
        self.azan = azan
    }

    func save() {
        var result: [String: String] = [:]
        for key in Namaz.jamaatKeys {
            let text = (values[key] ?? "").trimmingCharacters(in: .whitespaces)
            if text.isEmpty { continue }
            if text == Namaz.atAzan { result[key] = text; continue }
            guard let t = Namaz.normalizeTime(text) else { error = n.T("BadTime", text); return }
            result[key] = t
        }
        onSave(result, before)
        close()
    }
}

struct JamaatDialog: View {
    @ObservedObject var m: JamaatModel

    var body: some View {
        let n = m.n
        VStack(alignment: .leading, spacing: 0) {
            Text(n.T("JamaatHint")).foregroundColor(.secondary).fixedSize(horizontal: false, vertical: true).padding(.bottom, 12)
            ForEach(Namaz.jamaatKeys, id: \.self) { key in
                HStack {
                    Text(key == "juma" ? n.T("Juma") : n.L.prayers[key] ?? key)
                    Spacer()
                    let azan = m.azan[key == "juma" ? "besin" : key]
                    let atAzan = m.values[key] == Namaz.atAzan
                    if let a = azan { Text(n.T("Azan", a)).foregroundColor(.secondary).padding(.trailing, 10) }
                    Toggle(n.T("JamaatAtAzan"), isOn: Binding(get: { atAzan }, set: { m.values[key] = $0 ? Namaz.atAzan : "" }))
                        .toggleStyle(.checkbox).help(n.T("JamaatAtAzanTip")).padding(.trailing, 8)
                    TextField("", text: Binding(get: { atAzan ? (azan ?? "") : (m.values[key] ?? "") },
                                                set: { if !atAzan { m.values[key] = $0 } }))
                        .textFieldStyle(.roundedBorder).multilineTextAlignment(.center).frame(width: 80)
                        .disabled(atAzan)
                }
                .padding(.vertical, 3)
            }
            HStack {
                Text(n.T("JamaatBefore"))
                Spacer()
                Picker("", selection: $m.before) {
                    Text(n.T("JamaatNoBefore")).tag(0)
                    ForEach(Namaz.jamaatBeforeOptions, id: \.self) { mm in Text(n.T("JamaatBeforeItem", "\(mm)")).tag(mm) }
                }
                .labelsHidden().frame(minWidth: 180)
            }
            .padding(.top, 12)
            if let e = m.error { Text(e).foregroundColor(Color(hex: "#DC3545")).padding(.top, 10) }
            HStack {
                Spacer()
                Button(n.T("Save")) { m.save() }.keyboardShortcut(.defaultAction)
                Button(n.T("Cancel")) { m.close() }.keyboardShortcut(.cancelAction)
            }
            .padding(.top, 14)
        }
        .padding(16)
        .frame(width: 520)
        .fixedSize(horizontal: false, vertical: true)
    }
}

// Точка входа: приложение без значка в Dock (LSUIElement) — полумесяц в строке меню и панель под ним.

import AppKit

final class AppDelegate: NSObject, NSApplicationDelegate {
    var namaz: Namaz!

    func applicationDidFinishLaunching(_ notification: Notification) {
        let support = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask).first!.path
        namaz = Namaz(dir: support + "/NamazWidget")
        namaz.start()
    }

    // приложение открыли повторно (Finder, Spotlight) — показать виджет
    func applicationShouldHandleReopen(_ sender: NSApplication, hasVisibleWindows flag: Bool) -> Bool {
        namaz.showFull()
        return false
    }
}

let app = NSApplication.shared
let appDelegate = AppDelegate()
app.delegate = appDelegate
app.setActivationPolicy(.accessory)
app.run()

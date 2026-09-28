// Окна виджета: панель со стрелкой (компактная карточка и полный вид) и окно-уведомление в «жидком стекле».

import AppKit
import SwiftUI

private let k: CGFloat = 0.8   // как в Windows-версии: всё содержимое уменьшено до 80%

// Цвета прогресс-кольца и таблицы namaztimes.kz
enum P {
    static let hex: [String: String] = [
        "Green": "#30D158", "Blue": "#0A84FF", "Red": "#FF453A", "Orange": "#FF9F0A",
        "RowGreen": "#3830D158", "RowRed": "#38FF453A", "RowOrange": "#38FF9F0A",
        "TextGreen": "#4AE27A", "TextRed": "#FF7A72", "TextOrange": "#FFBE55",
        "Inactive": "#98989D",
        "InkGreen": "#30D158", "InkBlue": "#409CFF", "InkRed": "#FF6961", "InkOrange": "#FFB340"]
    static func c(_ name: String) -> Color { Color(hex: hex[name] ?? "#FFFFFF") }
    static let jamaat = Color(hex: "#64B5FF")
    static let primary = Color(hex: "#F5F5F7"), body = Color(hex: "#D1D1D6"), secondary = Color(hex: "#AEAEB2")
    static let rim = LinearGradient(colors: [Color.white.opacity(0.55), Color.white.opacity(0.08), Color.white.opacity(0.25)], startPoint: .top, endPoint: .bottom)
}

// NSHostingView, который принимает первое нажатие: карточка не становится активной, но нажатия получает
final class FirstMouseHostingView<Content: View>: NSHostingView<Content> {
    override func acceptsFirstMouse(for event: NSEvent?) -> Bool { true }
}

final class GlassPanel: NSPanel {
    private var keyable = true
    override var canBecomeKey: Bool { keyable }
    override var canBecomeMain: Bool { false }
    let effect = NSVisualEffectView()

    convenience init<V: View>(_ root: V, keyable: Bool) {
        self.init(contentRect: NSRect(x: 0, y: 0, width: 300, height: 200), styleMask: [.borderless, .nonactivatingPanel],
                  backing: .buffered, defer: false)
        self.keyable = keyable
        isOpaque = false
        backgroundColor = .clear
        hasShadow = true
        hidesOnDeactivate = false
        isReleasedWhenClosed = false
        collectionBehavior = [.canJoinAllSpaces]   // без fullScreenAuxiliary: поверх полноэкранных программ не показывается
        appearance = NSAppearance(named: .darkAqua)

        let container = NSView(frame: NSRect(x: 0, y: 0, width: 300, height: 200))
        effect.material = .hudWindow
        effect.blendingMode = .behindWindow
        effect.state = .active
        effect.maskImage = GlassPanel.mask(12)
        effect.frame = container.bounds
        effect.autoresizingMask = [.width, .height]
        let host = FirstMouseHostingView(rootView: root)
        host.frame = container.bounds
        host.autoresizingMask = [.width, .height]
        container.addSubview(effect)
        container.addSubview(host)
        contentView = container
        let size = host.fittingSize
        if size.width > 1 && size.height > 1 { setContentSize(size) }
    }

    // «Максимальная» прозрачность — без размытия: сквозь стекло видно, что под окном
    func setBlur(_ on: Bool) { effect.isHidden = !on }

    static func mask(_ r: CGFloat) -> NSImage {
        let edge = 2 * r + 1
        let image = NSImage(size: NSSize(width: edge, height: edge), flipped: false) { rect in
            NSColor.black.setFill()
            NSBezierPath(roundedRect: rect, xRadius: r, yRadius: r).fill()
            return true
        }
        image.capInsets = NSEdgeInsets(top: r, left: r, bottom: r, right: r)
        image.resizingMode = .stretch
        return image
    }
}

// сообщает размер содержимого — окно подстраивается под него
struct SizeReader: View {
    let onChange: (CGSize) -> Void
    var body: some View {
        GeometryReader { g in
            Color.clear
                .onAppear { onChange(g.size) }
                .onChange(of: g.size) { onChange($0) }
        }
    }
}

// Liquid Glass: тонировка сверху/снизу по уровню прозрачности, блик по верхнему краю, кромка-«линза»
struct GlassBackground: View {
    let level: Int
    var body: some View {
        let levels: [[Double]] = [[0x99, 0xB3], [0x66, 0x80], [0x40, 0x59], [0x40, 0x59]]
        let a = levels[max(0, min(3, level))].map { $0 / 255 }
        ZStack(alignment: .top) {
            LinearGradient(colors: [Color(hex: "#303036").opacity(a[0]), Color(hex: "#1C1C1E").opacity(a[1])], startPoint: .top, endPoint: .bottom)
            LinearGradient(colors: [Color.white.opacity(0.18), Color.white.opacity(0)], startPoint: .top, endPoint: .bottom).frame(height: 72)
        }
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .overlay(
            RoundedRectangle(cornerRadius: 12).strokeBorder(
                LinearGradient(colors: [Color.white.opacity(0.7), Color.white.opacity(0.15), Color.white.opacity(0.05), Color.white.opacity(0.35)],
                               startPoint: .topLeading, endPoint: .bottomTrailing), lineWidth: 1.2))
    }
}

// Кнопка-капсула: при наведении светлеет и чуть увеличивается, при нажатии — сжимается
struct CircleButton<Content: View>: View {
    let help: String
    var ring: Color? = nil
    @ViewBuilder let label: () -> Content
    let action: () -> Void
    @State var hover = false
    @State var pressed = false

    var body: some View {
        label()
            .frame(width: 30 * k, height: 30 * k)
            .background(Circle().fill(Color.white.opacity(pressed ? 0.3 : hover ? 0.2 : 0.1)))
            .overlay(Group {
                if let ring = ring { Circle().stroke(ring, lineWidth: 1) } else { Circle().stroke(P.rim, lineWidth: 1) }
            })
            .scaleEffect(pressed ? 0.9 : hover ? 1.08 : 1)
            .animation(.spring(response: 0.25, dampingFraction: 0.6), value: hover)
            .animation(.easeOut(duration: 0.09), value: pressed)
            .frame(width: 32 * k, height: 32 * k)
            .contentShape(Rectangle())
            .onHover { hover = $0 }
            .gesture(DragGesture(minimumDistance: 0)
                .onChanged { _ in pressed = true }
                .onEnded { _ in pressed = false; action() })
            .help(help)
    }
}

// ---------- панель со стрелкой: карточка по наведению, полный вид по нажатию ----------
struct PopoverRoot: View {
    @ObservedObject var n: Namaz
    var body: some View {
        Group {
            if n.preview { CardView(n: n) } else { FullView(n: n) }
        }
        .environment(\.colorScheme, .dark)
    }
}

// ---------- полный вид ----------
struct FullView: View {
    @ObservedObject var n: Namaz
    @State var jamaatHover = false

    var body: some View {
        let v = n.vs
        VStack(spacing: 0) {
            header(v).padding(EdgeInsets(top: 14 * k, leading: 18 * k, bottom: 4 * k, trailing: 14 * k))
            HStack(alignment: .top, spacing: 0) {
                left(v).frame(width: 292 * k)
                times(v).frame(minWidth: 250 * k).padding(.leading, 16 * k).padding(.trailing, 2 * k)
            }
            .padding(EdgeInsets(top: 4 * k, leading: 16 * k, bottom: 16 * k, trailing: 16 * k))
        }
        .fixedSize()
    }

    func header(_ v: ViewState) -> some View {
        HStack(spacing: 0) {
            Image(systemName: "mappin.and.ellipse").font(.system(size: 17 * k, weight: .semibold)).foregroundColor(P.c("Green"))
            VStack(alignment: .leading, spacing: 1) {
                Text(v.city).font(.system(size: 17 * k, weight: .semibold)).foregroundColor(P.primary).lineLimit(1)
                if !v.region.isEmpty { Text(v.region).font(.system(size: 12.5 * k)).foregroundColor(P.secondary).lineLimit(1) }
            }
            .padding(.leading, 10 * k).padding(.trailing, 6 * k)
            Spacer(minLength: 0)
            CircleButton(help: v.menuTip) {
                Image(systemName: "ellipsis").font(.system(size: 13 * k, weight: .bold)).foregroundColor(P.body)
            } action: { n.showMainMenu() }
        }
    }

    func left(_ v: ViewState) -> some View {
        VStack(spacing: 0) {
            ZStack {
                ArcRing(fraction: 1, color: Color.white.opacity(0.18))
                if v.hasArc {
                    ArcRing(fraction: v.arc, color: P.c(v.bar)).shadow(color: P.c(v.bar).opacity(0.75), radius: 8)
                }
                VStack(spacing: 0) {
                    Text(v.currentName).font(.system(size: 28 * k, weight: .semibold)).foregroundColor(P.c(v.currentInk))
                        .lineLimit(1).minimumScaleFactor(0.5).frame(maxWidth: 196 * k)
                    Text(v.leftLabel).font(.system(size: 13 * k)).foregroundColor(P.secondary).multilineTextAlignment(.center)
                        .frame(maxWidth: 186 * k).fixedSize(horizontal: false, vertical: true).padding(.top, 6 * k)
                    Text(v.leftValue).font(.system(size: 22 * k, weight: .semibold)).foregroundColor(P.primary)
                        .lineLimit(1).minimumScaleFactor(0.5).frame(maxWidth: 196 * k)
                    Text(v.nextText).font(.system(size: 14 * k)).foregroundColor(P.secondary)
                        .lineLimit(1).minimumScaleFactor(0.6).frame(maxWidth: 186 * k).padding(.top, 3 * k)
                }
                .padding(.bottom, 6 * k)
            }
            .frame(width: 272 * k, height: 228 * k)
            Text(v.percentText).font(.system(size: 13 * k, weight: .semibold)).foregroundColor(P.c(v.percentInk)).padding(.top, -18 * k)
            Text(v.weekday).font(.system(size: 14 * k, weight: .semibold)).foregroundColor(P.body).padding(.top, 8 * k)
            Text(v.clock).font(.system(size: 46 * k, weight: .light).monospacedDigit()).foregroundColor(P.primary).padding(.top, -4 * k)
            if let j = v.jamaatPill {
                Text(j).font(.system(size: 12.5 * k, weight: .semibold)).foregroundColor(P.jamaat)
                    .padding(EdgeInsets(top: 3 * k, leading: 12 * k, bottom: 4 * k, trailing: 14 * k))
                    .background(Capsule().fill(Color(hex: "#380A84FF")))
                    .overlay(Capsule().stroke(P.rim, lineWidth: 1))
                    .padding(.top, 4 * k)
            }
            VStack(spacing: 0) {
                Rectangle().fill(Color.white.opacity(0.15)).frame(height: 1)
                HStack {
                    Text(v.miladiLabel).font(.system(size: 12.5 * k)).foregroundColor(P.secondary)
                    Spacer(minLength: 10 * k)
                    Text(v.miladi).font(.system(size: 13.5 * k, weight: .semibold)).foregroundColor(P.primary).lineLimit(1).help(v.miladi)
                }
                .padding(.vertical, 8 * k)
                Rectangle().fill(Color.white.opacity(0.15)).frame(height: 1)
                HStack {
                    Text(v.hijriLabel).font(.system(size: 12.5 * k)).foregroundColor(P.secondary)
                    Spacer(minLength: 10 * k)
                    Text(v.hijri).font(.system(size: 13.5 * k, weight: .semibold)).foregroundColor(P.c("Green")).lineLimit(1).help(v.hijri)
                }
                .padding(.top, 8 * k)
            }
            .padding(.horizontal, 4 * k).padding(.top, 12 * k)
            if let st = v.status {
                Text(st).font(.system(size: 12 * k)).foregroundColor(Color(hex: "#FF6961"))
                    .fixedSize(horizontal: false, vertical: true).padding(.top, 6 * k)
            }
        }
    }

    func times(_ v: ViewState) -> some View {
        VStack(alignment: .leading, spacing: 0) {
            HStack(alignment: .bottom, spacing: 0) {
                Text(v.timesTitle).font(.system(size: 13 * k, weight: .semibold)).foregroundColor(P.secondary)
                Spacer(minLength: 16 * k)
                Text(v.jamaatHeader).font(.system(size: 12.5 * k, weight: .semibold)).foregroundColor(P.jamaat).underline(jamaatHover)
                    .onHover { jamaatHover = $0 }
                    .onTapGesture { n.showJamaatDialog() }
                    .help(v.jamaatTip)
            }
            .padding(EdgeInsets(top: 4 * k, leading: 12 * k, bottom: 8 * k, trailing: 12 * k))
            ForEach(v.rows) { r in TimeRow(r: r) }
        }
    }
}

// дуга 270° снизу-слева по часовой стрелке, как на сайте
struct ArcRing: View {
    let fraction: Double
    let color: Color
    var body: some View {
        Circle()
            .trim(from: 0, to: 0.75 * max(0, min(1, fraction)))
            .stroke(color, style: StrokeStyle(lineWidth: 12 * k, lineCap: .round))
            .rotationEffect(.degrees(135))
            .frame(width: 232 * k, height: 232 * k)
            .position(x: 136 * k, y: 124 * k)
    }
}

struct TimeRow: View {
    let r: RowState
    var body: some View {
        let size: CGFloat = (r.prayer ? 15 : 13.5) * k
        let ink = r.active ? P.c("Text" + r.type) : P.c("Inactive")
        HStack(spacing: 0) {
            Text(r.name).font(.system(size: size, weight: r.active ? .bold : .regular)).foregroundColor(ink).lineLimit(1)
            Spacer(minLength: 8 * k)
            Text(r.time).font(.system(size: size, weight: r.active ? .bold : .regular).monospacedDigit()).foregroundColor(ink)
            if let j = r.jamaat {
                Text(j).font(.system(size: 13.5 * k, weight: r.active ? .bold : .semibold).monospacedDigit()).foregroundColor(P.jamaat)
                    .frame(width: 46 * k, alignment: .trailing).padding(.leading, 10 * k)
            }
        }
        .padding(.horizontal, 10 * k).padding(.vertical, (r.prayer ? 5 : 3) * k)
        .background(RoundedRectangle(cornerRadius: 12 * k).fill(r.active ? P.c("Row" + r.type) : Color.clear))
        .overlay(RoundedRectangle(cornerRadius: 12 * k).stroke(P.rim, lineWidth: 1).opacity(r.active ? 1 : 0))
        .padding(.vertical, 1 * k)
    }
}

// ---------- компактная карточка ----------
struct CardView: View {
    @ObservedObject var n: Namaz

    var body: some View {
        let v = n.vs
        let jamaat = !v.cardJamaatHead.isEmpty
        HStack(spacing: 0) {
            // слева: сколько осталось до следующего намаза
            VStack(spacing: 2 * k) {
                Text(v.cardCountdown).font(.system(size: 40 * k, weight: .semibold).monospacedDigit()).foregroundColor(P.primary)
                Text(v.cardUntil).font(.system(size: 14 * k)).foregroundColor(P.secondary).multilineTextAlignment(.center)
                    .frame(maxWidth: 170 * k).fixedSize(horizontal: false, vertical: true)
            }
            Rectangle().fill(Color.white.opacity(0.15)).frame(width: 1).padding(.vertical, 4 * k).padding(.horizontal, 16 * k)
            // справа: текущий и следующий намаз, время азана и жамагата — столбцы по ширине текста
            HStack(alignment: .top, spacing: 14 * k) {
                VStack(alignment: .leading, spacing: 0) {
                    if jamaat { head(v.cardJamaatHead).hidden() }
                    cell(Text(v.cardCurName).font(.system(size: 18 * k, weight: .bold)).foregroundColor(P.c(v.cardInk)))
                    cell(Text(v.cardNextName).font(.system(size: 18 * k)).foregroundColor(P.primary))
                }
                VStack(alignment: .trailing, spacing: 0) {
                    if jamaat { head(v.cardJamaatHead).hidden() }
                    cell(Text(v.cardCurTime).font(.system(size: 20 * k, weight: .bold).monospacedDigit()).foregroundColor(P.c(v.cardInk)))
                    cell(Text(v.cardNextTime).font(.system(size: 20 * k, weight: .semibold).monospacedDigit()).foregroundColor(P.primary))
                }
                if jamaat {
                    VStack(alignment: .trailing, spacing: 0) {
                        head(v.cardJamaatHead)
                        cell(Text(v.cardCurJamaat).font(.system(size: 18 * k, weight: .semibold).monospacedDigit()).foregroundColor(P.jamaat))
                        cell(Text(v.cardNextJamaat).font(.system(size: 18 * k, weight: .semibold).monospacedDigit()).foregroundColor(P.jamaat))
                    }
                }
            }
        }
        .padding(EdgeInsets(top: 12 * k, leading: 18 * k, bottom: 12 * k, trailing: 18 * k))
        .contentShape(Rectangle())
        // нажатие — полный вид
        .onTapGesture { n.showFull() }
        .fixedSize()
    }

    // строки одинаковой высоты во всех столбцах, чтобы имя, время и жамагат стояли на одной линии
    func cell(_ t: Text) -> some View { t.lineLimit(1).frame(height: 30 * k) }
    func head(_ t: String) -> some View {
        Text(t).font(.system(size: 12 * k, weight: .semibold)).foregroundColor(P.jamaat).lineLimit(1).frame(height: 16 * k)
    }
}

// ---------- окно-уведомление ----------
struct PopupView: View {
    @ObservedObject var n: Namaz
    @State var hover = false

    var body: some View {
        let p = n.popupState
        let accent = P.c(p.accent)
        let ink = P.hex["Ink" + p.accent] != nil ? P.c("Ink" + p.accent) : accent
        VStack(spacing: 0) {
            HStack(spacing: 7) {
                Image(systemName: "mappin.and.ellipse").font(.system(size: 13, weight: .semibold)).foregroundColor(accent)
                Text(p.caption).font(.system(size: 14, weight: .semibold)).foregroundColor(P.secondary)
            }
            // название намаза светится своим цветом, как кольцо в виджете
            Text(p.title).font(.system(size: 36, weight: .semibold)).foregroundColor(ink).multilineTextAlignment(.center)
                .shadow(color: accent.opacity(0.8), radius: 9).padding(.top, 8)
            Text(p.message).font(.system(size: 17)).foregroundColor(P.primary).multilineTextAlignment(.center)
                .fixedSize(horizontal: false, vertical: true).padding(.top, 4).padding(.bottom, 18)
            Text(p.close).font(.system(size: 15, weight: .semibold)).foregroundColor(P.primary)
                .frame(maxWidth: .infinity).padding(.vertical, 10)
                .background(Capsule().fill(accent.opacity(hover ? 0.5 : 0.35)))
                .overlay(Capsule().stroke(P.rim, lineWidth: 1))
                .scaleEffect(hover ? 1.03 : 1)
                .animation(.spring(response: 0.25, dampingFraction: 0.6), value: hover)
                .contentShape(Capsule())
                .onHover { hover = $0 }
                .onTapGesture { n.closePopup() }
        }
        .padding(EdgeInsets(top: 20, leading: 26, bottom: 22, trailing: 26))
        .frame(width: 400)
        .background(GlassBackground(level: n.s.transparency))
        .contentShape(Rectangle())
        .gesture(DragGesture(minimumDistance: 3)
            .onChanged { _ in n.dragPopup(false) }
            .onEnded { _ in n.dragPopup(true) })
        .fixedSize()
        .background(SizeReader { n.resized(n.popup, $0) })
        .environment(\.colorScheme, .dark)
    }
}

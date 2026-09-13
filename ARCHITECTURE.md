# easySkills Crosshair — Architecture

Windows-Desktop-App für Steam (.NET 10, WPF, x64). Publisher: **easySkills**.

## 1. Ordner-/Projektstruktur (Separation of Concerns)

```
easySkillsCrosshair.sln
Directory.Build.props            # x64, Nullable, LangVersion latest — shared across all projects
src/
  easySkillsCrosshair.Core/          net10.0, keine WPF/Win32-Referenz
    Mvvm/            ViewModelBase, RelayCommand
    Crosshair/        CrosshairProfile, CrosshairShape, RgbaColor, DynamicReactionSettings
    Overlay/          IOverlayHost, IMonitorProvider, MonitorDescriptor, OverlayHostKind (Abstraktionen)
    Licensing/        ILicenseProvider, IFeatureGate, LicenseTier, Feature (Abstraktionen)
  easySkillsCrosshair.Overlay/       net10.0-windows, WPF
    Interop/          NativeMethods (P/Invoke: SetWindowLongPtr, SetWindowPos, GetDpiForMonitor)
    WindowsOverlayWindow.cs          IOverlayHost-Implementierung (das Topmost-Fenster)
    WindowsMonitorProvider.cs        Monitor-/DPI-Erkennung
    SimpleCrosshairRenderer.cs       Platzhalter-Rendering (siehe Abschnitt 3)
    GameBar/GameBarOverlayHost.cs    Erweiterungspunkt, noch nicht implementiert
    OverlayHostFactory.cs
  easySkillsCrosshair.Licensing/     net10.0
    TrialCatalog.cs                  die 4 fixen Shapes/Farben/Größen
    FeatureGate.cs                   IFeatureGate-Implementierung
    TrialLicenseProvider.cs          Fallback, immer Trial
    Steam/            ISteamAppsApi, SteamworksAppsApi, SteamLicenseProvider
  easySkillsCrosshair.App/           net10.0-windows, WPF, Exe — Composition Root
    App.xaml(.cs)     app.manifest (PerMonitorV2 DPI awareness), TrayIconController
    Theme/            Colors.xaml, Controls.xaml (Schwarz-Gold ResourceDictionaries)
    ViewModels/        MainViewModel + je ein ViewModel pro Sidebar-Bereich
    Views/            MainWindow + je ein UserControl pro Sidebar-Bereich
    Controls/          ProGateControl, CrosshairPreviewControl (Karten-Vorschau)
    Converters/        HexToBrushConverter
    Icons/            NavigationIcons (handgezeichnete, generische Vektor-Icons)
tests/
  easySkillsCrosshair.Licensing.Tests/   xUnit-Tests für FeatureGate

Zusätzlich in Core: Persistence/ (GameProfile, IProfileStore, JsonProfileStore) und
Community/ (ICommunityService, CommunityCrosshairListing, PlaceholderCommunityService).
```

Geplante, noch nicht angelegte Module (bewusst nicht als leere Stubs erzeugt): `CrosshairEngine` (volle Shape-Renderer, Custom-Media-Compositing), der volle Aim-Trainer (Trainingsmodi, Metriken), Input (globale Hotkeys), sowie ein separates `GameBarWidget`-Projekt, sobald das umgesetzt wird.

**Warum diese Trennung:** `Core` kennt weder WPF noch Win32 noch Steam — reine Domänenmodelle und Interfaces. `Overlay` und `Licensing` sind austauschbare Implementierungsdetails, die nur gegen `Core`-Interfaces arbeiten. `App` verdrahtet alles (Composition Root), ohne selbst Geschäftslogik zu enthalten.

## 2. Das Overlay-Fenster (100 % passiv)

`WindowsOverlayWindow` (`src/easySkillsCrosshair.Overlay/WindowsOverlayWindow.cs`) ist ein normales WPF-`Window` ohne DirectX-Hooking, Injection oder RAM-Zugriff:

- **Click-through/Topmost/No-Activate** über `WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_TOPMOST | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW`, gesetzt via `SetWindowLongPtr` in `OnSourceInitialized`.
- **Positionierung in physischen Pixeln** über `SetWindowPos` statt WPF's `Window.Left/Top` (DIP) — das umgeht die Mehrdeutigkeit von WPF's virtuellem Bildschirmkoordinatensystem bei Monitoren mit unterschiedlicher DPI-Skalierung.
- **X/Y-Offset** (`SetOffset`) addiert sich auf den Monitor-Mittelpunkt drauf, bevor in Pixel umgerechnet wird.
- **Multi-Monitor/DPI**: `WindowsMonitorProvider` liest reale Monitor-Bounds + DPI via `GetDpiForMonitor`; `app.manifest` erklärt `PerMonitorV2`, damit WPF den Inhalt pro Monitor korrekt skaliert.
- **Topmost-Keepalive**: ein 1-Sekunden-Timer setzt `HWND_TOPMOST` erneut, falls andere Overlays die Z-Order verändern.
- **Game-Bar-Erweiterungspunkt**: `IOverlayHost` + `OverlayHostFactory` + `GameBarOverlayHost` (Stub, wirft `NotSupportedException`) — der Austausch für Exclusive-Fullscreen ist später ein Einzeiler in der Factory.

**Rendering-Ansatz-Begründung:** Für Vektor-Shapes (Dot/Cross/Circle) reicht natives WPF-Rendering (`SimpleCrosshairRenderer`, hier als Platzhalter) völlig aus — die Overlay-Fläche ist winzig, die Render-Kosten sind irrelevant für die Spiel-FPS, da es ein komplett separates OS-Fenster ist. Für die volle **Crosshair Engine** (animierte GIFs, SVG-Compositing, Bloom-Interpolation mit hoher Framerate) würde sich später ein Wechsel auf **Direct2D-Interop** (`D3DImage`/`SharpDX`/`Vortice.Windows`) anbieten, weil WPF's Software-nahe Rendering-Pipeline bei GIF-Frame-Decoding + Transformationen bei 240 Hz spürbar ruckeln kann, während Direct2D das direkt auf der GPU hält. Das ist ein austauschbares Detail hinter `SimpleCrosshairRenderer`/einer künftigen `ICrosshairRenderer`-Abstraktion, nicht Teil des Overlay-Fensters selbst.

## 3. Feature-Toggle-System (Trial vs. Pro)

- `ILicenseProvider` (Core) — abstrahiert die Quelle der Lizenz. `SteamLicenseProvider` (Licensing/Steam) prüft `SteamApps.BIsDlcInstalled` über den schmalen `ISteamAppsApi`-Seam (testbar ohne echtes Steam); `TrialLicenseProvider` ist der Offline-Fallback.
- `IFeatureGate`/`FeatureGate` (Licensing) ist die **einzige Stelle**, die "Trial vs. Pro" kennt: `IsShapeAllowed`, `IsColorAllowed`, `IsSizeAllowed` gegen `TrialCatalog` (4 Shapes, 4 Farben, 4 Größen), `IsUnlocked(Feature)` für Dynamic Reactions/Custom Media/Aim-Trainer/Workshop, sowie `IsProfileWithinLicense(profile)` als Gesamt-Check.
- `App.xaml.cs` zeigt die Verdrahtung: Steam-Provider wird versucht, fällt bei fehlender `steam_api64.dll` sauber auf Trial zurück; das Overlay selbst enthält keine einzige Lizenz-Abfrage.
- DPI-Calculator ist bewusst *nicht* gated — laut Vorgabe von Anfang an voll nutzbar.

## 4. Steam-Release-Vorbereitung

- **`steam_appid.txt`** (`src/easySkillsCrosshair.App/steam_appid.txt`, Inhalt `480` = Valves öffentliche Test-AppId "Spacewar") wird per `<Content CopyToOutputDirectory="PreserveNewest">` ins Ausgabeverzeichnis kopiert, damit `SteamAPI.Init()` bei lokalen Testläufen ohne echte AppId nicht abstürzt. Vor dem echten Release durch die reale easySkills-AppId ersetzen.
- **Sauberes Beenden**: Die App läuft mit `ShutdownMode="OnExplicitShutdown"` (`App.xaml`) — sie beendet sich also *nicht* automatisch, wenn das Hauptfenster oder das Overlay-Fenster geschlossen wird ("X" auf dem Hauptfenster versteckt es nur in den Tray, siehe `MainWindow.OnClosing`). `TrayIconController` (App-Projekt) stellt ein Tray-Icon mit "Öffnen"/"Beenden" bereit; "Beenden" ruft `Application.Shutdown()` auf. `App.OnExit` räumt in fester Reihenfolge auf: Tray-Icon disposen (verhindert ein "hängendes" Icon), Overlay disposen (stoppt den Topmost-Keepalive-Timer), `SteamAPI.Shutdown()` über `ISteamAppsApi.Shutdown()` — und beendet den Prozess anschließend hart mit `Environment.Exit(0)`, damit Steam nach dem Schließen niemals dauerhaft "Wird ausgeführt" anzeigt.
- **Trial-Fallback-Validierung**: Das Konstruieren des Steamworks-Wrappers (`SteamworksAppsApi`, kann bei fehlendem/inkompatiblem `steam_api64.dll` werfen) ist bewusst von der eigentlichen Trial/Pro-Entscheidung getrennt (`LicenseProviderFactory.CreateFromSteam`). Dadurch ist der Fallback-Pfad — Steam-Client läuft nicht im Hintergrund → `TrialLicenseProvider` — unabhängig von echtem Steam per Unit-Test abgesichert (`LicenseProviderFactoryTests`), nicht nur durch manuelles Nachvollziehen.

## 5. GUI (WPF, MVVM, eigenes Schwarz-Gold-Styling)

- **Theme**: `Theme/Colors.xaml` (Farbpalette: `#0A0A0A`/`#121212` Basis, `#D4AF37` Gold-Akzent) + `Theme/Controls.xaml` (komplett eigene `ControlTemplate`s für Button/Slider/ComboBox/TextBox/CheckBox/Card/Sidebar-Nav — kein Drittanbieter-Theme als Basis).
- **Navigation**: `MainWindow` zeigt eine linke Sidebar (`ListBox` mit eigenem `ItemContainerStyle`) gegen `MainViewModel.NavigationItems`; der Content-Bereich ist ein `ContentControl`, dessen `DataTemplate`s (in `MainWindow.xaml`) jeden ViewModel-Typ auf sein `UserControl` mappen — klassische MVVM-Navigation ohne Code-Behind-Umschaltung.
- **Live-Vorschau, doppelt verdrahtet**: `CrosshairEditorViewModel` mutiert **ein** `CrosshairProfile`-Objekt in-place und rendert nach jeder Änderung über denselben `SimpleCrosshairRenderer`, den auch `WindowsOverlayWindow` benutzt — einmal direkt in die eigene Editor-Canvas (`CrosshairEditorView` hört auf das `PreviewChanged`-Event, da In-Place-Mutation die Referenzgleichheits-Erkennung von WPF-DependencyProperties umgeht) und einmal über `IOverlayHost.UpdateContent(...)` ins echte Overlay-Fenster auf dem Bildschirm. Für Karten-Ansichten (Profile/Community, jedes Element ein *eigenes* `CrosshairProfile`) übernimmt stattdessen `CrosshairPreviewControl` (eine DependencyProperty-basierte, wiederverwendbare Miniatur), da dort Referenzgleichheit ganz normal funktioniert.
- **Trial/Pro-Kennzeichnung**: `ProGateControl` (eigenes `ContentControl`) umschließt jeden Pro-only-Bereich (Custom-Shape, freier Hex/RGB/HSV-Picker, freier Größen-Regler, Dynamic Reactions, Custom-Media-Upload) — im gesperrten Zustand wird der Inhalt gedimmt/deaktiviert statt versteckt, plus dezentes Gold-umrandetes "PRO"-Badge mit Schloss-Icon. Die Sperr-Entscheidung kommt in jedem Fall aus `IFeatureGate`, nie aus der View selbst.
- **Profile/Community laden**: `ProfilesViewModel`/`CommunityViewModel` rufen `CrosshairEditorViewModel.ApplyProfile(...)` auf; diese Methode klemmt ein geladenes Profil aktiv auf die aktuelle Lizenzstufe (`IFeatureGate.IsShapeAllowed`/`IsColorAllowed`/`IsSizeAllowed`/`IsUnlocked`), bevor es angezeigt wird — ein importiertes Pro-Profil kann eine laufende Trial-Sitzung also nicht stillschweigend "hochstufen".
- **`ICommunityService`** (Core/Community) ist rein interface-basiert; `PlaceholderCommunityService` liefert statische Demo-Daten ohne Netzwerkzugriff. Ein späterer Server-Client ersetzt nur die Implementierung — `CommunityViewModel` und die View bleiben unverändert.
- **Persistenz**: `IProfileStore`/`JsonProfileStore` (Core/Persistence) speichert Per-Game-Profile als JSON unter `%AppData%\easySkills\Crosshair\profiles.json`.
- **Bewusst nicht in dieser Version**: der volle Aim-Trainer (nur Platzhalter-Ansicht mit Feature-Gate-Anzeige) und die per-Spiel-Yaw-Datenbank im DPI-Calculator (nur 3 verifizierte Presets + freies Yaw-Feld für alles andere) — beides eigenständige, größere Folge-Aufgaben.

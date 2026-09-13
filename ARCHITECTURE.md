# easySkills Crosshair — Architecture

Windows-Desktop-App für Steam (.NET 10, WPF, x64). Publisher: **easySkills**.

## 1. Ordner-/Projektstruktur (Separation of Concerns)

```
easySkillsCrosshair.sln
Directory.Build.props            # x64, Nullable, LangVersion latest — shared across all projects
src/
  easySkillsCrosshair.Core/          net10.0, keine WPF/Win32-Referenz
    Mvvm/            ViewModelBase, RelayCommand
    Crosshair/       CrosshairProfile, CrosshairLayer, LayerType, RgbaColor, DynamicReactionSettings
    Reactions/       ReactionEngine, CrosshairRenderState (Bloom/ADS/Bewegung, reine Logik)
    AimTraining/     AimTrainingSession (Modi, Ziele, Scoring — reine Logik)
    Sensitivity/     SensitivityMath, GameCatalog, games.json (eingebettet), SensitivityConverterState
    Community/       ICommunityService, LocalCommunityService, CrosshairShareCodec, CrosshairPresets
    Persistence/     GameProfile, IProfileStore, JsonProfileStore
    Overlay/         IOverlayHost, IMonitorProvider, MonitorDescriptor, OverlayHostKind
    Licensing/       ILicenseProvider, ILicenseSwitch, IFeatureGate, LicenseTier, Feature
  easySkillsCrosshair.Overlay/       net10.0-windows, WPF (+ SharpVectors.Wpf für SVG)
    Rendering/       CrosshairDrawing (einziger Renderpfad), CrosshairVisual, ImageAssetCache/ImageAsset
    Input/           RawInputListener (Maus/WASD per Raw Input, rein lesend)
    Interop/         NativeMethods (SetWindowLongPtr, SetWindowPos, GetDpiForMonitor)
    WindowsOverlayWindow.cs, WindowsMonitorProvider.cs, OverlayHostFactory.cs
    GameBar/GameBarOverlayHost.cs    Erweiterungspunkt, noch nicht implementiert
  easySkillsCrosshair.Licensing/     net10.0
    TrialCatalog, FeatureGate (inkl. ClampToLicense), TrialLicenseProvider,
    SwitchableLicenseProvider (Dev/QA), LicenseProviderFactory, Steam/
  easySkillsCrosshair.App/           net10.0-windows, WPF, Exe — Composition Root
    App.xaml(.cs), app.manifest (PerMonitorV2), TrayIconController
    Theme/           Colors.xaml, Controls.xaml (Schwarz-Gold ResourceDictionaries)
    ViewModels/      MainViewModel, CrosshairEditorViewModel, LayerViewModel, AimTrainingViewModel, …
    Views/           MainWindow + je ein UserControl pro Sidebar-Bereich
    Controls/        ProGateControl, AimArenaElement
    Converters/, Icons/
tests/
  easySkillsCrosshair.Licensing.Tests/   xUnit: FeatureGate, Lizenz-Fallback, ReactionEngine,
                                         ShareCodec, AimTrainingSession
```

**Warum diese Trennung:** `Core` kennt weder WPF noch Win32 noch Steam — Domänenmodelle, Interfaces und die gesamte testbare Spiel-/Reaktions-/Sharing-Logik. `Overlay` und `Licensing` sind austauschbare Implementierungsdetails gegen `Core`-Interfaces. `App` verdrahtet alles und enthält keine Geschäftslogik.

## 2. Das Overlay-Fenster (100 % passiv)

`WindowsOverlayWindow` ist ein normales WPF-`Window` ohne DirectX-Hooking, Injection oder RAM-Zugriff:

- **Click-through/Topmost/No-Activate** über `WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_TOPMOST | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW`.
- **Positionierung in physischen Pixeln** über `SetWindowPos` (umgeht WPF-DIP-Mehrdeutigkeit bei gemischter DPI); **X/Y-Offset** relativ zum Monitor-Mittelpunkt.
- **Multi-Monitor/DPI** via `GetDpiForMonitor` + `PerMonitorV2`-Manifest; **Topmost-Keepalive** jede Sekunde.
- **Game-Bar-Erweiterungspunkt** hinter `IOverlayHost`/`OverlayHostFactory`.

## 3. Crosshair-Engine (Pro)

- **Ebenen-Modell**: Ein `CrosshairProfile` ist ein Stapel `CrosshairLayer` (unten → oben). Typen: Fadenkreuz (Länge/Breite/Abstand/T-Form), Punkt, Kreis, X-Kreuz, Box (gefüllt/offen), Chevron, Linie, Bild. Jede Ebene hat eigene Farbe, Deckkraft, Kontur, Offset und Rotation — dadurch sind die Formen praktisch unbegrenzt kombinierbar.
- **Ein Renderpfad**: `CrosshairDrawing` zeichnet direkt in einen `DrawingContext` und wird vom Overlay, der Editor-Vorschau und allen Miniaturen gleichermaßen genutzt — Vorschau und Overlay können nicht auseinanderlaufen. Immediate-Mode statt eines neu aufgebauten Shape-Baums hält Slider-Drags, Bloom-Animation und GIF-Frames allokationsarm; Pixel-Zentrierung hält 1-/2-px-Arme scharf.
- **Custom Media**: `ImageAssetCache` dekodiert einmal pro Datei (Pfad + Änderungszeit): PNG, **animiertes GIF** (korrektes Frame-Compositing inkl. Offsets und Disposal) und **SVG** (SharpVectors → echte WPF-Vektorgrafik). Defekte Dateien zeichnen nichts statt zu crashen. `CrosshairVisual` abonniert Frame-Updates nur, solange tatsächlich ein animiertes GIF sichtbar ist.
- **Dynamic Reactions**: `RawInputListener` registriert Raw Input mit `RIDEV_INPUTSINK` — bewusst **kein** `WH_MOUSE_LL`-Hook, da Low-Level-Hooks synchron in der Eingabekette laufen und Latenz erzeugen können; Raw Input erhält nur asynchrone Kopien und verändert nichts, was das Spiel sieht. Die Registrierung ist nur aktiv, solange das aktive Profil Reaktionen nutzt. `ReactionEngine` (Core, getestet) berechnet daraus Bloom (voll bei gehaltener LMT, lineare Erholung), Ausblenden bei RMT und T-Form bei WASD. Eingaben werden ignoriert, solange die App selbst im Vordergrund ist. Der Pro-Frame-Loop läuft nur während einer abklingenden Bloom-Animation.

## 4. Feature-Toggle-System (Trial vs. Pro)

- `ILicenseProvider` abstrahiert die Lizenzquelle: `SteamLicenseProvider` (`BIsDlcInstalled`), `TrialLicenseProvider` (Fallback), `SwitchableLicenseProvider` (Dev/QA, zur Laufzeit umschaltbar).
- `IFeatureGate`/`FeatureGate` ist die **einzige Stelle**, die Trial vs. Pro kennt: `IsLayerTypeAllowed`/`IsColorAllowed`/`IsSizeAllowed` gegen `TrialCatalog` (Trial: 1 Ebene; Punkt/Kreuz/Kreis + T-Form; 4 Farben; 4 Größen; keine Kontur/Bilder/Reaktionen), `IsUnlocked(Feature)` für `MultipleLayers`, `DynamicReactions`, `CustomMediaUpload`, `FullAimTrainer`, `CommunitySharing`, sowie **`ClampToLicense`** — liefert eine auf die Stufe reduzierte Kopie. Jedes geladene Profil (Profile, Community, Tier-Wechsel) läuft da durch; ein Pro-Profil kann eine Trial-Sitzung nie stillschweigend hochstufen.
- `IFeatureGate.Changed` propagiert Tier-Wechsel live an alle ViewModels — kein Neustart nötig.
- **Pro-Schalter in Settings**: In **Debug-Builds** immer vorhanden, Start in Pro. In **Release-Builds** nur mit `EASYSKILLS_DEV_LICENSE=1` (Start Trial) oder `EASYSKILLS_FORCE_PRO=1` (Start Pro); ohne diese entscheidet ausschließlich das Steam-DLC — ein Steam-Release enthält keinen erreichbaren Gratis-Pro-Schalter.
- DPI-Calculator ist bewusst *nicht* gated.

## 5. Steam-Release-Vorbereitung

- **`steam_appid.txt`** (`480` = Test-AppId „Spacewar") wird ins Ausgabeverzeichnis kopiert. Vor Release echte AppId eintragen und `ProDlcAppId` in `App.xaml.cs` setzen.
- **Sauberes Beenden**: `ShutdownMode="OnExplicitShutdown"`; „X" versteckt das Hauptfenster in den Tray, nur Tray → „Beenden" beendet. `OnExit` disposed Tray-Icon, Overlay (inkl. Raw Input) und Steam API und erzwingt danach `Environment.Exit(0)`, damit Steam nie dauerhaft „Wird ausgeführt" zeigt.
- **Trial-Fallback** bei fehlendem Steam per Unit-Test abgesichert (`LicenseProviderFactoryTests`).
- **Offen**: `RefreshAsync` läuft nur beim Start — ein DLC-Kauf während der Sitzung greift erst nach Neustart (Auslöser über `DlcInstalled_t`-Callback oder periodisches Refresh nachrüsten; die Live-Update-Kette steht bereits).

## 6. GUI (WPF, MVVM, eigenes Schwarz-Gold-Styling)

- **Theme**: eigene `ControlTemplate`s für Button/Slider/ComboBox/TextBox/CheckBox/ToggleSwitch/ScrollBar/Card/Nav — kein Drittanbieter-Theme.
- **Navigation**: Sidebar-`ListBox` gegen `MainViewModel`; `DataTemplate`s mappen ViewModel-Typen auf Views.
- **Crosshair-Editor**: Ebenenliste (hinzufügen, duplizieren, sortieren, ein-/ausblenden, löschen) | Live-Vorschau mit Zoom und 1:1-Inset | Inspektor mit typabhängigen Abschnitten (Form-Kacheln als Live-Miniaturen, Hex/RGB/HSV, Größe, Breite/Abstand/T-Form, Kontur, Bild, Ebenen-Transform, Gesamt-Transform, Dynamic Reactions). Das Profil wird in-place mutiert; Views aktualisieren die Vorschau über das `PreviewChanged`-Event und melden sich bei `Unloaded` ab, damit neu erzeugte Views nicht über das langlebige ViewModel leaken.
- **Schießstand**: `AimTrainingSession` (Core) + `AimArenaElement` (Immediate-Mode-Rendering) + dein aktuelles Fadenkreuz als Cursor. Modi Flick/Präzision/Tracking, Dauer 0,5–30 min, Metriken Zeit/Score/Treffer/Präzision/Ø-Reaktion. Trial: 30-Sekunden-Flick-Vorschau.
- **Community**: `LocalCommunityService` — eingebaute Presets + lokale Bibliothek (`%AppData%\easySkills\Crosshair\community.json`). Teilen per **Share-Code** (`ESC1:` + gzip + Base64URL; ohne Bilddaten, lokale Pfade werden nie geteilt) oder **Datei** (`.escrosshair`, Bilder eingebettet und beim Import inhaltsadressiert nach `media\` entpackt). Decoder ist größenbegrenzt, versioniert und wirft ausschließlich `FormatException`. Steam Workshop ist später hinter `ICommunityService` nachrüstbar.
- **Trial/Pro-Kennzeichnung**: `ProGateControl` dimmt/deaktiviert gesperrte Bereiche und zeigt ein Gold-„PRO"-Badge (kompakte Icon-Variante für kleine Kacheln).
- **Sensitivity Converter** (Sidebar „DPI Calculator", in jeder Lizenzstufe frei): Quelle/Ziel-Spiel, Sensitivity, DPI getrennt je Seite → umgerechnete Sensitivity (gerundet auf die Einstellungs-Präzision des Zielspiels) plus exakter Wert, cm/360°, inch/360°, eDPI. Methode: gleiche 360°-Mausdistanz, `sens_to = sens_from · yaw_from · dpi_from / (yaw_to · dpi_to)`. Spieldaten in `games.json`: nur Titel mit linearer, gut belegter Umrechnung, jeder Faktor mit Herkunftsnotiz; dazu „Benutzerdefiniert" mit eigenem Faktor. Die Kopie `Data\games.json` neben der exe ist editierbar (Korrekturen nach Game-Patches ohne Rebuild); ist sie ungültig, greift die eingebettete Kopie mit sichtbarer Warnung. Eingaben akzeptieren Komma und Punkt; die letzte Auswahl wird unter `%AppData%\easySkills\Crosshair\sensitivity-converter.json` gespeichert.
- **Persistenz**: Per-Game-Profile als JSON unter `%AppData%\easySkills\Crosshair\profiles.json`.

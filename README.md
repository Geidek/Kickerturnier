# Kickerturnier - Foosball Tournament Management

Eine vollständige Blazor WebAssembly-Anwendung zur Verwaltung von Kickerturnieren (Tischfußball) mit .NET 10.

## 📋 Überblick

Diese Anwendung ermöglicht die Verwaltung eines Kickerturniers mit 5 Teams (oder mehr). Das Turnier besteht aus einer **Vorrunde** (Jeder-gegen-Jeden) und einer **Finalrunde** mit Finale und Spiel um Platz 3.

## ✨ Features

### Turniermodus
- **Vorrunde**: Round-Robin-System, jedes Team spielt gegen jedes andere Team einmal (10 Spiele bei 5 Teams)
- **Punktesystem**: Sieg = 3 Punkte, Unentschieden = 1 Punkt, Niederlage = 0 Punkte
- **Live-Tabelle**: Automatische Berechnung mit Sortierung nach:
  1. Punkte
  2. Tordifferenz
  3. Erzielte Tore
  4. Direkter Vergleich
- **Finalrunde**: Automatische Generierung nach Abschluss der Vorrunde
  - Finale: Platz 1 vs Platz 2
  - Spiel um Platz 3: Platz 3 vs Platz 4
  - Platz 5 bleibt auf Platz 5

### Funktionen
- ✅ Teams verwalten (Erstellen, Bearbeiten, Löschen)
- ✅ Turnier starten mit automatischer Spielplan-Generierung
- ✅ Spielergebnisse eintragen und bearbeiten
- ✅ Live-aktualisierte Tabelle
- ✅ Turnier-Bracket-Ansicht für Finalrunde
- ✅ Turniersieger-Anzeige
- ✅ Lokale Speicherung im Browser (LocalStorage)
- ✅ Beispiel-Teams für schnellen Start
- ✅ Responsive Design mit Fluent UI

## 🚀 Installation und Start

### Voraussetzungen
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Anwendung starten
```bash
# In das Projektverzeichnis wechseln
cd Kickerturnier

# Anwendung ausführen
dotnet run

# Im Browser öffnen
# Navigiere zu: http://localhost:5000
```

### Build für Produktion
```bash
dotnet publish -c Release
# Die veröffentlichten Dateien befinden sich in: bin/Release/net10.0/publish/wwwroot/
```

## 📖 Verwendung

### 1. Teams verwalten
- Navigiere zu "Teams verwalten"
- Füge Teams hinzu (Teamname und optional 2 Spielernamen)
- Mindestens 2 Teams sind erforderlich
- Klicke auf "Turnier starten" um die Vorrunde zu beginnen

### 2. Vorrunde
- Trage Spielergebnisse ein (Tore für beide Teams)
- Die Tabelle wird automatisch aktualisiert
- Alle 10 Spiele müssen abgeschlossen sein
- Die Finalrunde wird automatisch generiert

### 3. Finalrunde
- Sehe das Turnier-Bracket mit Finale und Spiel um Platz 3
- Trage die Endergebnisse ein
- Der Turniersieger wird hervorgehoben angezeigt

## 🏗️ Architektur

### Projektstruktur
```
Kickerturnier/
├── Models/              # Domain-Modelle
│   ├── Team.cs          # Team mit 2 Spielern
│   ├── Match.cs         # Spiel zwischen 2 Teams
│   ├── MatchPhase.cs    # Turnierphase (Vorrunde/Finale/Platz3)
│   └── Standing.cs      # Tabellenplatz mit Statistiken
├── Services/            # Business-Logik
│   ├── TournamentService.cs      # Kernlogik des Turniers
│   └── LocalStorageService.cs    # Browser-Speicherung
├── Pages/               # Blazor-Seiten
│   ├── Home.razor                # Startseite
│   ├── Teams.razor               # Teamverwaltung
│   ├── GroupStage.razor          # Vorrunde
│   └── FinalStage.razor          # Finalrunde
├── Layout/              # Layout-Komponenten
│   └── MainLayout.razor          # Hauptlayout mit Navigation
└── wwwroot/             # Statische Dateien
```

### Technologie-Stack
- **Framework**: Blazor WebAssembly (Standalone)
- **Runtime**: .NET 10.0
- **UI-Framework**: Microsoft Fluent UI for Blazor 4.13.2
- **State Management**: Singleton Service Pattern
- **Persistenz**: Browser LocalStorage

## 🎮 Beispiel-Teams

Die Anwendung enthält 5 vordefinierte Teams für einen schnellen Start:
1. **FC Tornado** - Max Mustermann & Anna Schmidt
2. **Die Kicker** - Tom Müller & Lisa Weber
3. **Tischmeister** - Jan Becker & Sarah Klein
4. **Ballmagier** - Lukas Wagner & Emma Hoffmann
5. **Torjäger** - Felix Schulz & Nina Fischer

## 🔧 Entwicklung

### Projektaufbau
```bash
# Projekt erstellen (bereits vorhanden)
dotnet new blazorwasm -n Kickerturnier -f net10.0

# Abhängigkeiten hinzufügen
dotnet add package Microsoft.FluentUI.AspNetCore.Components
dotnet add package Microsoft.FluentUI.AspNetCore.Components.Icons
```

### Code-Struktur
- **TournamentService**: Zentrale Geschäftslogik für:
  - Round-Robin-Spielplan-Generierung
  - Tabellen-Berechnung mit Tie-Breaking
  - Automatische Finalrunden-Generierung
  - Serialisierung/Deserialisierung
  
- **LocalStorageService**: Wrapper für Browser LocalStorage via JavaScript Interop

- **Pages**: Razor-Komponenten mit Event-Handling und reaktiver UI

## 📝 Lizenz

Dieses Projekt wurde als Beispielanwendung erstellt.

## 🤝 Beitragen

Verbesserungen und Erweiterungen sind willkommen!

Mögliche Erweiterungen:
- Export als PDF
- Statistiken und Charts
- Turnierhistorie
- Mehr Teams unterstützen (UI-Skalierung)
- Druckfreundliches CSS

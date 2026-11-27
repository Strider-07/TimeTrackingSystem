# TimeTrackingSystem

Aplikacja webowa do zarządzania pracownikami, ewidencji czasu pracy i obsługi wniosków urlopowych.
Projekt praktyczny – ASP.NET Core MVC + PostgreSQL.

## Funkcje

- Logowanie (email + hasło), zmiana i reset hasła (link w emailu), automatyczne wylogowanie po 15 min bezczynności
- Role: **Administrator**, **Kierownik**, **Pracownik**
- Panel administratora: CRUD użytkowników, role, aktywacja/dezaktywacja, reset hasła, raporty (czas pracy, wnioski) z eksportem do CSV i PDF
- Ewidencja czasu pracy: wejście/wyjście, historia, suma godzin w miesiącu, jeden wpis na dzień, korekta wpisu przez kierownika z uzasadnieniem
- Wnioski urlopowe: składanie, akceptacja/odrzucenie z komentarzem, statusy, powiadomienia email
- Powiadomienia email (MailKit/SMTP): powitanie, reset hasła, potwierdzenie wniosku, decyzja, przypomnienie o brakujących wpisach (codziennie 9:00, dni robocze)
- Dodatkowo: dashboard z wykresami (Chart.js), eksport PDF (iTextSharp), kalendarz urlopów (FullCalendar), dark mode

## Wymagania

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- PostgreSQL 14+
- narzędzie EF Core: `dotnet tool install --global dotnet-ef`
- (opcjonalnie) lokalny serwer SMTP do testów, np. [smtp4dev](https://github.com/rnwood/smtp4dev) lub MailHog

## Uruchomienie

### 1. Baza danych

Utwórz pustą bazę:

```sql
CREATE DATABASE "TimeTrackingSystem";
```

### 2. Konfiguracja (User Secrets)

Hasła **nie są** w repozytorium. W katalogu projektu wykonaj:

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:PostgreSQL" "Host=localhost;Port=5440;Database=TimeTrackingSystem;Username=postgres;Password=TWOJE_HASLO"

# SMTP (przykład: Gmail z hasłem aplikacji)
dotnet user-secrets set "EmailSettings:SmtpServer" "smtp.gmail.com"
dotnet user-secrets set "EmailSettings:SmtpPort" "587"
dotnet user-secrets set "EmailSettings:SenderEmail" "twoj-email@gmail.com"
dotnet user-secrets set "EmailSettings:Username" "twoj-email@gmail.com"
dotnet user-secrets set "EmailSettings:Password" "haslo-aplikacji"
dotnet user-secrets set "EmailSettings:EnableSsl" "true"
```

Skrypt SQL należy uruchomić z katalogu głównego projektu, po zastosowaniu migracji.
Jeśli polecenie psql nie jest dostępne w terminalu, należy użyć pełnej ścieżki do psql.exe

W Visual Studio: prawy klik na projekcie → **Manage User Secrets**.
Alternatywa: zmienne środowiskowe, np. `ConnectionStrings__PostgreSQL`, `EmailSettings__Password`.

> Do testów bez prawdziwego SMTP można użyć smtp4dev (`SmtpServer=localhost`, `SmtpPort=25`, `EnableSsl=false`, puste `Username`/`Password`).
> Błędy wysyłki nie zatrzymują aplikacji – są zapisywane w tabeli `EmailLogs`.

### 3. Migracje

Migracje są stosowane **automatycznie przy starcie aplikacji**. Ręcznie:

```bash
dotnet ef database update
```

### 4. Dane testowe

- W środowisku **Development** konta z tabeli poniżej są tworzone automatycznie przy starcie.
- Pełny zestaw (z wpisami czasu pracy i wnioskami) – skrypt SQL, uruchamiany po migracjach:

```bash
psql -h localhost -U postgres -d TimeTrackingSystem -f db/seed-test-data.sql
```

### 5. Start

```bash
dotnet run
```

Aplikacja: `https://localhost:7047` (lub `http://localhost:5271`).

## Konta testowe (hasło: `Test123!`)

| Email                              | Rola          | Uwagi              |
|------------------------------------|---------------|--------------------|
| adam.kowalski@techcorp.local       | Administrator |                    |
| anna.nowak@techcorp.local          | Kierownik     |                    |
| piotr.wisniewski@techcorp.local    | Pracownik     |                    |
| marta.zielinska@techcorp.local     | Pracownik     |                    |
| jan.kowalczyk@techcorp.local       | Pracownik     | konto nieaktywne (tylko skrypt SQL) |

## Struktura projektu

```
Controllers/   kontrolery MVC (Account, Admin, TimeEntry, LeaveRequest, LeaveCalendar, Dashboard, Home)
Models/
  Entities/    encje EF Core
  ViewModels/  modele widoków z walidacją
Data/          ApplicationDbContext, inicjalizacja ról, seeder
Services/      EmailService, PdfReportBuilder, MissingEntriesReminderService
Views/         widoki Razor (Bootstrap 5)
Migrations/    migracje EF Core
db/            skrypt SQL z danymi testowymi
docs/          dokumentacja techniczna
```

Dokumentacja techniczna (architektura, diagramy): [docs/DOKUMENTACJA.md](docs/DOKUMENTACJA.md)

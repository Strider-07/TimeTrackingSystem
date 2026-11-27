# Dokumentacja techniczna – TimeTrackingSystem

## 1. Technologie

| Warstwa        | Technologia                                   |
|----------------|-----------------------------------------------|
| Backend        | ASP.NET Core MVC (.NET 10)                    |
| Baza danych    | PostgreSQL 14+ (Npgsql + Entity Framework Core) |
| Autoryzacja    | ASP.NET Core Identity (cookie, role)          |
| Frontend       | Razor Views, Bootstrap 5, Bootstrap Icons     |
| Email          | MailKit (SMTP)                                |
| Dodatki        | Chart.js, FullCalendar, iTextSharp (PDF)      |

## 2. Architektura

Klasyczny MVC z warstwą usług. Kontrolery korzystają z `ApplicationDbContext` i `UserManager` (Identity), a wysyłkę emaili i generowanie PDF delegują do usług.

```mermaid
flowchart LR
    B[Przeglądarka] -->|HTTP| C[Kontrolery MVC]
    C --> V[Widoki Razor]
    C --> D[(ApplicationDbContext / EF Core)]
    C --> I[Identity: UserManager, SignInManager]
    C --> E[IEmailService]
    C --> P[PdfReportBuilder]
    I --> D
    E -->|MailKit| S[Serwer SMTP]
    E -->|log| D
    D --> PG[(PostgreSQL)]
    R[MissingEntriesReminderService<br/>BackgroundService, 9:00] --> E
    R --> D
```

### Kontrolery

| Kontroler        | Rola dostępu                         | Zadanie                                              |
|------------------|--------------------------------------|------------------------------------------------------|
| Account          | anonimowy / zalogowany               | logowanie, zmiana i reset hasła, wylogowanie         |
| Admin            | Administrator                        | użytkownicy, role, raporty, eksport CSV/PDF          |
| TimeEntry        | wszystkie; zarządzanie: Admin/Kierownik | rejestracja czasu, historia, korekty              |
| LeaveRequest     | wszystkie; decyzje: Admin/Kierownik  | wnioski urlopowe i ich rozpatrywanie                 |
| LeaveCalendar    | zalogowani                           | kalendarz urlopów (JSON dla FullCalendar)            |
| Dashboard        | Administrator, Kierownik             | statystyki i wykresy                                 |

### Bezpieczeństwo

- hasła hashowane przez Identity, blokada konta po wielokrotnych błędnych logowaniach
- tokeny antyforgery (`[ValidateAntiForgeryToken]`) w formularzach POST
- globalny filtr wymagający uwierzytelnienia + `[Authorize(Roles = ...)]`
- ciasteczko HttpOnly, sesja wygasa po 15 min bezczynności (sliding)
- EF Core (zapytania parametryzowane) – brak SQL Injection
- sekrety (connection string, hasło SMTP) w User Secrets / zmiennych środowiskowych
- treści emaili w `EmailLogs` maskują hasła i tokeny

## 3. Model danych

```mermaid
erDiagram
    AspNetUsers ||--o{ TimeEntries : "UserId"
    AspNetUsers ||--o{ TimeEntries : "ModifiedById"
    AspNetUsers ||--o{ LeaveRequests : "UserId"
    AspNetUsers ||--o{ LeaveRequests : "ReviewerId"
    AspNetUsers }o--o{ AspNetRoles : "AspNetUserRoles"

    AspNetUsers {
        string Id PK
        string Email
        string PasswordHash
        string FirstName
        string LastName
        string Position
        string Department
        bool IsActive
        datetime CreatedAt
    }
    TimeEntries {
        int Id PK
        string UserId FK
        date EntryDate
        datetime StartTime
        datetime EndTime
        decimal TotalHours
        string Note
        datetime CreatedAt
        string ModifiedById FK
    }
    LeaveRequests {
        int Id PK
        string UserId FK
        string LeaveType
        date StartDate
        date EndDate
        string Reason
        string Status
        string ReviewerId FK
        string ReviewerComment
        datetime RequestDate
        datetime DecisionDate
    }
    EmailLogs {
        int Id PK
        string RecipientEmail
        string Subject
        string Body
        datetime SentAt
        bool IsSuccess
        string ErrorMessage
    }
    AspNetRoles {
        string Id PK
        string Name
    }
```

Uwagi:
- `TotalHours` jest przeliczane automatycznie w `ApplicationDbContext.SaveChanges` dla dodanych/zmienionych wpisów.
- Daty/godziny przechowywane w UTC (`timestamptz`), `EntryDate` to lokalna data wpisu.
- Statusy wniosku: `Oczekujący`, `Zaakceptowany`, `Odrzucony`.

## 4. Kluczowe procesy

### Wniosek urlopowy

```mermaid
sequenceDiagram
    actor P as Pracownik
    actor K as Kierownik
    participant A as Aplikacja
    participant DB as PostgreSQL
    participant M as SMTP

    P->>A: Złóż wniosek (typ, daty, uwagi)
    A->>A: Walidacja (koniec ≥ początek, start ≥ dziś)
    A->>DB: Zapis (status: Oczekujący)
    A->>M: Email z potwierdzeniem złożenia
    K->>A: Lista oczekujących wniosków
    K->>A: Akceptuj / Odrzuć (+ komentarz)
    A->>DB: Zmiana statusu, ReviewerId, DecisionDate
    A->>M: Email z decyzją do pracownika
```

### Stany wniosku

```mermaid
stateDiagram-v2
    [*] --> Oczekujący
    Oczekujący --> Zaakceptowany : kierownik akceptuje
    Oczekujący --> Odrzucony : kierownik odrzuca (komentarz wymagany)
    Zaakceptowany --> [*]
    Odrzucony --> [*]
```

### Ewidencja czasu pracy

1. „Rozpocznij pracę” – tworzy wpis (blokada: aktywny wpis lub wpis z dzisiejszą datą już istnieje → jeden wpis na dzień).
2. „Zakończ pracę” – ustawia `EndTime`, `TotalHours` liczone przy zapisie.
3. Kierownik/Administrator może skorygować wpis (wymagane uzasadnienie w `Note`, zapisywany `ModifiedBy`).

### Przypomnienia email

`MissingEntriesReminderService` (BackgroundService) codziennie o 9:00 (Europe/Warsaw, dni robocze) wysyła przypomnienie aktywnym pracownikom bez wpisu z poprzedniego dnia roboczego i bez zaakceptowanego urlopu.

## 5. Konfiguracja

| Klucz                              | Opis                         |
|------------------------------------|------------------------------|
| `ConnectionStrings:PostgreSQL`     | connection string do bazy    |
| `EmailSettings:*`                  | SmtpServer, SmtpPort, SenderEmail, SenderName, Username, Password, EnableSsl |

Wartości wrażliwe – w User Secrets (patrz README).
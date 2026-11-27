-- =====================================================================
-- TimeTrackingSystem - dane testowe (PostgreSQL 14+)
-- Uruchamiać PO zastosowaniu migracji (tabele muszą istnieć).
-- Skrypt jest bezpieczny do ponownego uruchomienia (nie duplikuje danych).
--
-- Hasło wszystkich kont: Test123!
-- Daty (wpisy czasu pracy, wnioski) są liczone względem dnia uruchomienia.
-- =====================================================================

BEGIN;

-- ---------------------------------------------------------------------
-- 1. Role (tworzone też przez aplikację przy starcie)
-- ---------------------------------------------------------------------
INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
SELECT gen_random_uuid()::text, r, upper(r), gen_random_uuid()::text
FROM (VALUES ('Administrator'), ('Kierownik'), ('Pracownik')) AS t(r)
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------
-- 2. Użytkownicy (hash = ASP.NET Identity v3, hasło: Test123!)
-- ---------------------------------------------------------------------
INSERT INTO "AspNetUsers"
    ("Id", "FirstName", "LastName", "Position", "Department", "IsActive", "CreatedAt",
     "UserName", "NormalizedUserName", "Email", "NormalizedEmail", "EmailConfirmed",
     "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
     "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount")
SELECT gen_random_uuid()::text, v.fn, v.ln, v.pos, v.dep, v.active, now(),
       v.email, upper(v.email), v.email, upper(v.email), true,
       'AQAAAAEAAYagAAAAECXPJHwUOOEj84LysAlUUOYHSSgRdOvDV3e5Hle1KN9atLgIivLmXttL04AwHsFjHA==',
       replace(gen_random_uuid()::text, '-', ''), gen_random_uuid()::text,
       false, false, true, 0
FROM (VALUES
    ('Adam',  'Kowalski',     'Administrator systemu', 'IT', true,  'adam.kowalski@techcorp.local'),
    ('Anna',  'Nowak',        'Team Leader',           'IT', true,  'anna.nowak@techcorp.local'),
    ('Piotr', 'Wiśniewski',   'Backend Developer',     'IT', true,  'piotr.wisniewski@techcorp.local'),
    ('Marta', 'Zielińska',    'Frontend Developer',    'IT', true,  'marta.zielinska@techcorp.local'),
    ('Jan',   'Kowalczyk',    'Tester',                'IT', false, 'jan.kowalczyk@techcorp.local')
) AS v(fn, ln, pos, dep, active, email)
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------
-- 3. Przypisanie ról
-- ---------------------------------------------------------------------
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
SELECT u."Id", r."Id"
FROM (VALUES
    ('adam.kowalski@techcorp.local',    'Administrator'),
    ('anna.nowak@techcorp.local',       'Kierownik'),
    ('piotr.wisniewski@techcorp.local', 'Pracownik'),
    ('marta.zielinska@techcorp.local',  'Pracownik'),
    ('jan.kowalczyk@techcorp.local',    'Pracownik')
) AS m(email, role)
JOIN "AspNetUsers" u ON u."NormalizedEmail" = upper(m.email)
JOIN "AspNetRoles" r ON r."NormalizedName"  = upper(m.role)
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------
-- 4. Wpisy czasu pracy: dni robocze z ostatnich 10 dni, 8:00-16:00
-- ---------------------------------------------------------------------
INSERT INTO "TimeEntries"
    ("UserId", "EntryDate", "StartTime", "EndTime", "TotalHours", "Note", "CreatedAt", "ModifiedById")
SELECT u."Id",
       d::date,
       (d::date + time '08:00') AT TIME ZONE 'Europe/Warsaw',
       (d::date + time '16:00') AT TIME ZONE 'Europe/Warsaw',
       8.00,
       '',
       now(),
       NULL
FROM generate_series((CURRENT_DATE - 10)::timestamp, (CURRENT_DATE - 1)::timestamp, interval '1 day') AS d
CROSS JOIN "AspNetUsers" u
WHERE u."NormalizedEmail" IN ('ANNA.NOWAK@TECHCORP.LOCAL',
                              'PIOTR.WISNIEWSKI@TECHCORP.LOCAL',
                              'MARTA.ZIELINSKA@TECHCORP.LOCAL')
  AND EXTRACT(ISODOW FROM d) < 6
  AND NOT EXISTS (SELECT 1 FROM "TimeEntries" t
                  WHERE t."UserId" = u."Id" AND t."EntryDate" = d::date);

-- Przykład ręcznej korekty wpisu przez kierownika (ostatni wpis Marty)
UPDATE "TimeEntries" t
SET "EndTime"      = t."StartTime" + interval '7 hours 30 minutes',
    "TotalHours"   = 7.50,
    "Note"         = 'Korekta: pracownik zapomniał zakończyć pracy.',
    "ModifiedById" = (SELECT "Id" FROM "AspNetUsers" WHERE "NormalizedEmail" = 'ANNA.NOWAK@TECHCORP.LOCAL')
WHERE t."Id" = (
    SELECT t2."Id" FROM "TimeEntries" t2
    JOIN "AspNetUsers" u ON u."Id" = t2."UserId"
    WHERE u."NormalizedEmail" = 'MARTA.ZIELINSKA@TECHCORP.LOCAL'
    ORDER BY t2."EntryDate" DESC LIMIT 1
)
AND t."ModifiedById" IS NULL;

-- ---------------------------------------------------------------------
-- 5. Wnioski urlopowe (każdy status)
-- ---------------------------------------------------------------------
INSERT INTO "LeaveRequests"
    ("UserId", "LeaveType", "StartDate", "EndDate", "DaysCount",
     "Reason", "Status", "ReviewerId", "ReviewerComment",
     "RequestDate", "DecisionDate")
SELECT u."Id",
       v.leave_type,
       CURRENT_DATE + v.start_off,
       CURRENT_DATE + v.end_off,
       (SELECT COUNT(*)
        FROM generate_series(
            CURRENT_DATE + v.start_off,
            CURRENT_DATE + v.end_off,
            interval '1 day'
        ) AS day
        WHERE EXTRACT(ISODOW FROM day) < 6),
       v.reason,
       v.status,
       rv."Id",
       v.comment,
       now() - interval '5 days',
       v.decision
FROM (VALUES
    ('piotr.wisniewski@techcorp.local', 'wypoczynkowy',    14, 18, 'Urlop letni',            'Zaakceptowany', 'anna.nowak@techcorp.local', NULL::text,                                     now() - interval '1 day'),
    ('marta.zielinska@techcorp.local',  'okolicznościowy',  7,  7, 'Ślub rodzeństwa',        'Oczekujący',    NULL::text,                  NULL::text,                                     NULL::timestamptz),
    ('piotr.wisniewski@techcorp.local', 'bezpłatny',       30, 32, 'Sprawy prywatne',        'Odrzucony',     'anna.nowak@techcorp.local', 'Zbyt duże obłożenie zespołu w tym terminie.', now() - interval '2 days'),
    ('marta.zielinska@techcorp.local',  'wypoczynkowy',    21, 25, 'Wyjazd rodzinny',        'Zaakceptowany', 'anna.nowak@techcorp.local', NULL::text,                                     now() - interval '3 days')
) AS v(email, leave_type, start_off, end_off, reason, status, reviewer_email, comment, decision)
JOIN "AspNetUsers" u       ON u."NormalizedEmail"  = upper(v.email)
LEFT JOIN "AspNetUsers" rv ON rv."NormalizedEmail" = upper(v.reviewer_email)
WHERE NOT EXISTS (
    SELECT 1
    FROM "LeaveRequests" l
    WHERE l."UserId" = u."Id"
      AND l."LeaveType" = v.leave_type
      AND l."StartDate" = CURRENT_DATE + v.start_off
);

COMMIT;
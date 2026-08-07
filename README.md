# CineVision (FIT RS2 — IB200017)

Cinema booking platform:

| Part | What it is |
|------|------------|
| Backend | ASP.NET Core API + SQL Server + RabbitMQ email worker (**Docker**) |
| Desktop | Flutter Windows app — **Admin / Staff** |
| Mobile | Flutter Android app — **Customer** booking, Stripe, recommendations |

---

## Quick start (do this in order)

### 0. Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) — **running**
- [Flutter](https://docs.flutter.dev/get-started/install) (stable) — for desktop / mobile UI
- Optional: SQL Server Management Studio (SSMS) or Azure Data Studio

### 1. Create `.env`

```powershell
cd CineVision
Copy-Item .env.example .env
```

Open `CineVision/.env` and fill empty secrets (`MSSQL_SA_PASSWORD`, JWT, Stripe, SMTP, OpenAI, connection-string user/password).

Minimum so Docker can start SQL:

- `MSSQL_SA_PASSWORD` — strong password (letters, numbers, symbol)
- `DB_NAME` — already `200017` in the example

> Never commit `.env`. For seminar submission use a password-protected `.env-tajne.zip` instead.

### 2. Start the backend

```powershell
cd CineVision
.\start.ps1
```

Wait until you see **Backend is up.** First run builds images, applies migrations, and seeds data (can take a few minutes).

Check:

| Service | URL / address |
|---------|----------------|
| API | http://localhost:5126 |
| Health | http://localhost:5126/health |
| Swagger | http://localhost:5126/swagger |
| RabbitMQ UI | http://localhost:15672 |
| SQL Server | `localhost,1435` |

Stop later with:

```powershell
cd CineVision
docker compose down
```

### 3. Start desktop (Admin / Staff)

```powershell
cd CineVision\UI\cinevision_desktop
flutter pub get
flutter run -d windows
```

API URL defaults to `http://localhost:5126/`.

### 4. Start mobile (Customer)

Start an **Android emulator (AVD)**, then:

```powershell
cd CineVision\UI\cinevision_mobile
flutter pub get
flutter run
```

On the emulator the API URL defaults to `http://10.0.2.2:5126/` (host machine’s localhost).

---

## Login credentials (seed data)

All seeded app users use the same password:

### Password for every seed user: `Test123`

| App | Username | Password | Role | Email |
|-----|----------|----------|------|-------|
| **Desktop** | `admin1` | `Test123` | Admin | admin1@gmail.com |
| **Desktop** | `admin2` | `Test123` | Staff | admin2@gmail.com |
| **Desktop** | `admin3` | `Test123` | Staff | admin3@gmail.com |
| **Mobile** | `customer1` | `Test123` | Customer | customer1@gmail.com |
| **Mobile** | `customer2` | `Test123` | Customer | customer2@gmail.com |

Notes:

- Login fields use **username** (not email), e.g. `admin1` / `Test123`.
- Desktop: use **Admin** or **Staff** only.
- Mobile: use **Customer** accounts, or register a new customer in the app.

---

## Other logins & connections

### SQL Server Management Studio / Azure Data Studio

| Field | Value |
|-------|--------|
| Server | `localhost,1435` |
| Authentication | SQL Server Authentication |
| Login | `sa` |
| Password | value of `MSSQL_SA_PASSWORD` in `CineVision/.env` |
| Database | `200017` (or your `DB_NAME`) |

Also enable **Trust server certificate** if the client asks.

> Docker API uses `sa` + `MSSQL_SA_PASSWORD`. The `ConnectionStrings__DefaultConnection` in `.env` is for local `dotnet run` only — keep its password in sync with how you connect from SSMS.

### RabbitMQ management UI

| Field | Value |
|-------|--------|
| URL | http://localhost:15672 |
| Username | `guest` (or `RABBITMQ_USER` from `.env`) |
| Password | `guest` (or `RABBITMQ_PASSWORD` from `.env`) |

### Stripe (mobile payments)

Use **test** keys from your Stripe dashboard in `.env`:

- `Stripe__PublishableKey`
- `Stripe__SecretKey`

Card testing: Stripe’s test cards (e.g. `4242 4242 4242 4242`).

---

## Useful commands

```powershell
# Is API healthy?
Invoke-WebRequest http://localhost:5126/health

# Container status / logs
cd CineVision
docker compose ps
docker compose logs -f cinevision-api

# Rebuild images after code changes
.\start.ps1 -Build
```

### Release builds (for GitHub Release ZIP)

```powershell
# Desktop
cd CineVision\UI\cinevision_desktop
flutter clean
flutter build windows --release

# Mobile
cd CineVision\UI\cinevision_mobile
flutter clean
flutter build apk --release
```

Outputs:

- Desktop: `cinevision_desktop\build\windows\x64\runner\Release\`
- Mobile: `cinevision_mobile\build\app\outputs\flutter-apk\app-release.apk`

---

## Project layout

```text
RS2-CineVison/
  README.md
  recommender-dokumentacija.md
  CineVision/
    start.ps1                 # start Docker stack
    docker-compose.yml
    .env.example              # copy → .env
    CineVision.WebAPI/        # REST API
    CineVision.Services/      # EF, business logic, seed
    CineVision.Worker/        # email consumer
    CineVision.Model/
    UI/cinevision_desktop/    # Flutter Admin/Staff
    UI/cinevision_mobile/     # Flutter Customer
```

Recommender details: see [`recommender-dokumentacija.md`](recommender-dokumentacija.md).

---

## Troubleshooting

| Problem | What to try |
|---------|-------------|
| `start.ps1` fails | Docker Desktop running? `.env` present with `MSSQL_SA_PASSWORD` set? |
| API never healthy | `docker compose logs -f cinevision-api` |
| Desktop can’t login | Backend up? Use `admin1` / `Test123` |
| Mobile can’t reach API | Emulator + `10.0.2.2` (not `localhost`). API must listen on host port `5126` |
| Flutter symlink error (Windows) | `flutter clean`, delete `windows\flutter\ephemeral\.plugin_symlinks`, enable Developer Mode |
| SSMS connection fails | Server `localhost,1435` (comma, not colon). Password = `MSSQL_SA_PASSWORD` |

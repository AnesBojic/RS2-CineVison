# CineVision (FIT RS2 — IB200017)

Cinema booking platform:

| Part | Description |
|------|-------------|
| Backend | ASP.NET Core Web API + SQL Server + RabbitMQ email worker (**Docker**) |
| Desktop | Flutter Windows — **Admin / Staff** |
| Mobile | Flutter Android — **Customer** (bookings, Stripe, recommendations) |

Database name (seed / `.env`): **`200017`**

Recommender documentation: [`recommender-dokumentacija.md`](recommender-dokumentacija.md)

---

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) — **must be running**
- [Flutter](https://docs.flutter.dev/get-started/install) (stable) + Windows desktop / Android emulator (AVD)
- Optional: SSMS 
- Stripe test keys, SMTP (e.g. Gmail app password), OpenAI key — in `.env`

---

## 1. Configuration (`.env`)
 
 Unzip folder with name "env-tajne", there is .env file in it allready setted up for use.

---

## 2. Start the backend

```powershell
cd CineVision
.\start.ps1
```

Wait for **Backend is up.** The first run takes longer (image build, migrations, seed).

### Services

| Service | Address |
|---------|---------|
| API | http://localhost:5126 |
| Health | http://localhost:5126/health |
| Swagger | http://localhost:5126/swagger |
| RabbitMQ UI | http://localhost:15672 |
| SQL Server | `localhost,1435` |
| Email worker | `cinevision-worker` (no public HTTP port) |

Stop:

```powershell
cd CineVision
docker compose down
```

Rebuild after backend code changes:

```powershell
.\start.ps1 -Build
```

---

## 3. Login credentials (seed)

All seeded users share password **`Test123`**.  
Login uses **username** (not email), lowercase.

| App | Username | Password | Role |
|-----|----------|----------|------|
| Desktop | `admin1` | `Test123` | Admin |
| Desktop | `admin2` | `Test123` | Staff |
| Desktop | `admin3` | `Test123` | Staff |
| Mobile | `customer1` | `Test123` | Customer |
| Mobile | `customer2` | `Test123` | Customer |



- Desktop → Admin / Staff  
- Mobile → Customer (or register a new account)

---

## 4. Other connections

### SQL Server Management Studio / Azure Data Studio

| Field | Value |
|-------|--------|
| Server | `localhost,1435` (comma, not colon) |
| Auth | SQL Server Authentication |
| Login | `sa` |
| Password | `MSSQL_SA_PASSWORD` from `.env` |
| Database | `200017` |
| Trust server certificate | Yes |

### RabbitMQ

| Field | Value |
|-------|--------|
| URL | http://localhost:15672 |
| User / pass | `guest` / `guest` (or from `.env`) |

### Stripe (mobile)

Put test keys in `.env`. Test card e.g. `4242 4242 4242 4242`.

---

## 5. Desktop (Windows)

API must be running. Default URL: **`http://localhost:5126/`**

### Dev

```powershell
cd CineVision\UI\cinevision_desktop
flutter pub get
flutter run -d windows --dart-define=API_BASE_URL=http://localhost:5126/
```

### Release build (submission 9.2.2)

```powershell
cd CineVision\UI\cinevision_desktop
flutter clean
flutter pub get
flutter build windows --release --dart-define=API_BASE_URL=http://localhost:5126/
```

Output:

```text
CineVision\UI\cinevision_desktop\build\windows\x64\runner\Release\
```

Run `cinevision_desktop.exe` from that folder. Login: `admin1` / `Test123`.

---

## 6. Mobile (Android emulator / AVD)

API must be running. On the emulator the URL is **`http://10.0.2.2:5126/`** (host machine localhost).  
**Do not use `localhost` on the Android emulator.**

### Dev

1. Start an AVD (Android Studio → Device Manager → ▶️)  
2:

```powershell
cd CineVision\UI\cinevision_mobile
flutter pub get
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5126/
```

Login: `customer1` / `Test123`.

### Release APK (submission 9.2.1)

```powershell
cd CineVision\UI\cinevision_mobile
flutter clean
flutter pub get
flutter build apk --release --dart-define=API_BASE_URL=http://10.0.2.2:5126/
```

Output (install the **`.apk`**, not the `.sha1`):

```text
CineVision\UI\cinevision_mobile\build\app\outputs\flutter-apk\app-release.apk
```

### Test the APK in AVD

1. Uninstall the old CineVision app (App info → Uninstall), or:  
   `adb uninstall com.example.cinevision_mobile`
2. Drag `app-release.apk` onto the emulator window, or:  
   `adb install -r "....\app-release.apk"`
3. Open the app and sign in

> The first `assembleRelease` can take 5–15+ minutes — that is normal.

---

## 8. Useful commands

```powershell
# Health
Invoke-WebRequest http://localhost:5126/health

# Containers / logs
cd CineVision
docker compose ps
docker compose logs -f cinevision-sql
docker compose logs -f cinevision-api
```

---

## 9. Troubleshooting

| Problem | Fix |
|---------|-----|
| `cinevision-sql` unhealthy / *Login failed for user 'sa'* | `MSSQL_SA_PASSWORD` in `.env` is empty or does not match the existing Docker volume. Set the password, or run `docker compose down -v` then `.\start.ps1` (wipes the local DB). |
| Desktop: *connection refused* on `localhost:5126` | API is not up — fix SQL/backend first. |
| Mobile cannot reach API | Use `10.0.2.2`, not `localhost`. Use an AVD emulator. |
| Login fails | Username lowercase: `admin1` / `customer1`, password `Test123`. |
| Flutter symlink error (Windows) | Delete `windows\flutter\ephemeral\.plugin_symlinks`, run `flutter clean`, enable Developer Mode. |
| SSMS cannot connect | Server `localhost,1435`, password = `MSSQL_SA_PASSWORD`. |

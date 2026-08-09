# CineVision (FIT RS2 — IB200017)

Cinema booking platform: **ASP.NET Core API** (Docker) + **Flutter desktop** (Admin/Staff) + **Flutter mobile** (Customer).

Database: **`200017`** · Recommender docs: [`recommender-dokumentacija.md`](recommender-dokumentacija.md)

---

## 1. Backend setup

**Prerequisites:** Docker Desktop running, Flutter (optional if you only use prebuilt UI).

```powershell
cd CineVision
Copy-Item .env.example .env
# Fill MSSQL_SA_PASSWORD (required), JWT, Stripe, SMTP, OpenAI
.\start.ps1
```

Wait for **Backend is up.**

| Service | Address |
|---------|---------|
| API / Health / Swagger | http://localhost:5126 · `/health` · `/swagger` |
| RabbitMQ UI | http://localhost:15672 (`guest` / `guest`) |
| SQL Server | `localhost,1435` — user `sa`, password = `MSSQL_SA_PASSWORD` from `.env`, database `200017` |

Stop: `docker compose down`  
If SQL is *unhealthy* / `sa` login fails: set `MSSQL_SA_PASSWORD`, or `docker compose down -v` then `.\start.ps1` (wipes local DB).

> Do not commit `.env`. For submission use a password-protected `.env-tajne.zip` (password goes on the DL system, not in the GitHub Release).

---

## 2. Seed logins

Password for all seed users: **`Test123`** (username, lowercase).

| App | Username | Role |
|-----|----------|------|
| Desktop | `admin1` | Admin |
| Desktop | `admin2`, `admin3` | Staff |
| Mobile | `customer1`, `customer2` | Customer |

---

## 3. Desktop (Windows)

Prebuilt files are already in **`Buildovi/Release/`** — you can use the app right away (backend must be running):

1. Open `Buildovi\Release\`
2. Run `cinevision_desktop.exe`
3. Sign in with `admin1` / `Test123`  
   API: `http://localhost:5126/`

### Optional — run from terminal (dev)

```powershell
cd CineVision\UI\cinevision_desktop
flutter pub get
flutter run -d windows --dart-define=API_BASE_URL=http://localhost:5126/
```

Project path: `CineVision/UI/cinevision_desktop`

---

## 4. Mobile (Android emulator / AVD)

Prebuilt APK is already in **`Buildovi/app-release.apk`** — you can use the app right away (backend must be running).  
On the emulator the API host is **`10.0.2.2`** (not `localhost`).

1. Start an AVD (Android Studio → Device Manager)
2. Uninstall any old CineVision app on the emulator
3. Drag `Buildovi\app-release.apk` onto the emulator (or `adb install -r Buildovi\app-release.apk`)
4. Sign in with `customer1` / `Test123`

### Optional — run from terminal (dev)

```powershell
cd CineVision\UI\cinevision_mobile
flutter pub get
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5126/
```

Project path: `CineVision/UI/cinevision_mobile`

---

## 5. Submission notes (RS2 §9.2)

- Put builds in a **GitHub Release** ZIP (do not commit binaries into git history). Suggested name: `fit-build-20gg-mm-dd.zip`
- ZIP should contain the mobile APK and the Windows `Release` folder (same artifacts as in `Buildovi/`)
- Use an **immutable** release: Draft → add ZIP → verify → Publish
- Do **not** put `.env` or other secrets in the Release
- On the DL system: link to the **exact release tag** (not `/releases/latest`) + password for the `.env` ZIP
- Prefer **HTTP** for the API (avoid self-signed HTTPS)

---

## 6. Project layout

```text
RS2-CineVison/
  Buildovi/                     # prebuilt UI (APK + Windows Release)
  README.md
  recommender-dokumentacija.md
  CineVision/
    start.ps1 · docker-compose.yml · .env.example
    CineVision.WebAPI/ · Services/ · Worker/ · Model/
    UI/cinevision_desktop/ · UI/cinevision_mobile/
```

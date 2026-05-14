# Mesa Mohloane — Development README

Short developer-oriented README for local development and test accounts.

## Project
- Frontend: Razor Views (MVC-style) / Razor Pages in `Mesa-Mohloane_Frontend`
- Backend: ASP.NET Core Web API in `Mesa-Mohloane_Backend`
- Target framework: .NET 10

## Quick start (local)
1. From solution root run:
   - `dotnet build` (build all projects)
   - `dotnet run --project Mesa-Mohloane_Backend` (start backend)
   - `dotnet run --project Mesa-Mohloane_Frontend` (start frontend)
2. Open the frontend login page: `/Auth/Login` (or `https://localhost:<port>/Auth/Login` depending on launch settings).

## Test accounts (development only)
Use these accounts only for local development and testing. Do NOT use them in production.

| Role      | Email                      | Password       |
|-----------|----------------------------|----------------|
| Admin     | `mesa_admin@gmail.com`     | `Admin@12345`  |
| Citizen   | `joel@email.com`           | `Joel@123`     |
| Contractor| `lebelo@gmail.com`         | `Lebelo@123`   |
| Inspector | `poloko@gmail.com`         | `Poloko@123`   |

Notes:
- The login endpoint is `GET/POST /Auth/Login` (see `Views/Auth/Login.cshtml` and `AuthController`).
- If the project seeds users, these credentials may already exist; otherwise create test users via the UI or backend seeder.

## Security / housekeeping
- These passwords are included for convenience during local development only. Change default passwords immediately for any deployed environment.
- Do not commit real credentials, secrets, or production keys to source control. Use environment variables or a secrets store.
- Rotate or remove these test accounts before sharing or deploying the application.

## Troubleshooting
- If authentication fails, confirm the backend is running and check `appsettings.Development.json` for the API base URL used by the frontend.
- Check browser console/network for failing API calls and backend logs in `Mesa-Mohloane_Backend/logs`.



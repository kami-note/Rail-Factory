# Identity and Access Management (IAM)

Configuration for **Development** and **Deploy**:

## Accessing the API when running with Aspire

1. Start the stack: `dotnet run --project RailFactory.AppHost` (from `src/`). The Aspire dashboard usually opens in the browser (e.g. `https://localhost:17289`).
2. In the dashboard, open the **identity-access-management** resource. Use the **Endpoint** URL shown there (e.g. `https://localhost:XXXX` or `http://localhost:YYYY`) as the base URL of the IAM API.
3. **Google login** – send a POST request to the IAM base URL:

   ```http
   POST {baseUrl}/api/auth/google
   Content-Type: application/json

   { "idToken": "GOOGLE_ID_TOKEN_HERE" }
   ```

   The client must obtain the Google ID token (e.g. via Google Sign-In in a web or mobile app) and send it in the body. The API returns the created/updated user (or 401/403 on failure).

4. **Swagger** (Development only): open `{baseUrl}/swagger` in the browser to try the endpoints from the UI.

**Redirect flow (browser):**

- **GET /auth/google** – Redirects the user to Google sign-in. Requires `Google:RedirectUri` and `Google:FrontendRedirectUri` in config.
- **GET /auth/google/callback** – Google redirects here with `?code=...&state=...`. The API exchanges the code for an ID token, registers/updates the user, then redirects to `FrontendRedirectUri` with `#id_token=...` so the frontend can store it.

Configure in Google Cloud Console: add `RedirectUri` (e.g. `https://your-api/auth/google/callback`) to the OAuth client’s authorized redirect URIs.

Other endpoints: `GET /api/users/me` (Bearer token), `PUT /api/users/{id}/profile`, `POST /api/users/{id}/expel`.

## Development

- **Run via AppHost** (`dotnet run --project RailFactory.AppHost`): the connection string for `iamdb` is injected by Aspire. Set `Google:ClientId` in the IAM API project (user secrets or `appsettings.Development.json`).
- **Run IAM API standalone**: set both `ConnectionStrings__iamdb` and `Google__ClientId` (env vars or user secrets). See `RailFactory.Iam.Api/env.example`.

User secrets (recommended for local values):

```bash
cd src/RailFactory.Iam/RailFactory.Iam.Api
dotnet user-secrets set "Google:ClientId" "your_client_id.apps.googleusercontent.com"
# If running standalone:
dotnet user-secrets set "ConnectionStrings:iamdb" "Host=localhost;Port=5432;Database=iamdb;Username=postgres;Password=..."
```

## Deploy (Production)

Set environment variables; do not rely on appsettings for secrets:

- `ConnectionStrings__iamdb` – PostgreSQL connection string.
- `Google__ClientId` – Google OAuth Client ID for ID token validation.

Optional: `ASPNETCORE_ENVIRONMENT=Production` (default in most hosts). Migrations run on startup; ensure the database is reachable.

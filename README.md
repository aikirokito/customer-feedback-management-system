# Customer Feedback Management System

CFMS is a role-based customer feedback and support-ticket platform built with ASP.NET Core 9, Entity Framework Core, PostgreSQL, React 19, and Vite.

## Feature coverage

- Customer registration, email/password login, Google sign-in, JWT rotation, logout, and password changes.
- Customer profile management and automatic access-token refresh.
- Feedback submission with database-managed categories and up to three validated attachments.
- Customer feedback history with filters, pagination, ticket deletion while new, visible discussion threads, and post-resolution ratings.
- Support workflow with assignment, reassignment, unassignment, priorities, guarded status transitions, official responses, internal notes, attachments, and full status/assignment history.
- Threaded customer comments with author/admin edit and delete controls.
- In-app notifications with unread counts, mark-read actions, ticket links, polling, and SignalR server delivery support.
- System administration for users, roles, account status, departments, feedback categories, and audit logs.
- Department-scoped manager access and staff assignment rules.
- Summary, category, priority, status, monthly-trend, resolution-time, satisfaction, and staff-performance reports.
- Soft deletion, token revocation, audit trails, FluentValidation, centralized error responses, authorization checks, and cross-ticket resource scoping.

## Prerequisites

- .NET SDK 9
- Node.js compatible with Vite 8
- PostgreSQL 14+ or a Supabase PostgreSQL project
- Optional: Supabase Storage for feedback attachments
- Optional: Google OAuth web client for Google sign-in

## Backend setup

Configure secrets with environment variables or .NET user secrets. Do not commit real secrets.

```powershell
cd BE/src/CFMS.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=cfms;Username=postgres;Password=YOUR_PASSWORD"
dotnet user-secrets set "Jwt:Secret" "YOUR_RANDOM_SECRET_WITH_AT_LEAST_32_CHARACTERS"
dotnet user-secrets set "SupabaseStorage:Url" "https://YOUR_PROJECT.supabase.co"
dotnet user-secrets set "SupabaseStorage:SecretKey" "YOUR_SB_SECRET_KEY"
dotnet user-secrets set "GoogleAuth:ClientId" "YOUR_GOOGLE_CLIENT_ID"
```

Apply the database migrations and start the API:

```powershell
cd BE
dotnet ef database update --project src/CFMS.Infrastructure --startup-project src/CFMS.Api
dotnet run --project src/CFMS.Api
```

The development Swagger UI is served from the API root. The default frontend expects the API at `http://localhost:5000/api`; adjust the launch profile or frontend environment variable if necessary.

To create the four local test roles after applying migrations, run [SeedTestData.sql](BE/SeedTestData.sql). All seeded accounts use `Password123!`:

| Role | Account |
|---|---|
| Customer | `customer1@test.com` |
| Support staff | `staff1@test.com` |
| Department manager | `manager1@test.com` |
| System administrator | `admin1@test.com` |

## Frontend setup

Copy `FE/.env.example` to `FE/.env` and configure the optional Google client ID:

```powershell
cd FE
Copy-Item .env.example .env
npm install
npm run dev
```

The frontend runs at `http://localhost:5173` by default.

## Verification

```powershell
cd BE
dotnet test CFMS.sln --configuration Release
dotnet build CFMS.sln --configuration Release

cd ../FE
npm run lint
npm run build
```

Automated tests cover authentication, JWTs, categories, departments, feedback status/rating rules, assignment, comments, notifications, user deletion, reports, and audit logging.

## Deployment notes

- Replace every placeholder in `appsettings.json` through environment-specific configuration.
- Keep the Supabase `sb_secret_...` key on the backend only; the legacy `service_role` key remains supported for existing deployments.
- Create a public Supabase Storage bucket named by `SupabaseStorage:BucketName` (the default is `cfms-attachments`).
- Add deployed frontend origins to `Cors:AllowedOrigins`.
- Configure the same Google web client ID in `GoogleAuth:ClientId` and `VITE_GOOGLE_CLIENT_ID`.
- Apply migrations before starting a new API deployment.

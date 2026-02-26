# ⚡ CodeArena

A **blazing-fast, self-hosted code execution sandbox** — write code in any languages, run it in isolated Docker containers, save snippets, and push them directly to GitHub.

## Features

- 🔐 **GitHub OAuth** login with secure HttpOnly JWT cookie session
- 🐳 **Docker-isolated** sandboxed execution (no network, resource-limited)
- ⚡ **5 languages**: Python 3, C# (.NET 8), JavaScript (Node.js), C (GCC), C++ 17
- 📝 **Snippet CRUD** with tags and unique slugs per user
- ✅ **Test cases** — run against multiple stdin/stdout pairs
- 🔴 **Live output streaming** via SignalR WebSockets
- 🐙 **Push to GitHub** — commits your snippet files to any of your repos
- 📊 Swagger UI included in Development mode

---

## Tech Stack

| Layer             | Technology                                                            |
| ----------------- | --------------------------------------------------------------------- |
| **API**           | ASP.NET Core 8, EF Core 8, Npgsql, SignalR, Serilog, FluentValidation |
| **Runner**        | .NET 8 Worker, StackExchange.Redis, Docker CLI (sibling containers)   |
| **Frontend**      | React 18, TypeScript, Vite, Monaco Editor, Zustand, Axios, SignalR JS |
| **Database**      | PostgreSQL 16                                                         |
| **Cache / Queue** | Redis 7                                                               |
| **Proxy**         | Caddy (prod) / Nginx (alt)                                            |

---

## Prerequisites

| Tool                  | Min Version                 | Notes                                                      |
| --------------------- | --------------------------- | ---------------------------------------------------------- |
| **Docker Desktop**    | 24+                         | Must be running. Enables Docker-in-Docker execution.       |
| **Docker Compose** v2 | bundled with Docker Desktop | Used to start all services                                 |
| **WSL 2**             | —                           | Required on Windows for Linux containers                   |
| **Git**               | any                         | To clone the repo                                          |
| **.NET 8 SDK**        | 8.0                         | Only needed to run tests or develop locally without Docker |
| **Node.js**           | 20 LTS                      | Only needed for local frontend dev without Docker          |

---

## Quick Start (Docker — recommended)

### 1. Clone the repo

```bash
git clone https://github.com/<your-org>/codearena.git
cd codearena
```

### 2. Create a GitHub OAuth App

1. Go to **GitHub → Settings → Developer settings → OAuth Apps → New OAuth App**
2. Set:
   - **Homepage URL**: `http://localhost:5000`
   - **Authorization callback URL**: `http://localhost:5000/api/auth/callback`
3. Note the **Client ID** and generate a **Client Secret**

### 3. Configure environment variables

```bash
cd infra
cp .env.example .env
```

Edit `.env` and fill in **all** values:

```dotenv
POSTGRES_PASSWORD=<strong-password>
REDIS_PASSWORD=<strong-redis-password>
JWT_KEY=<at-least-32-character-random-string>
GITHUB_CLIENT_ID=<your-oauth-app-client-id>
GITHUB_CLIENT_SECRET=<your-oauth-app-client-secret>
RUNNER_TOKEN=<random-shared-secret-between-runner-and-api>
```

> **Tip:** Generate secrets with `openssl rand -base64 32`

### 4. Build the language sandbox images

**On Windows (PowerShell):**

```powershell
cd scripts
.\build-runner-images.ps1
```

**On Linux / macOS / WSL:**

```bash
cd scripts
bash build-runner-images.sh
```

**On Windows (PowerShell):**

```powershell
cd scripts
.\build-runner-images.ps1
```

This builds 4 Docker images:

- `codearena-runner-python:latest`
- `codearena-runner-node:latest`
- `codearena-runner-csharp:latest`
- `codearena-runner-c-cpp:latest`

### 5. Start the full stack

```bash
cd infra
docker compose up --build
```

| Service     | URL                           |
| ----------- | ----------------------------- |
| **Web UI**  | http://localhost:5173         |
| **API**     | http://localhost:5000         |
| **Swagger** | http://localhost:5000/swagger |
| **Health**  | http://localhost:5000/health  |

---

## Local Development (without Docker)

### API

```bash
cd apps/api
# Fill in appsettings.Development.json with a local Postgres + Redis
dotnet restore
dotnet run
```

The API runs on `http://localhost:5000`.

### Web Frontend

```bash
cd apps/web
npm install
npm run dev
```

The dev server runs on `http://localhost:5173` and proxies `/api` and `/hubs` to `http://localhost:5000` (configured in `vite.config.ts`).

### Runner

The runner requires Docker access and a running Redis. In development, the worker won't start until `RUNNER_TOKEN` and `ConnectionStrings__Redis` are set. Set them in `appsettings.Development.json` or as environment variables:

```bash
cd apps/runner
ConnectionStrings__Redis="localhost:6379,password=<pw>" \
Api__HubUrl="http://localhost:5000/hubs/execution" \
Api__RunnerToken="<RUNNER_TOKEN>" \
dotnet run
```

---

## Running Tests

```bash
cd apps/api/Tests
dotnet test
```

Tests use **xunit + Moq + EF InMemory** — no live database or Redis required.

---

## Project Structure

```
codearena/
├── apps/
│   ├── api/                  # ASP.NET Core 8 Web API
│   │   ├── Controllers/      # Auth, Snippets, Execution, GitHub endpoints
│   │   ├── Data/             # AppDbContext + EF migrations (SQL)
│   │   ├── DTOs/             # Request/response records + FluentValidation
│   │   ├── Hubs/             # SignalR ExecutionHub for live output
│   │   ├── Middleware/       # Global exception handler
│   │   ├── Models/           # EF entity classes
│   │   ├── Services/         # Business logic (Auth, Snippet, Execution, GitHub)
│   │   └── Tests/            # xunit unit tests
│   ├── runner/               # .NET Worker Service — dequeues jobs, runs Docker containers
│   │   ├── DockerExecutor.cs # Spawns sandbox containers with strict resource limits
│   │   ├── LanguageRegistry.cs # Maps language names → Docker images + commands
│   │   ├── Worker.cs         # Redis queue consumer, SignalR result reporting
│   │   └── SandboxOptions.cs # Configurable timeout, memory, output limits
│   └── web/                  # React + TypeScript SPA
│       └── src/
│           ├── api/          # Typed Axios wrappers (client, snippets, execution, github)
│           ├── components/   # Monaco editor, output panel, test results
│           ├── hooks/        # useExecution (poll/run), useSignalR (live stream)
│           ├── pages/        # LoginPage, SnippetsPage, EditorPage
│           ├── store/        # Zustand editor state
│           └── types/        # Shared TypeScript interfaces
├── infra/
│   ├── docker-compose.yml    # Development stack
│   ├── docker-compose.prod.yml # Production (with Caddy reverse proxy)
│   ├── .env.example          # Template — copy to .env and fill secrets
│   └── runner-images/        # Dockerfiles for per-language sandbox images
└── scripts/
    ├── build-runner-images.sh  # Linux / macOS builder
    └── build-runner-images.ps1 # Windows PowerShell builder
```

---

## Security Notes

- JWT is stored in an **HttpOnly, Secure, SameSite=Strict** cookie (`ca_jwt`) — never in localStorage
- GitHub OAuth tokens are encrypted at rest using ASP.NET Core Data Protection
- Runner sandbox containers run with: `--network=none`, `--read-only`, `--cap-drop=ALL`, `--security-opt no-new-privileges`, `--pids-limit=100`, memory and CPU quotas
- CSRF is mitigated by SameSite=Strict cookies + CORS allowlist
- Open-redirect protection in OAuth state parameter

---

## Production Deployment

```bash
cd infra
docker compose -f docker-compose.prod.yml up --build -d
```

The production compose file uses Caddy as a TLS-terminating reverse proxy. Edit `infra/caddy/Caddyfile` with your domain.

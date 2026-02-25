# Deployment Guide

## Prerequisites

- A VPS with Docker & Docker Compose installed (Ubuntu 22.04+)
- A domain pointing to your VPS IP
- GitHub OAuth App created at https://github.com/settings/developers

## Step 1: Clone & Configure

```bash
git clone https://github.com/yourorg/codearena.git
cd codearena

cp .env.example .env
# Edit .env:
#   GITHUB_CLIENT_ID=your_client_id
#   GITHUB_CLIENT_SECRET=your_client_secret
#   JWT_KEY=$(openssl rand -base64 32)
#   RUNNER_TOKEN=$(openssl rand -hex 32)
#   REDIS_PASSWORD=$(openssl rand -hex 16)
```

## Step 2: Build Runner Sandbox Images

```bash
bash scripts/build-runner-images.sh
```

## Step 3: Build Production Images

```bash
docker compose -f infra/docker-compose.yml -f infra/docker-compose.prod.yml build
```

## Step 4: Configure Caddy

Edit `infra/caddy/Caddyfile` — replace `yourdomain.com` and email.

Update `GitHub__RedirectUri` in your .env:

```
GitHub__RedirectUri=https://yourdomain.com/api/auth/callback
```

Update your GitHub OAuth App callback URL to match.

## Step 5: Deploy

```bash
cd infra
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

## Step 6: Verify

```bash
# Health check
curl https://yourdomain.com/health

# View logs
docker compose logs -f api
docker compose logs -f runner
```

## Local Development

```bash
# 1. Start dependencies
docker compose -f infra/docker-compose.yml up postgres redis -d

# 2. Start API
cd apps/api
dotnet run

# 3. Start Runner (separate terminal)
cd apps/runner
dotnet run

# 4. Start Web
cd apps/web
npm install && npm run dev

# 5. Build runner images (needed for actual code execution)
bash scripts/build-runner-images.sh
```

## Running Tests

```bash
# API unit tests
dotnet test apps/api/Tests/CodeArena.Api.Tests.csproj

# Web lint
cd apps/web && npm run lint
```

## Environment Variables Reference

| Variable               | Description                            |
| ---------------------- | -------------------------------------- |
| `GITHUB_CLIENT_ID`     | GitHub OAuth App client ID             |
| `GITHUB_CLIENT_SECRET` | GitHub OAuth App client secret         |
| `JWT_KEY`              | 32+ byte random secret for JWT signing |
| `RUNNER_TOKEN`         | Internal token for Runner ↔ Hub auth   |
| `REDIS_PASSWORD`       | Redis auth password (prod only)        |

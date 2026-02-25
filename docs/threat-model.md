# CodeArena Threat Model

## Assets

- User GitHub OAuth tokens (encrypted at rest with ASP.NET Data Protection)
- User code snippets (stored in PostgreSQL, owned by user)
- Execution infrastructure (Docker, Redis queue)
- API JWT signing key

## Trust Boundaries

1. **Frontend ↔ API**: HTTPS only in prod. JWT Bearer for all authenticated endpoints.
2. **API ↔ Runner**: Internal network (docker-compose). Runner has no direct DB access.
3. **Runner ↔ Docker**: Runner uses Docker socket to spawn containers. CRITICAL boundary.
4. **Sandbox containers ↔ host**: Isolated via docker flags; no network, no capabilities.

## Threats & Mitigations

| Threat                               | Impact   | Mitigation                                                                                                                 |
| ------------------------------------ | -------- | -------------------------------------------------------------------------------------------------------------------------- |
| Code injection / escape from sandbox | Critical | `--network=none`, `--read-only`, `--cap-drop=ALL`, `--no-new-privileges`, `--pids-limit`, `--memory-swap`, tmpfs workspace |
| Fork bomb                            | High     | `--pids-limit=100`                                                                                                         |
| Infinite loop / CPU exhaustion       | High     | 2s timeout via Go process timeout + SIGKILL, CPU quota via `--cpu-quota`                                                   |
| Memory exhaustion                    | High     | `--memory=256m --memory-swap=256m` (swap disabled)                                                                         |
| Path traversal in workspace          | Medium   | Workspace per-job UUID, bind-mount source read-only                                                                        |
| Docker socket exposure               | Critical | Runner container is privileged but not exposed externally; deploy runner on a separate node ideally                        |
| GitHub token theft                   | High     | Tokens encrypted with ASP.NET DataProtection (AES-256), keys stored in mounted volume with restricted permissions          |
| CSRF on OAuth callback               | Medium   | `state` parameter validated against HttpOnly cookie                                                                        |
| JWT tampering                        | Medium   | HMAC-SHA256 signed, server-side validation                                                                                 |
| Redis queue poisoning                | Medium   | Redis is not exposed publicly; internal only                                                                               |
| Output data leak between users       | Low      | Per-job Redis keys with 5-min TTL; users can only query their own job IDs                                                  |
| Dependency confusion / supply chain  | Medium   | Lock files committed; use `dotnet restore --locked-mode` in CI                                                             |

## Recommended Hardening for Production

1. Run Runner service on a dedicated VM, not the same host as the API.
2. Use Docker socket proxy (like Tecnativa/docker-socket-proxy) to restrict which Docker commands Runner can invoke.
3. Use gVisor (`--runtime=runsc`) or Kata Containers as the Docker runtime for sandboxed execution.
4. Enable AppArmor or seccomp profile on runner containers.
5. Rotate JWT key and Data Protection keys regularly.
6. Rate-limit `/api/execution` endpoints (e.g., 10 req/min per user).
7. Set `AllowedOrigins` to your specific production domain only.
8. Store `GITHUB_CLIENT_SECRET` and `JWT_KEY` in a secrets manager (Vault, AWS SSM), never in env files.

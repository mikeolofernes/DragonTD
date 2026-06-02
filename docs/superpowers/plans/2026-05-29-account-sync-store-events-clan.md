# Account Sync, Store Validation, Events, and Clan Backend Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the next Dragon Dominion backend pass: device authentication, authenticated progression sync, battle reward sync, server-side IAP receipt validation, event challenge endpoints, clan shell endpoints, and regression coverage.

**Architecture:** Keep the current single `DragonTD.API` backend project instead of creating the planned Domain/Infrastructure split. Add focused models/services/controllers inside the API project, with JWT bearer auth on new `/api/v1/*` routes and raw JSON progression storage for Unity client compatibility.

**Tech Stack:** ASP.NET Core `net8.0`, EF Core, Npgsql for runtime, EF InMemory for tests, xUnit, Unity C# client HTTP services.

---

### Task 1: Backend Test Harness

**Files:**
- Create: `Backend/DragonTD.Tests/DragonTD.Tests.csproj`
- Create: `Backend/DragonTD.Tests/TestApiFactory.cs`
- Modify: `Backend/DragonTD.sln`

- [ ] Add xUnit, ASP.NET Core testing, and EF InMemory packages.
- [ ] Add a test host that replaces Postgres with an in-memory database and configures a deterministic JWT secret.
- [ ] Add the test project to the solution.

### Task 2: Device Auth

**Files:**
- Create: `Backend/DragonTD.Tests/AuthControllerTests.cs`
- Create: `Backend/DragonTD.API/Controllers/AuthController.cs`
- Create: `Backend/DragonTD.API/Services/JwtTokenService.cs`
- Modify: `Backend/DragonTD.API/Models/Player.cs`
- Modify: `Backend/DragonTD.API/Program.cs`

- [ ] Write failing tests for `POST /api/v1/auth/device` creating and reusing a player.
- [ ] Implement device auth response envelope matching Unity `AuthResponseDto`.
- [ ] Configure JWT bearer authentication and protect new non-auth `/api/v1/*` routes.

### Task 3: Progression Sync

**Files:**
- Create: `Backend/DragonTD.Tests/ProgressionControllerTests.cs`
- Create: `Backend/DragonTD.API/Controllers/ProgressionController.cs`
- Create: `Backend/DragonTD.API/Models/PlayerProgressionState.cs`
- Modify: `Backend/DragonTD.API/Data/AppDbContext.cs`

- [ ] Write failing tests for authenticated `PUT /api/v1/progression`, `GET /api/v1/progression`, and `POST /api/v1/progression/battle-rewards`.
- [ ] Store raw Unity save JSON and return it raw so existing `JsonUtility` loading works.
- [ ] Persist battle reward JSON separately and update player last login/sync timestamps.

### Task 4: Server IAP Validation

**Files:**
- Create: `Backend/DragonTD.Tests/IapControllerTests.cs`
- Create: `Backend/DragonTD.API/Controllers/IapController.cs`
- Create: `Backend/DragonTD.API/Models/IapPurchaseReceipt.cs`
- Modify: `Backend/DragonTD.API/Data/AppDbContext.cs`
- Modify: `Unity/Assets/Scripts/Core/IapPurchaseServices.cs`
- Modify: `Unity/Assets/Scripts/Core/PlayerInventory.cs`

- [ ] Write failing tests for accepted editor mock receipts, duplicate transaction idempotency, bad product rejection, and authenticated Gem grants.
- [ ] Implement `/api/v1/iap/validate` with server catalog validation.
- [ ] Update Unity API validator to attach bearer token and use base URL endpoint construction.

### Task 5: Events and Clan Shell

**Files:**
- Create: `Backend/DragonTD.Tests/EventsAndClanControllerTests.cs`
- Create: `Backend/DragonTD.API/Controllers/EventsController.cs`
- Create: `Backend/DragonTD.API/Controllers/ClanController.cs`
- Create: `Backend/DragonTD.API/Models/EventChallengeState.cs`
- Create: `Backend/DragonTD.API/Models/ClanModels.cs`
- Modify: `Backend/DragonTD.API/Data/AppDbContext.cs`

- [ ] Write failing tests for event list, daily claim persistence, challenge score submission, and locked clan shell response.
- [ ] Implement events with daily reset semantics and simple score tracking.
- [ ] Implement clan shell endpoints that are authenticated and explicit about locked/unavailable state.

### Task 6: Verification

**Files:**
- Modify: `docs/2026-05-27-dragon-dominion-prototype-handoff.md`

- [ ] Run `dotnet test Backend/DragonTD.sln`.
- [ ] Run `dotnet build Backend/DragonTD.sln`.
- [ ] Update the handoff with implemented backend endpoints and remaining Unity IAP package installation note.

# Mbarie Intelligence Console – Setup Guide

This document explains how to configure the application for local development, how to supply secrets, and how to troubleshoot common configuration issues.

---

## 1. Environment Variables for Development

The desktop app (`MIC.Desktop.Avalonia`) loads configuration in this order:

1. `appsettings.json`
2. `appsettings.{DOTNET_ENVIRONMENT}.json` (e.g. `appsettings.Development.json`)
3. Environment variables with the `MIC_` prefix

### 1.1 Required Environment Variables

Set these for local development (values are examples):

- `MIC_AI__OpenAI__ApiKey`
  - Example: `sk-proj-xxxxxxxxxxxxxxxxxxxxxxxx`
- `MIC_ConnectionStrings__MicDatabase`
  - Example: `Host=localhost;Port=5432;Database=micdb;Username=mic;Password=local-dev-password;SSL Mode=Disable;Trust Server Certificate=true`
- `MIC_AI__AzureOpenAI__Endpoint` (only if using Azure OpenAI)
  - Example: `https://your-resource-name.openai.azure.com/`

### 1.2 How to Set Environment Variables

**Windows (PowerShell)**

```powershell
$env:MIC_AI__OpenAI__ApiKey = "sk-proj-..."
$env:MIC_ConnectionStrings__MicDatabase = "Host=localhost;Port=5432;Database=micdb;Username=mic;Password=local-dev-password;SSL Mode=Disable;Trust Server Certificate=true"
$env:MIC_AI__AzureOpenAI__Endpoint = "https://your-resource-name.openai.azure.com/"  # optional
```

**Windows (cmd.exe)**

```cmd
set MIC_AI__OpenAI__ApiKey=sk-proj-...
set MIC_ConnectionStrings__MicDatabase=Host=localhost;Port=5432;Database=micdb;Username=mic;Password=local-dev-password;SSL Mode=Disable;Trust Server Certificate=true
set MIC_AI__AzureOpenAI__Endpoint=https://your-resource-name.openai.azure.com/
```

**macOS / Linux (bash / zsh)**

```bash
export MIC_AI__OpenAI__ApiKey="sk-proj-..."
export MIC_ConnectionStrings__MicDatabase="Host=localhost;Port=5432;Database=micdb;Username=mic;Password=local-dev-password;SSL Mode=Disable;Trust Server Certificate=true"
export MIC_AI__AzureOpenAI__Endpoint="https://your-resource-name.openai.azure.com/"
```

You can also create a `.env` file for your shell and `source` it locally. `.env` files are ignored by git; use `.env.example` as a template.

---

## 2. User Secrets (Alternative for ASP.NET / CLI Hosts)

`MIC.Desktop.Avalonia` currently relies on environment variables and `appsettings.{Environment}.json`. If you introduce a web/CLI host where .NET User Secrets make sense, you can use them instead of env vars on your dev machine.

### 2.1 Enabling User Secrets

From the project directory of the host application (for example, a web or console project):

```bash
# One-time initialization for the project
cd path/to/YourHostProject
 dotnet user-secrets init
```

This adds a `UserSecretsId` to the project file. In that host, you can then call:

```csharp
// In Program.cs for that host (not the Avalonia desktop app)
configurationBuilder.AddUserSecrets<Program>(optional: true);
```

### 2.2 Setting Secrets

Once initialized:

```bash
# OpenAI key
 dotnet user-secrets set "AI:OpenAI:ApiKey" "sk-proj-..."

# Postgres connection string
 dotnet user-secrets set "ConnectionStrings:MicDatabase" "Host=localhost;Port=5432;Database=micdb;Username=mic;Password=local-dev-password;SSL Mode=Disable;Trust Server Certificate=true"

# Azure OpenAI endpoint (optional)
 dotnet user-secrets set "AI:AzureOpenAI:Endpoint" "https://your-resource-name.openai.azure.com/"
```

These secrets are stored outside the repo and are never committed.

---

## 3. Example Configuration Values

### 3.1 `appsettings.Development.json` (desktop)

- Uses SQLite for local storage:
  - `"ConnectionStrings:MicSqlite": "Data Source=mic_dev.db"`
- `"DeleteDatabaseOnStartup": true` (for easy reseeding in dev)
- AI OpenAI `ApiKey` is empty and expected from env vars.

### 3.2 `appsettings.Production.json` (desktop)

- Uses PostgreSQL:
  - `"ConnectionStrings:MicDatabase": "Host=YOUR_DB_HOST;Port=5432;Database=micdb;Username=mic;Password=CHANGE_ME;SSL Mode=Require;Trust Server Certificate=false"`
- `"DeleteDatabaseOnStartup": false`
- AI OpenAI `ApiKey` is empty and expected from env vars or secret store.

### 3.3 Minimal Environment Setup for Local Dev

1. Ensure `DOTNET_ENVIRONMENT=Development` when running the desktop app.
2. Set:
   - `MIC_AI__OpenAI__ApiKey`
   - (Optional) `MIC_ConnectionStrings__MicDatabase` if you want Postgres instead of SQLite.
3. Run database setup script or let the app seed via `DbInitializer`.

---

## 4. Troubleshooting Common Configuration Errors

### 4.1 "No database connection string configured"

**Symptoms:**
- Application fails at startup with an `InvalidOperationException` from `MIC.Infrastructure.Data.DependencyInjection`.

**Cause:**
- None of the following are set:
  - `MIC_CONNECTION_STRING`
  - `ConnectionStrings:MicDatabase`
  - `ConnectionStrings:MicSqlite`

**Fix:**
- For dev (SQLite): ensure `appsettings.Development.json` contains `ConnectionStrings:MicSqlite`.
- Or set `MIC_ConnectionStrings__MicDatabase` for Postgres.

### 4.2 "OpenAI API key is required but was not configured"

**Symptoms:**
- App throws `InvalidOperationException` when creating `ChatService` or building the Semantic Kernel.

**Cause:**
- `AI.OpenAI.ApiKey` is empty in config and `MIC_AI__OpenAI__ApiKey` is not set.

**Fix:**
- Set `MIC_AI__OpenAI__ApiKey` in your shell.
- Or, in a host that uses User Secrets, run:

  ```bash
   dotnet user-secrets set "AI:OpenAI:ApiKey" "sk-proj-..."
  ```

### 4.3 "AI service rejected the configured API key"

**Symptoms:**
- First chat request fails with a user-facing message:
  - `"AI service rejected the configured API key. Verify that your AI API key is valid and has access to the selected model."`

**Cause:**
- The key is malformed, expired, or does not have permissions for the configured model/deployment.

**Fix:**
- Verify the key in the OpenAI/Azure OpenAI portal.
- Confirm the model or deployment name in configuration (`AI:OpenAI:Model`, `AI:AzureOpenAI:ChatDeploymentName`).
- Replace the key and restart the application.

### 4.4 Environment Name Issues

**Symptoms:**
- Changes in `appsettings.Development.json` dont apply.

**Cause:**
- `DOTNET_ENVIRONMENT` is not set, so the app defaults to `Production`.

**Fix:**
- Set `DOTNET_ENVIRONMENT=Development` in your shell before starting the app.

  ```bash
  export DOTNET_ENVIRONMENT=Development   # macOS/Linux
  $env:DOTNET_ENVIRONMENT="Development"  # PowerShell
  set DOTNET_ENVIRONMENT=Development      # cmd.exe
  ```

---

If configuration still fails after these steps, check the console output for messages from `MIC.Infrastructure.Data.DependencyInjection` and `MIC.Infrastructure.AI.Services.ChatService`, then adjust environment variables or `appsettings.{Environment}.json` accordingly.

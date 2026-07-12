# PensionVault System Setup & Update Guide

This guide explains how to pull the latest changes, configure local environments, and run both the frontend and backend microservices.

---

## 🔄 1. Get the Latest Changes (Git Update)

If the repository is already cloned on your laptop, run these commands to update to the latest code:

```powershell
# 1. Fetch latest updates from the remote repository
git fetch

# 2. Pull the latest commits to the main branch
git pull origin main
```
*(Note: If you have local untracked changes that prevent pulling, run `git stash` before pulling, and then `git stash pop` after pulling).*

---

## ⚙️ 2. Backend Microservices Setup

### Step A: Configure Local Database Connections
Verify that the SQL Server connection strings in the configuration files match your local SQL Server instance:
*   `PensionVault.Gateway/appsettings.json`
*   Microservices configurations under the `microservices/PensionVault.*.Service` folders.
*(By default, they connect to `(local)\SQLEXPRESS` or LocalDB with Windows Authentication (`Trusted_Connection=True;`)).*

### Step B: Start the Services (Separate Terminals)
Open **5 separate terminal windows** and run the following commands to start the backend:

#### Terminal 1: API Gateway (Port 7000)
```powershell
dotnet run --project PensionVault.Gateway\PensionVault.Gateway.csproj
```

#### Terminal 2: Members Service (Port 7001)
```powershell
dotnet run --project microservices\PensionVault.Members.Service\PensionVault.Members.Service.csproj
```

#### Terminal 3: Claims Service (Port 7002)
```powershell
dotnet run --project microservices\PensionVault.Claims.Service\PensionVault.Claims.Service.csproj
```

#### Terminal 4: Annuity Service (Port 7003)
```powershell
dotnet run --project microservices\PensionVault.Annuity.Service\PensionVault.Annuity.Service.csproj
```

#### Terminal 5: Contributions Service (Port 7004)
```powershell
dotnet run --project microservices\PensionVault.Contributions.Service\PensionVault.Contributions.Service.csproj
```

---

## 🎨 3. Frontend React UI Setup

Open a new terminal window in the `pensionvault-ui` directory and run:

```powershell
# 1. Install all UI dependency packages
npm install

# 2. Start the React development web server
npm start
```
*The browser will automatically load the live interface at `http://localhost:3000`.*

---

## 🧪 4. Optional: Running the Test Suite

To verify all core API flows and integrations across the gateway, run the automated PowerShell test script:
```powershell
powershell -ExecutionPolicy Bypass -File .\test_microservices.ps1
```

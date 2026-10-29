# 租屋管理系統

管理端（租客/房源/合約/收支/電費分帳/報修）＋ 房客自助入口（合約/帳單/線上報修）。

## 文件
- 規格：`docs/spec.md`（v2）
- 重構計畫與紀錄：`docs/refactor-plan.md`

## 後端（.NET 10 + PostgreSQL）
- 路徑：`src/backend/RentalManager.Api`（方案檔 `src/backend/RentalManager.slnx`）
- 架構：Controller（薄）→ Service（業務邏輯）→ EF Core；DTO 依領域拆分；錯誤統一 ProblemDetails
- 啟動：
  - `dotnet run`
- 測試：`tests/RentalManager.Api.Tests`
  - `dotnet test src/backend/RentalManager.slnx`
- OpenAPI（開發環境）：`GET /openapi/v1.json`
- 連線字串：
  - 共用預設：`src/backend/RentalManager.Api/appsettings.json`
  - 本機開發：`src/backend/RentalManager.Api/appsettings.Development.json`
- Migration：
  - 新增：`dotnet ef migrations add <Name>`
  - 套用：`dotnet ef database update`（開發環境 API 啟動時也會自動套用；正式環境不自動，見部署文件）
- 種子資料：
  - 匯出目前資料庫成範本：`dotnet run -- --export-seed Seed/seed-template.json`
  - 啟動時若資料庫為空，會自動套用 `Seed/seed-template.json`
- 附件儲存：
  - 設定 `Storage:Root`（預設 `App_Data/uploads`）、`Storage:MaxFileSizeMB`（預設 10）
  - 正式環境為 `/opt/RentalManager/data/uploads`
- 預設管理者：帳號 `admin` / 密碼 `admin123`（正式環境請立即修改）

## 前端（Vue 3 + Pinia + DaisyUI）
- 路徑：`src/frontend`
- 啟動：
  - `npm install`
  - `npm run dev`（dev server 會將 `/api` 代理到 `http://localhost:5091`）
- 路由：
  - `/admin/*` 管理端（AdminLayout 側欄）
  - `/portal/*` 房客端（PortalLayout 頁籤）
  - 登入後依角色自動導向；舊路徑（如 `/tenants`）自動轉址

## 角色
- **管理員**（Role=1）：全部管理功能
- **房客**（Role=2）：帳號需在「帳號管理」關聯租客後建立；登入後只能查看自己的合約、帳單、附件，並可線上報修

## 已實作 API（v2 摘要，完整見 docs/spec.md）
- `POST /api/auth/login`、`POST /api/auth/change-password`
- 管理端（AdminOnly）：`/api/tenants`、`/api/properties`、`/api/rooms`、`/api/contracts`、`/api/charges`、`/api/expenses`、`/api/electricity/*`、`/api/electricity-meter-readings`、`/api/reports/monthly`、`/api/users`、`/api/attachments`、`/api/repair-tickets`
- 房客端（TenantOnly）：`/api/portal/contracts`、`/api/portal/charges`、`/api/portal/repair-tickets`、`/api/portal/attachments`

## 部署（Ubuntu：WAF + nginx + systemd）
完整步驟見 `docs/DEPLOYMENT_UBUNTU.md`。

- 本機建置：`.uilduild-deploy.ps1`（或 VS Code 預設建置工作 `build: deploy`）
  - 後端 → `build/server-backend/`（含 `db_migration/` 冪等 SQL）
  - 前端 → `build/server-frontend/`
- 部署設定來源：`build/systemd/`（服務與環境變數範本）、`build/nginx/nginx.conf`
- 資料庫（PostgreSQL）自行管理；正式環境**不會**自動 migrate，更新後端前先套用 `db_migration/` 腳本
- 備份：外部資料庫 + `/opt/RentalManager/data/uploads`

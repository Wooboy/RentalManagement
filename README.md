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
- 測試：
  - `dotnet test src/backend/RentalManager.slnx`
- OpenAPI（開發環境）：`GET /openapi/v1.json`
- 連線字串：
  - 共用預設：`src/backend/RentalManager.Api/appsettings.json`
  - 本機開發：`src/backend/RentalManager.Api/appsettings.Development.json`
- Migration：
  - 新增：`dotnet ef migrations add <Name>`
  - 套用：`dotnet ef database update`（API 啟動時也會自動套用）
- 種子資料：
  - 匯出目前資料庫成範本：`dotnet run -- --export-seed Seed/seed-template.json`
  - 啟動時若資料庫為空，會自動套用 `Seed/seed-template.json`
- 附件儲存：
  - 設定 `Storage:Root`（預設 `App_Data/uploads`）、`Storage:MaxFileSizeMB`（預設 10）
  - Docker 部署掛載 volume `uploads_data`
- 預設管理者：帳號 `admin` / 密碼 `admin123`（正式環境請立即修改）

## 前端（Vue 3 + Pinia + DaisyUI）
- 路徑：`src/frontend/rental-manager-web`
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

## Docker Compose 部署（Synology NAS）
- 單一 `build/` 資料夾同時是「部署設定來源」與「建置產物輸出」：
  - 追蹤的設定來源：
    - `build/docker-compose.yml`（使用 `./data/postgres` 與 `./data/uploads` bind mounts）
    - `build/.env.example`
    - `build/docker/api-runtime.Dockerfile`
    - `build/docker/web-runtime.Dockerfile`
    - `build/nginx.conf`（`client_max_body_size 20m`，附件上傳用）
  - 由 `scripts/publish-local.ps1` 產生（已 gitignore）：`build/api/`、`build/web/`、`build/seed/`
- 部署模式：
  - 本機先 build 前端與後端，成品直接輸出到 `build/`
  - Docker 只打包執行成品，不在容器內編譯
- 前端容器透過 `nginx` 代理 `/api` 到後端容器。

### 本機先測試
1. 產出部署成品：`powershell -ExecutionPolicy Bypass -File .\scripts\publish-local.ps1`
2. 複製環境檔：`Copy-Item .\build\.env.example .\build\.env`
3. 至少修改 `.env` 內的：
   - `POSTGRES_PASSWORD`
   - `JWT_KEY`
4. 啟動：
   - `cd .\build`
   - `docker compose up -d --build`
5. 開啟：`http://localhost:8080`

### Synology NAS 部署
1. 在本機執行 `scripts/publish-local.ps1` 產出 `build/`
2. 將整個 `build/` 目錄放到 NAS，例如 `/volume1/docker/rental-manager`
3. 在 NAS 上將 `.env.example` 複製成 `.env`，填入正式值
4. 用 Synology Container Manager 以 `build/docker-compose.yml` 建立專案
5. 啟動後，從 `http://NAS_IP:8080` 存取

### 更新流程
1. 本機修改程式後重新執行 `scripts/publish-local.ps1`
2. 把新的 `build/` 覆蓋到 NAS
3. Container Manager 重新部署
4. **注意**：更新內含 DB migration 時，API 啟動會自動套用；`postgres_data` 與 `uploads_data` volume 不會因重建容器而清空

### 建議
- 正式網域請用 Synology Reverse Proxy 導到 `web` 對外埠
- `Seed/seed-template.json` 掛載到 API 容器，資料庫為空時自動初始化
- 附件實體檔存於 `uploads_data` volume，備份時請連同 `postgres_data` 一起備份

# 租屋管理系統

## 規格文件
- `docs/spec.md`

## 後端（.NET 10 + PostgreSQL）
- 路徑：`src/backend/RentalManager.Api`
- 啟動：
  - `dotnet run`
- 連線字串：
  - 共用預設：`src/backend/RentalManager.Api/appsettings.json`
  - 本機開發：`src/backend/RentalManager.Api/appsettings.Development.json`
- Migration：
  - 新增 migration：`dotnet ef migrations add <Name>`
  - 套用 migration：`dotnet ef database update`
- 種子資料：
  - 匯出目前資料庫成範本：`dotnet run -- --export-seed Seed/seed-template.json`
  - 啟動時若資料庫為空，會自動套用 `src/backend/RentalManager.Api/Seed/seed-template.json`
- 預設管理者：
  - 帳號 `admin`
  - 密碼 `admin123`

## 前端（Vue3 + DaisyUI）
- 路徑：`src/frontend/rental-manager-web`
- 啟動：
  - `npm install`
  - `npm run dev`

## 已實作 API（v1）
- `POST /api/auth/login`
- `GET/POST/PUT/DELETE /api/tenants`（含 `keyword` 查詢）
- `GET/POST/PUT/DELETE /api/properties`（含 `keyword` 查詢）
- `GET/POST/PUT/DELETE /api/rooms`（可用 `propertyUnitId` 篩選）
- `GET/POST/PUT/DELETE /api/contracts`（含 `keyword` 查詢）
- `GET/POST/PUT/DELETE /api/charges`（含 `year/month` 查詢）
- `GET/POST/PUT/DELETE /api/expenses`（含 `year/month` 查詢）
- `POST /api/electricity/calculate`
- `GET /api/reports/monthly`

## 備註
- 合約已支援 `PropertyUnitId` 房源關聯（同時保留 `PropertyName/PropertyAddress` 快照欄位）
- 使用 PostgreSQL 前，請先建立資料庫 `rental_manager`

## Docker Compose 部署（Synology NAS）
- 已提供：
  - `deploy/docker-compose.yml`
  - `deploy/.env.example`
  - `deploy/docker/api-runtime.Dockerfile`
  - `deploy/docker/web-runtime.Dockerfile`
  - `scripts/publish-local.ps1`
- 部署模式改為：
  - 本機先 build 前端與後端
  - 由 `deploy/` 模板組出 `publish/`
  - Docker 只打包執行成品，不在容器內編譯
- 前端容器會透過 `nginx` 代理 `/api` 到後端容器，因此前端不再綁死 `localhost` API 位址。

### 本機先測試
1. 複製環境檔：`deploy/.env.example` -> `publish/.env`
2. 至少修改：
   - `POSTGRES_PASSWORD`
   - `JWT_KEY`
3. 在本機產出部署成品：
   - `powershell -ExecutionPolicy Bypass -File .\scripts\publish-local.ps1`
4. 若尚未建立 `publish/.env`，複製：
   - `Copy-Item .\deploy\.env.example .\publish\.env`
5. 啟動：
   - `cd .\publish`
   - `docker compose up -d --build`
6. 開啟：
   - `http://localhost:8080`

### Synology NAS 部署
1. 在本機執行：
   - `powershell -ExecutionPolicy Bypass -File .\scripts\publish-local.ps1`
2. 腳本會在 `publish/` 產出部署成品：
   - `api/publish/`：後端執行檔
   - `web/dist/`：前端靜態檔案
   - `docker/`：Dockerfile
   - `docker-compose.yml`
   - `.env.example`
   - `nginx.conf`
   - `seed/`
3. 將整個 `publish/` 目錄放到 NAS，例如 `/volume1/docker/rental-manager`
4. 在 NAS 上將 `.env.example` 複製成 `.env`，填入正式值
5. 開啟 Synology Container Manager
6. 使用 `publish/docker-compose.yml` 建立專案，working directory 指到 `publish` 所在目錄
7. 啟動後，從 `http://NAS_IP:8080` 存取

### 更新流程
1. 在本機修改程式
2. 重新執行：
   - `powershell -ExecutionPolicy Bypass -File .\scripts\publish-local.ps1`
3. 把新的 `publish/` 目錄覆蓋到 NAS
4. 在 Synology Container Manager 重新部署或重新啟動專案

### 建議
- 若要用正式網域，請在 Synology Reverse Proxy 將網域導到 `web` 對外埠。
- `Seed/seed-template.json` 會掛載到 API 容器內，資料庫為空時可自動初始化。
- PostgreSQL 資料存放在 compose volume `postgres_data`，重建容器不會清空資料。

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

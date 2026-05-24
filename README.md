# 租屋管理系統

## 規格文件
- `docs/spec.md`

## 後端（.NET 10 + SQLite）
- 路徑：`src/backend/RentalManager.Api`
- 啟動：
  - `dotnet run`
- Migration：
  - 新增 migration：`dotnet ef migrations add <Name>`
  - 套用 migration：`dotnet ef database update`
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

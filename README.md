# 租屋管理系統

## 規格文件
- `docs/spec.md`

## 後端（.NET 10 + SQLite）
- 路徑：`backend/RentalManager.Api`
- 啟動：
  - `dotnet run`
- 預設管理者：
  - 帳號 `admin`
  - 密碼 `admin123`

## 前端（Vue3 + DaisyUI）
- 路徑：`frontend/rental-manager-web`
- 啟動：
  - `npm install`
  - `npm run dev`

## 已實作 API（v1）
- `POST /api/auth/login`
- `GET/POST /api/tenants`
- `GET/POST /api/contracts`
- `GET/POST /api/charges`
- `GET/POST /api/expenses`
- `POST /api/electricity/calculate`
- `GET /api/reports/monthly`

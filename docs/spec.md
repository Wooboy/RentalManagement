# 租屋管理系統規格文件 v2

> v1 為初版管理端規格；v2 於 2026-07 重構後更新，加入房客入口、附件與報修模組。
> 重構執行紀錄見 `docs/refactor-plan.md`。

## 1. 目標與範圍
建立一套「房屋仲介租屋管理平台」：
- 管理者以 Web 方式管理租客、房源、合約、收支與電費分帳。
- 房客可登入自助入口查看合約、帳單，並線上報修追蹤進度。

## 2. 系統架構
- 前端：Vue 3 + Vite + DaisyUI（Tailwind CSS）+ Pinia + Vue Router
- 後端：ASP.NET Core Web API (.NET 10)，分層：Controller（薄）→ Service（業務邏輯）→ EF Core
- 資料庫：PostgreSQL（EF Core Code First + Migrations）
- 檔案儲存：`IFileStorage` 抽象，目前為本機磁碟（Docker volume），可替換物件儲存
- 驗證：JWT（claims 含 role 與 tenantId）；錯誤統一回 ProblemDetails
- 部署：前後端分離，nginx 反代 API，Docker Compose（Synology NAS）
- 測試：`RentalManager.Api.Tests`（xunit，涵蓋電費計算、租期計算、抄表序列）

### 後端目錄結構
```
RentalManager.Api/
  Common/       例外定義與 ExceptionHandlingMiddleware
  Controllers/  薄控制器（路由 + 授權標註）
  Dtos/         依領域拆分的 request/response record
  Services/     業務邏輯（每領域一個 Service；純計算為 static）
  Data/         AppDbContext
  Models/       實體與列舉
  Migrations/   EF migrations
```

### 前端目錄結構
```
src/
  stores/       Pinia（auth）
  layouts/      AdminLayout（側欄）/ PortalLayout（房客頁籤）
  views/        管理端頁面
  views/portal/ 房客端頁面
  components/   共用元件（StatCard、AttachmentManager…）
  services/     axios 實例（token 注入、錯誤正規化、401 自動登出）
  router/       /admin/*、/portal/* 與角色守衛
```

## 3. 角色與權限
- 管理者（Admin，`Role=1`）
  - 全部管理功能：租客、房源、合約、收支、電費、報修、帳號
  - API 授權 Policy：`AdminOnly`
- 房客（Tenant，`Role=2`）
  - 帳號需關聯租客（`AppUser.TenantId`）
  - 只能查看自己的合約、帳單、附件；建立與追蹤自己的報修單
  - API 授權 Policy：`TenantOnly`，所有查詢以 JWT 的 `tenantId` 過濾
- 登入後依角色導向 `/admin` 或 `/portal`；跨角色路徑自動導回

## 4. 核心模組

### 4.1 租客管理（同 v1）
- 租客類型：自然人 / 法人
- 欄位：類型、姓名/公司名稱、統編、身分證字號、電話、Email、地址、緊急聯絡人

### 4.2 合約管理（同 v1）
- 關聯租客與多間房間（可跨房源）；快照 `PropertyName/PropertyAddress`
- 付款間隔（月/季/年）與每期應付；批次建立本期租金應收
- 合約狀態：生效中/已到期/已終止

### 4.3 房源管理（同 v1）
- 房源 + 房間主從管理

### 4.4 費用管理（同 v1）
- 應收（租金/水費/電費/其他，支援錶數）與支出（10 類）
- 月報表（收入、支出、損益）

### 4.5 電費規則管理（同 v1）
- 規則 1 依度數、規則 2 平均、規則 3 多錶含公電分攤
- 依電錶抄表與支出帳單試算 → 儲存分帳 → 轉入應收

### 4.6 附件管理（v2 新增）
- 多型附件：`EntityType`（合約/應收/支出/報修）+ `EntityId`
- 檔案類型白名單：jpg/jpeg/png/webp/heic/pdf；大小上限 `Storage:MaxFileSizeMB`（預設 10MB）
- 管理端：合約、應收、支出、報修可上傳/下載/刪除
- 房客端：可下載自己合約/帳單/報修的附件；僅可上傳到自己的報修單
- 檔案存於 `Storage:Root`（預設 `App_Data/uploads`，Docker volume `uploads_data`）

### 4.7 房客入口（v2 新增）
- 總覽：生效中合約數、未繳金額、處理中報修數
- 我的合約：租期、月租、押金、房間、合約附件
- 我的帳單：可依合約與繳費狀態篩選，查看收據附件
- 線上報修：建立（合約/房間/標題/描述/緊急程度）、留言往來（聊天形式）、上傳照片、追蹤狀態

### 4.8 報修管理（v2 新增）
- 狀態流：已送出 → 處理中 → 已解決 → 已結案（可取消）
- 優先度：低/一般/高/緊急
- 管理端：列表篩選、狀態更新（記錄處理人與解決時間）、留言回覆、附件、刪除（連同附件檔案）

## 5. 資料模型（v2）

### AppUser（原 AdminUser）
- Id, Username(unique), PasswordHash, Role(Admin/Tenant)
- DisplayName, Email, TenantId(FK→Tenant, 租客帳號必填), IsActive
- CreatedAtUtc

### Tenant / PropertyUnit / PropertyRoom / Contract / ContractRoom（同 v1）

### ChargeRecord / ExpenseRecord / ElectricityBill / ElectricityAllocation / ElectricityMeterReading（同 v1 實作版）

### Attachment（v2 新增）
- Id, EntityType(Contract/ChargeRecord/ExpenseRecord/RepairTicket), EntityId
- FileName, ContentType, FileSize, StoragePath
- UploadedByUserId(FK→AppUser, SetNull), CreatedAtUtc
- Index: (EntityType, EntityId)

### RepairTicket（v2 新增）
- Id, ContractId(FK, Cascade), PropertyUnitId?, PropertyRoomId?
- Title, Description, Status, Priority
- CreatedByUserId?, HandledByUserId?, ResolvedAtUtc?
- CreatedAtUtc, UpdatedAtUtc

### RepairTicketComment（v2 新增）
- Id, RepairTicketId(FK, Cascade), UserId?, Content, CreatedAtUtc

## 6. API（v2）

### 共用
- `POST /api/auth/login`（回傳 token/userId/username/role/tenantId/displayName）
- `POST /api/auth/change-password`

### 管理端（AdminOnly）
- `GET/POST/PUT/DELETE /api/tenants`
- `GET/POST/PUT/DELETE /api/properties`、`/api/rooms`
- `GET/POST/PUT/DELETE /api/contracts`、`POST /api/contracts/batch-create-period-charges`
- `GET/POST/PUT/DELETE /api/charges`、`/api/expenses`
- `POST /api/electricity/calculate`、`preview-from-expenses`、`bills`、`bills/{id}/create-charges`；`GET bills`、`bills/{id}`
- `GET/POST/PUT /api/electricity-meter-readings`、`GET latest-by-room`
- `GET /api/reports/monthly`
- `GET/POST/PUT/DELETE /api/users`（含租客帳號建立/停用）
- `GET/POST/DELETE /api/attachments`、`GET /api/attachments/{id}/download`
- `GET /api/repair-tickets`、`GET {id}`、`PUT {id}/status`、`POST {id}/comments`、`DELETE {id}`

### 房客端（TenantOnly，一律以 tenantId 過濾）
- `GET /api/portal/contracts`
- `GET /api/portal/charges?contractId=&isPaid=`
- `GET/POST /api/portal/repair-tickets`、`GET {id}`、`POST {id}/comments`
- `GET /api/portal/attachments`、`GET {id}/download`、`POST`（僅限自己的報修單）

### 錯誤格式
所有錯誤統一為 ProblemDetails：`{ "title", "status", "detail" }`；前端攔截器將其正規化為字串。

## 7. 前端頁面（v2）
- 登入頁（依角色導向）
- 管理端 `/admin/*`：儀表板（月營運/未收款/到期合約/待處理報修）、租客、房源、合約、應收、支出、電費試算、電錶抄表、報修管理、帳號管理
- 房客端 `/portal/*`：總覽、我的合約、我的帳單、線上報修

## 8. 非功能需求
- 所有 API 需 JWT 驗證並依角色授權
- 房客端資料存取一律經過擁有權檢查（跨租客回 404）
- 上傳檔案類型/大小白名單驗證；儲存路徑防跳脫
- 金額採 decimal；日期統一 UTC
- 錯誤訊息使用者可讀（中文），系統錯誤不外洩內部細節

## 9. 里程碑
1. M1–M4（v1）：已完成
2. R1：後端分層重構 + 單元測試（已完成）
3. R2：身分模型（AppUser + 租客帳號 + 授權 Policy）（已完成）
4. R3：前端重構（Pinia/layouts/角色路由）+ 管理端改版（已完成）
5. R4：附件系統（已完成）
6. R5：房客入口與報修（已完成）
7. 後續候選：Email 通知、報修指派多管理員、帳單匯出 PDF、行動裝置 PWA

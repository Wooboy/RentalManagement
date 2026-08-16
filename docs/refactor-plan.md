# 重構執行計畫

目標：為「房客登入報修/查看」與「附件上傳（合約、水電費收據）」兩項擴充做好架構準備，同時重新設計畫面。

## 現況問題盤點

### 後端（RentalManager.Api）
- 業務邏輯全部寫在 Controller（`ElectricityController` 587 行），無 Service 層，難以測試與重用。
- 幾乎沒有 DTO（`Dtos/Requests.cs` 只有 14 行），API 回傳匿名物件，前後端契約隱性、易破。
- 錯誤處理與驗證散落各 Controller（回傳中文字串），無統一格式。
- `AdminUser` 命名已不符未來「房客也能登入」的需求。
- 無檔案儲存抽象、無附件資料表、無報修資料表。
- 無 OpenAPI/Swagger，前端開發靠人工對照。

### 前端（rental-manager-web）
- 無狀態管理，`localStorage` 在 router、api、views 各處直接讀寫。
- 無共用元件與 composables，每個 View 重複寫表格、Modal、API 呼叫、錯誤處理。
- 路由是扁平結構，無法承載「管理端 / 房客端」兩種入口。
- 殘留模板檔案（`counter.ts`、`hero.png`、`typescript.svg`）。

### 資料庫
- 整體命名尚可，主要缺口：使用者模型（`AdminUser`）、附件、報修三塊。

---

## Phase 1：後端分層重構（不改任何行為）

**內容**
1. 建立分層結構：
   - `Services/`：每個領域一個 Service（TenantService、ContractService、ChargeService、ExpenseService、ElectricityService…），Controller 只負責路由、驗證入口、回傳。
   - 電費計算邏輯抽成獨立的 `ElectricityCalculationService`（純邏輯，可單元測試）。
2. 建立明確 DTO：
   - `Dtos/` 依領域拆檔，request/response 都用 `record` 定義，淘汰匿名物件。
3. 統一錯誤處理：
   - Exception middleware + `ProblemDetails`，Service 丟領域例外（NotFound/Validation），Controller 不再手刻錯誤字串。
4. 加入 Swagger（開發環境）。
5. 建立測試專案 `RentalManager.Api.Tests`，先覆蓋電費計算與各 Service 的核心驗證邏輯。

**驗收**：所有既有 API 行為不變（以 Swagger + 前端手動驗證），電費計算有單元測試，`dotnet build`、`dotnet test` 通過。

## Phase 2：資料庫與身分模型重構

**內容**
1. `AdminUser` → `AppUser`（table rename migration，保留資料）：
   - 新增欄位：`TenantId (nullable FK)`、`Email (nullable)`、`DisplayName`、`IsActive`。
   - Role 沿用現有 `UserRoleType`（Admin/Tenant）。
2. JWT 重整：
   - Token claims 加入 `role`、`tenantId`。
   - 建立授權 Policy：`AdminOnly`、`TenantOnly`，各 Controller 明確標註（目前全部 `AdminOnly`）。
3. 管理端 API：管理者可為租客建立/停用登入帳號、重設密碼。
4. `DbSeedService` 與 `seed-template.json` 同步更新。
5. 其他欄位調整順手做：檢視現有 snapshot 欄位（如 Contract 的 `PropertyName/PropertyAddress`）是否保留，統一 `CreatedAtUtc/UpdatedAtUtc` 覆蓋率。

**驗收**：既有 admin 帳號可正常登入；可建立租客帳號並取得帶 `tenantId` 的 token；migration 可在既有資料庫上順利升級。

## Phase 3：前端基礎重構 + 管理端畫面重新設計

**內容**
1. 基礎建設：
   - 導入 Pinia：`authStore`（token、role、tenantId、登入/登出）取代散落的 localStorage 存取。
   - `composables/`：`useApi`（統一錯誤處理）、`useCrud`（列表/新增/編輯/刪除通用流程）。
   - `components/`：DataTable、FormModal、ConfirmDialog、PageHeader、EmptyState 等共用元件。
   - API 層依領域拆檔（`api/tenants.ts`、`api/contracts.ts`…），型別對齊後端 DTO。
   - 清除模板殘留檔。
2. 路由重組：
   - `/admin/*`（管理端）與 `/portal/*`（房客端，Phase 5 填內容），依 role 導向與守衛。
3. 畫面重新設計（管理端）：
   - Sidebar + Topbar 的後台版型（AdminLayout），DaisyUI theme 統一。
   - Dashboard 改為有意義的總覽（本月應收/已收、到期合約提醒、待處理報修數）。
   - 各管理頁改用共用元件重寫，統一表格、篩選、表單體驗，支援 RWD。

**驗收**：所有既有功能在新版面下可正常操作；重複的表格/Modal 程式碼收斂到共用元件。

## Phase 4：附件系統（合約、水電費收據）

**內容**
1. DB：新增 `Attachment`：
   - `Id, FileName, ContentType, FileSize, StoragePath, EntityType (Contract/ChargeRecord/ExpenseRecord/RepairTicket), EntityId, UploadedByUserId, CreatedAtUtc`。
2. 後端：
   - `IFileStorage` 抽象 + `LocalFileStorage` 實作（儲存路徑由設定檔控制，Docker 掛 volume）。
   - 上傳/下載/刪除 API，含檔案類型與大小白名單驗證；下載權限檢查（房客只能看自己合約相關附件）。
3. 前端：
   - `AttachmentUploader` / `AttachmentList` 共用元件，掛進合約、收費、支出頁。
4. 部署：`docker-compose` 加 uploads volume，nginx `client_max_body_size` 調整。

**驗收**：合約/收費/支出可上傳、預覽、下載、刪除附件；重啟容器附件不遺失；越權下載被拒。

## Phase 5：房客入口（登入、查看、報修）

**內容**
1. DB：新增 `RepairTicket`：
   - `Id, ContractId, PropertyUnitId, PropertyRoomId (nullable), Title, Description, Status (Submitted/InProgress/Resolved/Closed/Cancelled), Priority, CreatedByUserId, HandledByUserId (nullable), ResolvedAtUtc, CreatedAtUtc, UpdatedAtUtc`。
   - `RepairTicketComment`（雙方留言往來）：`Id, RepairTicketId, UserId, Content, CreatedAtUtc`。
   - 附件沿用 Phase 4 的 `Attachment`（`EntityType = RepairTicket`）。
2. 後端 API：
   - 房客端（`TenantOnly`）：查看自己的合約、繳費紀錄、附件；建立報修單（可附照片）、查看進度、留言。
   - 管理端（`AdminOnly`）：報修單列表/指派/更新狀態/留言。
3. 前端：
   - PortalLayout（簡潔的房客版型，行動優先）。
   - 頁面：我的合約、我的帳單、我的報修（列表 + 建立 + 明細留言）。
   - 管理端新增「報修管理」頁，Dashboard 顯示待處理報修。

**驗收**：房客帳號登入只能看到自己的資料；完整報修流程（建立→處理→結案）雙端可操作。

## Phase 6：部署與文件收尾

**內容**
1. `publish` 腳本與 docker compose 更新（uploads volume、環境變數）。
2. 更新 `README.md`、`docs/spec.md`（v2：角色權限、附件、報修模組）。
3. seed template 更新（含示範租客帳號、報修單）。
4. 全流程驗證：本機 docker compose 起一輪，跑過管理端與房客端主要情境。

---

## 執行順序與依賴

```
Phase 1（後端分層） → Phase 2（DB/身分） → Phase 3（前端基礎+改版）
                                              ├→ Phase 4（附件）
                                              └→ Phase 5（房客入口，依賴 Phase 4 的附件）
Phase 6（部署文件）最後收尾
```

- Phase 1–3 是純重構（對外功能不變），每個 Phase 結束都保持系統可部署。
- Phase 4、5 是新功能，建立在重構後的架構上。
- 每個 Phase 一個（或數個）獨立 commit/PR，可隨時中斷不留半成品。

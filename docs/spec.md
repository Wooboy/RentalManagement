# 租屋管理系統規格文件 v1

## 1. 目標與範圍
建立一套「房屋仲介租屋管理平台」，提供管理者以 Web 方式管理租客、合約、收支與帳單，並支援多種電費計算規則。

## 2. 系統架構
- 前端：Vue 3 + Vite + DaisyUI（Tailwind CSS）
- 後端：ASP.NET Core Web API (.NET 10)
- 資料庫：SQLite（EF Core Code First）
- 驗證：管理者帳號密碼登入（JWT）
- 部署：前後端分離，API + SPA

## 3. 角色與權限
- 管理者（Admin）
  - 登入系統
  - 管理租客資料
  - 管理合約
  - 建立應收/應付費用
  - 設定電費計算規則
  - 查看報表

## 4. 核心模組

### 4.1 租客管理
- 租客類型：自然人 / 法人
- 欄位
  - 類型
  - 姓名或公司名稱
  - 統一編號（法人）
  - 身分證字號（自然人，可選）
  - 電話、Email、通訊地址
  - 緊急聯絡人與電話

### 4.2 合約管理
- 建立合約並關聯租客與房源
- 欄位
  - 合約編號
  - 房源名稱/地址
  - 起租日、迄租日
  - 月租金
  - 押金
  - 居住人數
  - 電費計算規則
- 功能
  - 查詢關聯繳款記錄
  - 合約狀態（生效中/已到期/已終止）

### 4.3 房源管理
- 管理多間房屋/房源基本資料
- 欄位
  - 房源代碼
  - 房源名稱
  - 房源地址
  - 備註
- 功能
  - 房源新增/編輯/刪除
- 房間主從管理：房源底下可新增多間房間
- 合約建立時可選擇單一房源下的多間房間

### 4.4 費用管理
- 收入：租金、其他收入
- 支出：水費、電費、瓦斯費、網路費、電視費、管理費、停車費、稅金、修繕、其他
- 功能
  - 新增應收費用（特別支援水費/電費含錶數）
  - 新增支出費用
  - 月報表（收入、支出、損益）

### 4.5 電費規則管理
每張電費帳單可指定規則：

1) 依度數計算
- 租客當期度數 * 每度金額

2) 平均計算
- 帳單金額 / 租客使用度數總和 = 每度均價
- 租客度數 * 每度均價

3) 多錶計算（含公電）
- 多個電錶帳單合併
- 單價 = 帳單總額 / 總度數
- 租客私電 = 租客度數 * 單價
- 公電總額 = 帳單總額 - 全體租客私電總和
- 平均日單價 = 公電總額 / Σ(租客人數 * 入住日數)
- 租客公電 = 租客人數 * 入住日數 * 平均日單價
- 應繳金額 = 租客私電 + 租客公電

## 5. 資料模型（初版）

### AdminUser
- Id, Username, PasswordHash, CreatedAt

### Tenant
- Id, Type(Person/Company), Name, TaxId, PersonalId
- Phone, Email, Address, EmergencyContactName, EmergencyContactPhone
- CreatedAt, UpdatedAt

### Contract
- Id, ContractNo, TenantId, PropertyUnitId, PropertyName, PropertyAddress
- StartDate, EndDate, MonthlyRent, Deposit, OccupantCount
- ElectricityRuleType(Unit/Avg/MultiMeter), Status
- CreatedAt, UpdatedAt

### PropertyUnit
- Id, Code, Name, Address, Notes, CreatedAt, UpdatedAt

### PropertyRoom
- Id, PropertyUnitId, Code, Name, Notes, CreatedAt, UpdatedAt

### ChargeRecord（應收）
- Id, ContractId, Category(Rent/Water/Electricity/...)
- BillingStart, BillingEnd
- MeterStart, MeterEnd, UsageUnits
- Amount, Notes, IsPaid, PaidAt
- CreatedAt

### ExpenseRecord（支出）
- Id, Category
- BillingStart, BillingEnd
- Amount, Notes, OccurredAt
- CreatedAt

### ElectricityBill
- Id, ContractId
- RuleType(Unit/Avg/MultiMeter)
- BillPeriodStart, BillPeriodEnd
- TotalAmount, TotalUnits
- UnitPrice(可空，系統計算)
- PublicElectricityAmount(可空)
- CreatedAt

### ElectricityMeterEntry
- Id, ElectricityBillId, MeterName
- StartReading, EndReading, Units, Amount

### TenantElectricityUsage
- Id, ElectricityBillId, ContractId
- TenantUnits
- OccupantCount
- OccupancyDays
- PrivateElectricityAmount
- PublicElectricityAmount
- PayableAmount

## 6. API（v1）
- `POST /api/auth/login`
- `GET/POST/PUT/DELETE /api/tenants`
- `GET/POST/PUT/DELETE /api/properties`
- `GET/POST/PUT/DELETE /api/rooms`
- `GET/POST/PUT/DELETE /api/contracts`
- `GET/POST /api/charges`
- `GET/POST /api/expenses`
- `POST /api/electricity/calculate`（輸入規則與用量，回傳試算）
- `GET /api/reports/monthly?year=YYYY&month=MM`

## 7. 前端頁面（v1）
- 登入頁
- 儀表板（本月收入/支出/損益）
- 租客管理頁
- 房源管理頁
- 合約管理頁
- 應收費用頁（含水電錶數）
- 支出管理頁
- 電費試算頁
- 月報表頁

## 8. 非功能需求
- 所有管理 API 需 JWT 驗證
- 主要清單支援分頁與關鍵字搜尋
- 重要異動保留時間戳記
- 金額採 decimal(18,2)
- 日期統一儲存 UTC

## 9. 里程碑
1. M1：專案骨架、登入、租客 CRUD、合約 CRUD
2. M2：應收/支出、報表
3. M3：三種電費計算與試算/入帳
4. M4：優化 UX、補測試、部署

# Ubuntu 主機部署（WAF + nginx + systemd）

後端（ASP.NET Core）以 systemd 服務執行，前端（Vue 靜態檔）由同一台主機的 nginx 提供，
nginx 同時反向代理 API。對外由 WAF 終止 TLS 後轉入。
**資料庫（PostgreSQL）自行管理**，正式環境不會自動 migrate（開發環境仍會）。

```text
瀏覽器
  -> HTTPS -> WAF（終止 TLS）
  -> HTTP  -> Ubuntu nginx :80
               -> /        -> /opt/RentalManager/frontend（Vue 靜態檔）
               -> /api/*   -> 保留前綴轉入 -> Kestrel 127.0.0.1:5091
  -> 外部 PostgreSQL
```

## 1. 檔案來源

| 版控中的檔案                                   | 用途                               | 主機上的位置                                       |
| ---------------------------------------------- | ---------------------------------- | -------------------------------------------------- |
| `build/build-deploy.ps1`                       | 本機建置前後端（依序呼叫下面兩支） | —                                                  |
| `build/build-backend.ps1`                      | 本機只建置後端                     | —                                                  |
| `build/build-frontend.ps1`                     | 本機只建置前端                     | —                                                  |
| `build/systemd/rental-manager.service`         | 後端服務定義                       | `/etc/systemd/system/rental-manager.service`       |
| `build/systemd/rental-manager-api.env.example` | 後端環境變數範本                   | `/opt/RentalManager/config/rental-manager-api.env` |
| `build/nginx/nginx.conf`                       | nginx 站台設定                     | `/etc/nginx/sites-available/rental-manager`        |

建置產物（不進版控）：

| 建置輸出                                      | 主機上的位置                  |
| --------------------------------------------- | ----------------------------- |
| `build/server-backend/`（含 `db_migration/`） | `/opt/RentalManager/backend`  |
| `build/server-frontend/`                      | `/opt/RentalManager/frontend` |

## 2. 本機建置

```powershell
.\build\build-deploy.ps1                 # 預設 Release、linux-x64
.\build\build-deploy.ps1 -RunTests       # 先跑後端 xUnit 測試
.\build\build-deploy.ps1 -EfBundle       # 另外產生 efbundle（約 140MB）
.\build\build-deploy.ps1 -IncludeSeed    # 一併打包種子資料範本（預設不打包）

.\build\build-backend.ps1                # 只建置後端（同樣支援 -RunTests、-EfBundle、-IncludeSeed）
.\build\build-frontend.ps1               # 只建置前端
```

單獨建置只會清除並重建自己的輸出目錄，另一邊的產物不受影響。

VS Code 預設建置工作 `build: deploy` 等同第一行；另有 `build: deploy backend`、`build: deploy frontend`。

> 種子資料範本 `src/backend/RentalManager.Api/Seed/seed-template.json` 可能含真實資料，**預設不打包**。
> 新主機要匯入初始資料時才加 `-IncludeSeed`：範本會放進 `backend/Seed/`，於資料庫業務資料為空時
> 自動匯入（見 4.5 `SeedData__ApplyOnStartup`）。匯入完成後，之後的版本更新不需再加。

## 3. 主機目錄與權限

`/opt/RentalManager` 同時是服務帳號 `rentalmgr` 的 HOME。原則是**服務帳號對程式與設定一律唯讀**，
唯一可寫的是附件目錄 `data/`（systemd 的 `ProtectSystem=strict` + `ReadWritePaths` 另行強制）。

| 路徑        | 擁有者                | 目錄 / 檔案權限 | 說明                                                 |
| ----------- | --------------------- | --------------- | ---------------------------------------------------- |
| `./`        | `root:root`           | `755`           | nginx（www-data）需能進入以讀取 `frontend/`          |
| `backend/`  | `root:root`           | `755` / `644`   | 後端程式（由 `dotnet` 載入，不需執行權限）           |
| `frontend/` | `root:root`           | `755` / `644`   | 前端靜態檔                                           |
| `config/`   | `root:rentalmgr`      | `750` / `640`   | `rental-manager-api.env`（連線字串、JWT 金鑰等機密） |
| `data/`     | `rentalmgr:rentalmgr` | `700`           | 附件上傳（`data/uploads`），服務唯一可寫入的目錄     |

## 4. 首次部署

### 4.1 安裝套件

```bash
sudo apt update
sudo apt install -y nginx postgresql-client
```

ASP.NET Core Runtime 需與專案相同的 **.NET 10**，依
[Microsoft 的 Ubuntu 安裝說明](https://learn.microsoft.com/dotnet/core/install/linux-ubuntu)
安裝 `aspnetcore-runtime-10.0`，並確認 `dotnet --list-runtimes` 列出 `Microsoft.AspNetCore.App 10.x`、
`/usr/bin/dotnet` 存在（服務的 `ExecStart` 使用此路徑）。

### 4.2 建立服務帳號與目錄

```bash
sudo useradd --system --home-dir /opt/RentalManager --no-create-home \
     --shell /usr/sbin/nologin rentalmgr

sudo install -d -o root      -g root      -m 755 /opt/RentalManager
sudo install -d -o root      -g root      -m 755 /opt/RentalManager/backend
sudo install -d -o root      -g root      -m 755 /opt/RentalManager/frontend
sudo install -d -o root      -g rentalmgr -m 750 /opt/RentalManager/config
sudo install -d -o rentalmgr -g rentalmgr -m 700 /opt/RentalManager/data
sudo install -d -o rentalmgr -g rentalmgr -m 700 /opt/RentalManager/data/uploads
```

### 4.3 上傳程式

把 `build/server-backend/`、`build/server-frontend/`、`build/systemd/`、`build/nginx/` 上傳到主機
暫存目錄（例如 `~/release/`，可用 `scp -r` 或 WinSCP），再以 root 安裝並重設權限。
前後端指令各自獨立，更新時只需執行有變動的那一段（見第 6 節）。

#### 4.3.1 後端

```bash
cd ~/release
sudo rsync -a --delete server-backend/ /opt/RentalManager/backend/
sudo chown -R root:root /opt/RentalManager/backend
sudo find /opt/RentalManager/backend -type d -exec chmod 755 {} +
sudo find /opt/RentalManager/backend -type f -exec chmod 644 {} +
```

#### 4.3.2 前端

```bash
cd ~/release
sudo rsync -a --delete server-frontend/ /opt/RentalManager/frontend/
sudo chown -R root:root /opt/RentalManager/frontend
sudo find /opt/RentalManager/frontend -type d -exec chmod 755 {} +
sudo find /opt/RentalManager/frontend -type f -exec chmod 644 {} +
```

### 4.4 資料庫

使用兩個帳號，應用程式帳號不具建表權限：

| 帳號 | 用途 | 權限 |
|---|---|---|
| `rental_owner` | 套用 migration（建表、改表） | 資料表與序列的擁有者 |
| `rental` | 應用程式連線（`ConnectionStrings__DefaultConnection`） | 只有資料讀寫（`SELECT/INSERT/UPDATE/DELETE`） |

以下帳號、資料庫名稱為範例，請依實際環境替換；SQL 皆以 superuser（例如 `postgres`）執行。

#### 4.4.1 建立資料庫與帳號（首次）

```bash
sudo -u postgres psql
```

```sql
CREATE ROLE rental_owner LOGIN PASSWORD 'change_this_owner_password';
-- CONNECTION LIMIT -1 = 不限制；設成 0 會無法連線（SqlState 53300）
CREATE ROLE rental LOGIN PASSWORD 'change_this_app_password' CONNECTION LIMIT -1;
CREATE DATABASE prod_rental OWNER rental_owner;
```

帳號已存在時，確認可登入且連線上限不是 `0`：

```sql
SELECT rolname, rolcanlogin, rolconnlimit FROM pg_roles WHERE rolname IN ('rental_owner', 'rental');
ALTER ROLE rental CONNECTION LIMIT -1;   -- rolconnlimit 為 0 時
```

#### 4.4.2 套用資料庫結構

以 **owner 帳號**套用 `rental-manager-schema.idempotent.sql`。此為冪等腳本，
可對任何版本的既有 DB 重複執行，只套用尚未套用的 migration：

```bash
psql "host=DB_HOST port=5432 dbname=prod_rental user=rental_owner" \
     -v ON_ERROR_STOP=1 \
     -f /opt/RentalManager/backend/db_migration/rental-manager-schema.idempotent.sql
```

使用 `-EfBundle` 建置時，也可改用自帶執行檔：

```bash
sudo chmod 755 /opt/RentalManager/backend/db_migration/efbundle
/opt/RentalManager/backend/db_migration/efbundle \
     --connection "Host=DB_HOST;Port=5432;Database=prod_rental;Username=rental_owner;Password=..."
```

> 從舊的 Docker 部署移轉時，資料庫已由舊版 API 自動 migrate 過，腳本會略過已套用的 migration。

#### 4.4.3 授權應用程式帳號（首次）

在 4.4.2 之後執行。除了授權現有資料表，也設定**預設權限**：之後 migration 由同一個 owner
新增的資料表與序列會自動授權給 `rental`，更新版本時不需重跑。

> ⚠️ 必須連進 `prod_rental` 執行（提示字元為 `prod_rental=#`）。在 `postgres` 資料庫執行會找不到
> 資料表，授權不會生效。

```bash
sudo -u postgres psql -d prod_rental
```

```sql
DO $$
DECLARE
  owner_role text;
BEGIN
  -- 以 EF 的 migration 紀錄表擁有者，作為「跑 migration 的帳號」
  SELECT tableowner INTO owner_role
  FROM pg_tables
  WHERE schemaname = 'public' AND tablename = '__EFMigrationsHistory';

  IF owner_role IS NULL THEN
    RAISE EXCEPTION '找不到 __EFMigrationsHistory：請確認已連進正確的資料庫，且已完成 4.4.2';
  END IF;
  RAISE NOTICE 'migration owner = %', owner_role;

  -- 連線與 schema
  EXECUTE format('GRANT CONNECT ON DATABASE %I TO rental', current_database());
  EXECUTE 'GRANT USAGE ON SCHEMA public TO rental';

  -- 現有物件（序列的 UPDATE 供匯入種子資料後以 setval 重設 ID）
  EXECUTE 'GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO rental';
  EXECUTE 'GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA public TO rental';

  -- 未來 migration 由同一個 owner 建立的物件自動授權
  EXECUTE format('ALTER DEFAULT PRIVILEGES FOR ROLE %I IN SCHEMA public
                  GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO rental', owner_role);
  EXECUTE format('ALTER DEFAULT PRIVILEGES FOR ROLE %I IN SCHEMA public
                  GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO rental', owner_role);
END $$;
```

驗證（同樣在 `prod_rental` 內）：

```sql
-- 每張資料表應列出 SELECT,INSERT,UPDATE,DELETE
SELECT table_name, string_agg(privilege_type, ',') AS privs
FROM information_schema.role_table_grants
WHERE grantee = 'rental' AND table_schema = 'public'
GROUP BY table_name ORDER BY table_name;

-- 預設權限應有 r（table）與 S（sequence）兩列
SELECT pg_get_userbyid(defaclrole) AS owner, defaclobjtype, defaclacl FROM pg_default_acl;
```

> 預設權限只對**同一個 owner** 之後建立的物件生效。往後的 migration 一律以 `rental_owner` 套用；
> 若改用其他帳號，需對該帳號重跑上面兩行 `ALTER DEFAULT PRIVILEGES`，並重新授權現有物件。

### 4.5 後端環境變數

```bash
sudo install -o root -g rentalmgr -m 640 /tmp/rental/systemd/rental-manager-api.env.example \
     /opt/RentalManager/config/rental-manager-api.env
sudo nano /opt/RentalManager/config/rental-manager-api.env
```

各參數說明見檔案內註解。至少需填：

- `ConnectionStrings__DefaultConnection`
- `Jwt__Key`（`openssl rand -base64 48` 產生）
- `AllowedHosts`（對外網域）

檔案必須是 LF 換行（版控已以 `.gitattributes` 固定；若在 Windows 另行編輯後上傳，請確認未轉成 CRLF，
否則每個值結尾都會多一個 `\r`）。

### 4.6 註冊後端服務

```bash
sudo cp /tmp/rental/systemd/rental-manager.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now rental-manager
systemctl status rental-manager
curl http://127.0.0.1:5091/api/health          # 預期 {"status":"ok"}
```

以 `-IncludeSeed` 建置且業務資料為空時，首次啟動會自動匯入種子資料。
預設管理者帳號 `admin` / `admin123`（由 4.4.2 的 migration 建立），**登入後請立即修改密碼**。

### 4.7 nginx 站台

先修改 `nginx.conf` 的 `set_real_ip_from`（WAF 位址）與 `server_name`（對外網域），再安裝：

```bash
sudo cp /tmp/rental/nginx/nginx.conf /etc/nginx/sites-available/rental-manager
sudo ln -s /etc/nginx/sites-available/rental-manager /etc/nginx/sites-enabled/
sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t && sudo systemctl reload nginx
curl -H "Host: rental.wooboy.me" http://127.0.0.1/api/health
```

與其他站台共用同一台主機時，以 `server_name` 區分即可（`map` 變數已帶站台前綴，不會衝突）。
日誌位於 `/var/log/nginx/`，由 Ubuntu nginx 套件內建的 `/etc/logrotate.d/nginx` 每日輪替。

### 4.8 主機防火牆

80 埠只允許 WAF 連入，避免繞過 WAF 直連：

```bash
sudo ufw allow OpenSSH
sudo ufw allow from 10.0.0.0/24 to any port 80 proto tcp     # 改成 WAF 的 IP 或網段
sudo ufw enable
```

## 5. WAF 設定需求

- 對外終止 TLS，以 HTTP 轉入 Ubuntu 主機 80 埠。
- **保留使用者原始的 `Host` 標頭**，否則會被後端 `AllowedHosts` 擋下（HTTP 400），也無法命中 nginx 的 `server_name`。
- 送出 `X-Forwarded-For`（使用者 IP）與 `X-Forwarded-Proto: https`。
- HSTS（`Strict-Transport-Security`）建議在 WAF 上送出；確定全站已穩定走 HTTPS 後再啟用。
- 不可快取 `/api/*`。
- 上傳大小限制不可小於 20MB（附件上傳）。

## 6. 更新版本

依變動範圍只建置、上傳、安裝需要的部分（前後端都要更新時用 `build-deploy.ps1` 一次建置）。

### 6.1 只更新前端

1. 本機執行 `.\build\build-frontend.ps1`，上傳 `server-frontend/` 到主機暫存目錄。
2. 重跑 4.3.2。

不需重啟後端或 reload nginx。前端 `index.html` 設為 no-store、`/assets/*` 檔名帶雜湊，
更新後不需清除瀏覽器快取。

### 6.2 更新後端

1. 本機執行 `.\build\build-backend.ps1`，上傳 `server-backend/` 到主機暫存目錄。
2. 若有新的 migration，以 `rental_owner` 重跑 4.4.2（冪等）。**須在重啟服務前完成**，否則新版程式會讀不到新欄位。
   新增的資料表會依 4.4.3 的預設權限自動授權給 `rental`，不需重跑 4.4.3。
3. 重跑 4.3.1。
4. `sudo systemctl restart rental-manager`，並以 `curl http://127.0.0.1:5091/api/health` 確認。

### 6.3 服務或 nginx 設定變更

若 `build/systemd/` 或 `build/nginx/` 有變更，上傳後重跑 4.6 或 4.7。

## 7. 備份

- 外部 PostgreSQL（`pg_dump`）
- `/opt/RentalManager/data/uploads`（附件實體檔）
- `/opt/RentalManager/config/rental-manager-api.env`

## 8. 疑難排解

| 現象                                                 | 檢查                                                                                    |
| ---------------------------------------------------- | --------------------------------------------------------------------------------------- |
| 服務起不來                                           | `journalctl -u rental-manager -n 100`；確認 `/usr/bin/dotnet` 與 .NET 10 runtime        |
| 服務啟動即失敗、日誌出現 relation ... does not exist | 尚未套用 4.4.2 的 migration 腳本（啟動時會檢查種子資料）                                |
| `SqlState: 53300` too many connections for role      | 帳號連線上限：`rolconnlimit` 為 `0` 時完全無法連線，見 4.4.1 改為 `-1`                  |
| `42501: permission denied for table ...`             | 應用程式帳號未授權：在 `prod_rental` 內重跑 4.4.3；若 migration 換了帳號套用，見 4.4.3 末段 |
| 附件上傳失敗                                         | `data/uploads` 是否存在且為 `rentalmgr` 擁有；`Storage__Root` 路徑；nginx／WAF 上傳大小 |
| HTTP 400 Bad Request                                 | WAF 未保留 `Host`，或 `AllowedHosts` 未包含對外網域                                     |
| HTTP 413                                             | nginx `client_max_body_size` 或 WAF 上傳大小限制                                        |
| HTTP 502                                             | 後端未啟動，或 `ASPNETCORE_URLS` 埠號與 nginx `proxy_pass` 不一致                       |

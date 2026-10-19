# Docker – Project Management AI

## 1. Giới thiệu

Thư mục `infra/docker` chứa cấu hình Docker Compose phục vụ môi trường phát triển của hệ thống quản lý công việc tích hợp AI.

Trong Tuần 2, Docker Compose được sử dụng để triển khai PostgreSQL 16, phục vụ các module của hệ thống.

## 2. Yêu cầu

- Docker Desktop đã được cài đặt.
- Docker Engine đang chạy.
- Docker Compose phiên bản 2 trở lên.

Kiểm tra:

```powershell
docker --version
docker compose version
```

## 3. Khởi động PostgreSQL

Tại thư mục gốc dự án, chạy:

```powershell
docker compose -f infra/docker/docker-compose.yml up -d postgres
```

## 4. Kiểm tra trạng thái

```powershell
docker ps
```

Container `pmai-postgres` cần có trạng thái `healthy`.

Kiểm tra kết nối:

```powershell
docker exec pmai-postgres pg_isready -U pmai -d pmai
```

Kết quả mong đợi:

```text
accepting connections
```

## 5. Thông tin kết nối Database

| Thuộc tính | Giá trị |
|---|---|
| Host | localhost |
| Port | 5432 |
| Database | pmai |
| Username | pmai |
| Password | pmai_Tuan2_2026 |

Thông tin trên chỉ dùng cho môi trường phát triển local.

## 6. Xem log PostgreSQL

```powershell
docker compose -f infra/docker/docker-compose.yml logs postgres
```

## 7. Dừng PostgreSQL

```powershell
docker compose -f infra/docker/docker-compose.yml down
```

Lệnh này dừng container nhưng vẫn giữ dữ liệu trong Docker volume.

## 8. Reset Database

```powershell
docker compose -f infra/docker/docker-compose.yml down -v
```

**Cảnh báo:** Lệnh này xóa volume dữ liệu PostgreSQL của Compose project. Chỉ sử dụng khi thực sự muốn reset database.

## 9. Kiểm tra cấu hình Docker Compose

```powershell
docker compose -f infra/docker/docker-compose.yml config -q
```

Nếu lệnh thành công, mã thoát sẽ bằng `0`.

## 10. Áp dụng migration EF Core

Sau khi PostgreSQL có trạng thái `healthy`, tạo schema và bảng cho các module từ
thư mục gốc của dự án. Các biến môi trường dưới đây chỉ có hiệu lực trong cửa sổ
PowerShell hiện tại và không được commit vào repository.

```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
$env:Jwt__SecretKey="chuoi-bat-ky-dai-hon-32-ky-tu-0123456789"
Remove-Item Env:ConnectionStrings__Default -ErrorAction SilentlyContinue

dotnet ef database update `
  --project src/Modules/Identity/Identity.Infrastructure `
  --startup-project src/Host/ProjectManagementAI.Api

dotnet ef database update `
  --project src/Modules/Projects/Projects.Infrastructure `
  --startup-project src/Host/ProjectManagementAI.Api
```

Chạy lại mỗi lệnh `database update` lần thứ hai. Kết quả mong đợi là:
`No migrations were applied. The database is already up to date.`

Kiểm tra schema và bảng đã được tạo:

```powershell
docker exec pmai-postgres psql -U pmai -d pmai -c "\dn"
docker exec pmai-postgres psql -U pmai -d pmai -c "\dt identity.*"
docker exec pmai-postgres psql -U pmai -d pmai -c "\dt projects.*"
```

Kết quả mong đợi:

- `\dn` có các schema `identity`, `projects` và `public`.
- `\dt identity.*` có `__EFMigrationsHistory` và `users`.
- `\dt projects.*` có `__EFMigrationsHistory`, `project_members` và `projects`.

Kiểm tra model không còn thay đổi chưa tạo migration:

```powershell
dotnet ef migrations has-pending-model-changes `
  --project src/Modules/Identity/Identity.Infrastructure `
  --startup-project src/Host/ProjectManagementAI.Api

dotnet ef migrations has-pending-model-changes `
  --project src/Modules/Projects/Projects.Infrastructure `
  --startup-project src/Host/ProjectManagementAI.Api
```

Cả hai lệnh phải in:
`No changes have been made to the model since the last migration.`

## 11. Người phụ trách

Testing, CI/CD & Deploy – Đào Xuân Hòa.

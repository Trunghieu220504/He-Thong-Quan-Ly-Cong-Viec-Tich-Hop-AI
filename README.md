# Hệ Thống Quản Lý Công Việc Tích Hợp AI

> Hệ thống quản lý dự án kiểu Trello/Jira thu nhỏ, tích hợp AI dự đoán rủi ro trễ tiến độ.

## Tổng quan

Hệ thống cho phép quản lý dự án, task theo Kanban (kéo-thả), sprint/burndown, thông báo real-time và dashboard tổng hợp. AI đóng vai trò hỗ trợ (dự đoán trễ, gợi ý ưu tiên) — **PM là người quyết định cuối cùng, AI không tự động thay đổi dữ liệu**.

## Kiến trúc

**Clean Architecture (trong mỗi module) + Modular Monolith (toàn hệ thống)**

```
Api (Host) -> Infrastructure -> Application -> Domain (SharedKernel)
```

- Mỗi module nghiệp vụ (Identity, Projects, Tasks, Sprints, Notifications, Dashboard, RiskAI) có Domain / Application / Infrastructure / Contracts / Api riêng.
- Module chỉ giao tiếp qua `*.Contracts` (DTO + interface) và `Integration Event` (qua `IIntegrationEventBus`), cấm reference trực tiếp Domain/Infrastructure của module khác.
- Database: 1 PostgreSQL, N schema (`identity`, `projects`, `tasks`, `sprints`, `notifications`, `riskai`), không FK vật lý xuyên schema.
- Host `ProjectManagementAI.Api` là composition root duy nhất biết toàn bộ hệ thống.
- Chi tiết kiến trúc xem tài liệu `Huong_dan_trien_khai_Clean_Architecture_ModularMonolith.pdf`.

## Công nghệ

| Thành phần | Công nghệ |
|---|---|
| Backend | .NET 10, ASP.NET Core (Minimal API), EF Core + Npgsql, MediatR, FluentValidation |
| AI Service | Python 3.12, FastAPI, scikit-learn |
| Frontend | React + TypeScript (Vite), TanStack Query, Zustand, @dnd-kit, SignalR |
| Infra | PostgreSQL 16, MinIO, MailHog, Docker Compose, Nginx |
| Test & CI | xUnit, NetArchTest, Vitest, Playwright, pytest, GitHub Actions |

## Cấu trúc thư mục

```
.
├── src/
│   ├── BuildingBlocks/
│   │   ├── SharedKernel/              # Entity, AggregateRoot, ValueObject, Result, DomainEvent
│   │   ├── Application.Abstractions/  # ICommand, IQuery, ICurrentUser, IUnitOfWork
│   │   ├── Infrastructure.Persistence/# BaseDbContext, Outbox
│   │   └── EventBus/                  # IIntegrationEventBus
│   ├── Modules/
│   │   ├── Identity/                  # Đăng ký, đăng nhập, JWT, phân quyền
│   │   ├── Projects/                  # CRUD dự án, thành viên
│   │   ├── TaskManagement/            # Task/Kanban lõi
│   │   ├── Sprints/                   # Sprint, burndown
│   │   ├── Notifications/             # In-app (SignalR) + email
│   │   ├── Dashboard/                 # Tổng hợp (chỉ đọc qua Contracts)
│   │   └── RiskAI/                    # Orchestrator gọi ai-service
│   └── Host/
│       ├── ProjectManagementAI.Api/   # Composition root
│       └── ProjectManagementAI.Worker/# BackgroundService (Outbox, deadline scan)
├── tests/
│   └── ArchitectureTests/             # NetArchTest - khóa hướng phụ thuộc
├── ai-service/                        # Python FastAPI
├── web/                               # React + TypeScript
├── infra/
│   ├── docker/docker-compose.yml
│   └── scripts/
├── docs/
│   └── architecture/
├── ProjectManagementAI.slnx
└── Directory.Build.props
```

Quy tắc reference giữa các project xem mục 2.2 trong tài liệu hướng dẫn.

## Bắt đầu

### Yêu cầu

- .NET SDK 10.0
- Node.js 20+, Python 3.12 (nếu làm FE/AI)
- Docker Desktop + Compose v2
- Git

### Chạy bằng Docker (khuyến dùng)

```bash
git clone https://github.com/Trunghieu220504/He-Thong-Quan-Ly-Cong-Viec-Tich-Hop-AI.git
cd He-Thong-Quan-Ly-Cong-Viec-Tich-Hop-AI/infra/docker
docker compose up -d --build

# Kiểm tra
curl http://localhost:5000/health/ready   # API
curl http://localhost:8000/health/ready   # AI service
# Mở http://localhost:3000 (web) hoặc http://localhost (qua nginx)
```

### Chạy backend local (không Docker)

```bash
dotnet restore
dotnet build
dotnet run --project src/Host/ProjectManagementAI.Api
```

### Chạy test

```bash
dotnet test tests/ArchitectureTests -c Release
dotnet test --filter "Category!=Integration"
```

## Quy ước làm việc

| Quy ước | Mô tả |
|---|---|
| Nhánh | `main` (deploy), `develop` (tích hợp), `feature/<ISSUE>-<module>-<tên>`, `fix/*`, `hotfix/*` |
| Commit | `feat(taskmanagement): ...`, `fix(identity): ...`, `chore(infra): ...` |
| PR | Bắt buộc qua PR vào `develop`/`main`, tối thiểu 1 approve, CI phải xanh |
| CODEOWNERS | Mỗi module có owner riêng, tự động yêu cầu review |
| CI | `backend-test`, `frontend-test`, `ai-test`, `architecture-test`, `docker-build` là required checks |

Chi tiết GitHub workflow xem `.github/workflows/`.

## Thành viên

| # | Thành viên | Vai trò |
|---|---|---|
| 1 | Khuất Trung Hiếu | Nhóm trưởng & Kiến trúc |
| 2 | Hoàng Phi Hải | Backend nghiệp vụ |
| 3 | Nguyễn Đức Phương | Backend & AI |
| 4 | Nguyễn Trọng Bảo Anh | Frontend |
| 5 | Đào Xuân Hòa | Testing, CI/CD & Deploy |

# AgentsBaoAnh.md – Hướng dẫn cho AI trợ lý code (dành cho Nguyễn Trọng Bảo Anh)

> Dùng làm ngữ cảnh cho AI (Claude Code, Copilot, Cursor…) trên máy của Bảo Anh. AI phải đọc hết trước khi sửa code.
> Người chịu trách nhiệm là Bảo Anh, không phải AI. Trả lời tiếng Việt, ngắn gọn, từng bước.
> Vai trò: **Frontend (`web/`)**. Không sửa backend, `ai-service`, CI của người khác.

## 0. BẮT BUỘC làm đầu tiên

```powershell
git fetch origin
git switch -c feature/T4-web-kanban origin/develop     # luôn từ origin/develop mới nhất
cd web
npm ci
```

Nhánh `develop` local có thể cũ. Khi cần xem code backend (DTO, endpoint), đọc từ `origin/develop`:
`git show origin/develop:<đường-dẫn-file>`.

## 1. Dự án

"Hệ Thống Quản Lý Công Việc Tích Hợp AI". Backend .NET 10 Modular Monolith (Host `http://localhost:5046`), PostgreSQL, `ai-service` FastAPI (dự đoán rủi ro, mock luôn trả `low`), và `web/` (phần của Bảo Anh).

Luồng nghiệm thu Tuần 4: **Đăng nhập → danh sách project → vào project (Kanban) → tạo task, gán người → kéo thả đổi trạng thái (lưu backend) → xem rủi ro AI**.

## 2. Hiện trạng `web/` (đã kiểm chứng trên `origin/develop`)

```text
web/
  package.json, package-lock.json, vite.config.ts, index.html
  tsconfig.json, tsconfig.app.json, tsconfig.node.json, .oxlintrc.json, .gitignore
  src/main.tsx         QueryClientProvider (TanStack Query) bọc <App />
  src/App.tsx          khung mẫu: DndContext + nút đếm (chưa gọi API)
  src/stores/counterStore.ts   ví dụ Zustand (bỏ khi có store thật)
```

- Stack: React 18.3, TypeScript 5.6, Vite 5.4, `@tanstack/react-query` 5, `zustand` 4, `@dnd-kit/core|sortable|utilities`. Lint: **oxlint** (`npm run lint`). Build: `npm run build` = `tsc -b && vite build`.
- **Chưa có**: router, API client, types dùng chung, `.env.example`, trang Login/Projects/Kanban. Đây chính là việc của Bảo Anh.
- Dev server cố định **cổng 3000** (`strictPort: true`; cổng bận thì báo lỗi, không tự đổi). Backend CORS phải cho phép `http://localhost:3000` (không phải 5173).
- Backend **chưa có CORS** cho đến khi Hiếu merge (ngày 2). Trước đó gọi API từ trình duyệt sẽ bị chặn → dùng mock (mục 7).

Quy tắc TypeScript của dự án (build sẽ đỏ nếu vi phạm):

- `verbatimModuleSyntax`: import kiểu phải dùng `import type { ... }`.
- `noUnusedLocals`, `noUnusedParameters`: không để biến/tham số thừa.
- `erasableSyntaxOnly`: **không dùng `enum`** và parameter properties; dùng union type (`type TaskStatus = 'Todo' | 'InProgress' | 'Done'`).
- `allowImportingTsExtensions`: import file `.tsx` có thể ghi đuôi, theo phong cách hiện có (`./App.tsx`).
- oxlint: `react/rules-of-hooks` = error; `react/only-export-components` = warn.

## 3. Hợp đồng API backend (nguồn sự thật: code backend)

Quy ước chung: JSON **camelCase**, mọi endpoint nghiệp vụ cần header `Authorization: Bearer <accessToken>`. Lỗi trả dạng ProblemDetails; thông báo người dùng nằm trong trường `detail`.

| Mã | Ý nghĩa |
|---|---|
| 400 | sai dữ liệu |
| 401 | chưa đăng nhập / token hết hạn / sai mật khẩu (ở login) |
| 403 | không phải member / không đủ quyền |
| 404 | không tìm thấy |
| 409 | trùng / sai trạng thái |
| 503 | AI service không khả dụng (chỉ endpoint risk) |

### 3.1. Đã có và chạy được

**Auth** (`/api/auth`, không cần token)

- `POST /api/auth/register` body `{ email, password, displayName }` → **201** `{ id }`. Ràng buộc: email ≤254; password 8–72 ký tự; displayName 2–100.
- `POST /api/auth/login` body `{ email, password }` → 200 `{ accessToken, accessTokenExpiresAtUtc, refreshToken, refreshTokenExpiresAtUtc }`. Sai thông tin → 401.
- `POST /api/auth/refresh` body `{ refreshToken }` → 200 cùng dạng token mới. Access token sống 15 phút, refresh token 7 ngày. Token dùng cho API là **accessToken** (gửi refreshToken làm Bearer sẽ bị từ chối).

**Projects** (`/api/projects`, cần token)

- `POST /api/projects` body `{ name, description? }` → 201 `{ id }` (name ≤200, description ≤2000).
- `GET /api/projects?page=1&pageSize=20` → `{ items: ProjectSummary[], page, pageSize, totalCount }` với `ProjectSummary = { id, name, description, ownerId, myRole, memberCount, createdAt }`.
- `GET /api/projects/{id}` → `{ id, name, description, ownerId, myRole, createdAt, updatedAt, members: [{ userId, role, joinedAt }] }`. **Hiện members chỉ có `userId`, chưa có tên.**
- `POST /api/projects/{id}/members` body `{ userId, role }` → 201. Role: `Admin | Member | Viewer`. Chỉ Owner/Admin thêm được.
- Vai trò: `Owner | Admin | Member | Viewer`. Viewer chỉ xem.

### 3.2. Sẽ có trong Tuần 4 (đang do người khác làm; dùng mock tới khi merge)

| API | Người làm | Ghi chú |
|---|---|---|
| `displayName` trong `members[]` của `GET /api/projects/{id}` | Hải | để hiển thị tên ở ô chọn assignee |
| `POST /api/projects/{projectId}/tasks` → 201 | Hiếu | body gợi ý `{ title, description?, priority, assigneeId?, dueDate? }` |
| `GET /api/projects/{projectId}/tasks` | Hiếu | lọc theo status, phân trang |
| `GET /api/tasks/{taskId}` | Hiếu | |
| `PATCH /api/tasks/{taskId}/status` | Hiếu | body `{ status }` |
| `PATCH /api/tasks/{taskId}/assignee` | Hiếu | body `{ assigneeId }` |
| `GET /api/tasks/{taskId}/risk` | Phương | `{ taskId, risk, source: "ai-service" }`; 503 nếu AI lỗi |
| Sprint (`/api/projects/{id}/sprints`, `/api/sprints/{id}/...`) | Hải | Tuần 4 chưa bắt buộc có UI |

**Trước khi gọi API thật cho Task/Risk: hỏi Hiếu/Phương hoặc xem code trên `origin/develop` để lấy đúng tên trường**; các body ở bảng trên là gợi ý, chưa chốt.

Giá trị chốt:

```text
Task status:    Todo | InProgress | Done        (UI hiển thị "In Progress", GỬI API là "InProgress")
Task priority:  Low | Medium | High
Risk:           low | medium | high             (chữ thường)
Id:             Guid (chuỗi)
```

## 4. Việc của Bảo Anh Tuần 4 (PMS-130, nhánh `feature/T4-web-kanban`)

Thứ tự khuyến nghị (mỗi bước một commit nhỏ, build xanh):

1. **Nền tảng**: cài `react-router-dom`; tạo `src/api/` (client dùng `fetch`, tự gắn Bearer, đọc `detail` từ lỗi), `src/types/`; base URL từ `import.meta.env.VITE_API_URL` (mặc định `http://localhost:5046`), thêm `web/.env.example`, **không commit `.env`**.
2. **Login**: form email/mật khẩu → lưu token (Zustand + persist hoặc localStorage), route bảo vệ, 401 → về trang Login; refresh trang vẫn giữ đăng nhập. Tự gọi `/api/auth/refresh` khi access token hết hạn (hoặc ghi rõ giới hạn nếu chưa làm).
3. **Danh sách project** (`useQuery`) + tạo project; trang chi tiết project.
4. **Kanban 3 cột** Todo / InProgress / Done bằng `@dnd-kit`. Kéo thả: cập nhật giao diện tạm → `PATCH status` → thành công thì invalidate query; lỗi thì **hoàn tác + báo lỗi**.
5. **Form tạo task** + chọn assignee (từ members của project) + hiển thị **risk**; 503 → "Chưa có dự đoán", không làm hỏng board.
6. Mọi màn hình có **loading, empty, error state**.

Tiêu chí hoàn thành: login nhận JWT; project list từ backend; tạo task từ UI; kéo task giữa các cột và backend lưu; refresh vẫn đúng dữ liệu; hiển thị risk; `npm ci`, `npm run lint`, `npm run build` đều xanh; PR merge vào `develop`.

## 5. Cấu trúc thư mục gợi ý

```text
web/src/
  api/        client.ts, auth.ts, projects.ts, tasks.ts, risk.ts
  types/      auth.ts, project.ts, task.ts
  stores/     authStore.ts           (thay counterStore.ts)
  pages/      LoginPage.tsx, ProjectsPage.tsx, ProjectBoardPage.tsx
  components/ KanbanBoard.tsx, KanbanColumn.tsx, TaskCard.tsx, CreateTaskForm.tsx
  main.tsx, App.tsx
```

Giữ phong cách code hiện có: không dấu chấm phẩy, nháy đơn, thụt 2 space, comment tiếng Việt ngắn khi cần.

Route gợi ý: `/login`, `/projects`, `/projects/:projectId` (Kanban). Chưa đăng nhập vào `/projects*` thì chuyển về `/login`; đã đăng nhập vào `/login` thì chuyển về `/projects`.

## 6. Mẹo kỹ thuật cho AI

- Server state (project, task, risk) dùng **TanStack Query**; chỉ dùng Zustand cho auth/UI state. Sau mutation gọi `queryClient.invalidateQueries`.
- Phân quyền giao diện theo `myRole` của project: **Viewer** chỉ xem (ẩn/khóa nút tạo task, kéo thả, đổi assignee); Owner/Admin/Member được thao tác. Backend vẫn trả 403 nếu vi phạm, UI phải hiện thông báo từ `detail`.
- Lưu token ở localStorage chấp nhận được cho đồ án; không ghi token ra console, không đưa vào URL.
- `react-router-dom`: React hiện là 18.3, dùng phiên bản tương thích (v6 hoặc v7) và ghi rõ phiên bản đã cài vào PR.
- Kanban: `DndContext` + `onDragEnd`; xác định cột đích từ `over.id`; chỉ gọi API khi cột thay đổi. Dùng optimistic update bằng `onMutate` + `onError` để rollback.
- Không lưu mật khẩu; không `console.log` token; không hard-code URL/secret.
- Id là Guid chuỗi, ngày là ISO string.

## 7. Làm việc khi backend chưa sẵn sàng (ngày 1–2)

- Tách `api/` thành hàm có kiểu rõ ràng; viết bản mock (dữ liệu giả trong cùng interface) và bật bằng `VITE_USE_MOCK=true`. Khi API thật có (ngày 3–4), chỉ cần đổi cờ, không sửa giao diện.
- Khi chạy thật cần: Postgres (`docker compose -f infra/docker/docker-compose.yml up -d`) và Host:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:Jwt__SecretKey = "<chuoi-it-nhat-32-ky-tu>"
$env:ConnectionStrings__Default = "Host=localhost;Port=5432;Database=pmai;Username=pmai;Password=pmai_Tuan2_2026"
dotnet run --project src/Host/ProjectManagementAI.Api --launch-profile http   # http://localhost:5046
```

- **Dùng profile `http`**. Host có `UseHttpsRedirection`; profile `https` (cổng 7110) làm yêu cầu `http://localhost:5046` bị chuyển hướng sang https, trình duyệt sẽ lỗi CORS/preflight. `VITE_API_URL` phải là `http://localhost:5046`.
- Chuỗi kết nối đã có sẵn trong `appsettings.Development.json`; bắt buộc phải đặt `Jwt__SecretKey` (≥ 32 ký tự), thiếu thì Host không khởi động.

- Tạo tài khoản thử qua `POST /api/auth/register` (xem mục 3.1). Mật khẩu ≥ 8 ký tự.
- Bị CORS chặn trên trình duyệt = Host chưa có CORS/chưa cho `http://localhost:3000`: báo Hiếu, **không** sửa backend và **không** tắt bảo mật trình duyệt.
- Host cũ còn chiếm cổng 5046 khiến endpoint mới trả 404/405 → tắt tiến trình rồi chạy lại.

## 8. Kiểm tra trước khi mở PR (AI phải chạy thật và báo kết quả thật)

```powershell
cd web
npm ci
npm run lint
npm run build
npm run dev        # mở http://localhost:3000, thử tay luồng chính
```

- Thử tay: login, danh sách, tạo task, kéo thả, refresh trang, trạng thái lỗi (tắt backend) và trạng thái rỗng.
- Đính ảnh/GIF chụp màn hình vào PR.
- Không commit `node_modules/`, `dist/`, `.env`.

## 9. Git/GitHub/Jira

1. Nhánh `feature/T4-web-kanban` từ `origin/develop`; không code trực tiếp trên `develop`.
2. Commit nhỏ, rõ (`feat(web): add login page`). Chỉ `git add` file thuộc `web/`, không `git add .`.
3. Không commit thư mục `docs/`, secret, token.
4. Không thêm dòng `Co-Authored-By` của AI vào commit hay mô tả PR.
5. Bảo Anh tự push bằng GitHub của Bảo Anh; PR vào `develop`, reviewer Hiếu, CI xanh + 1 Approve, **không tự merge**.
6. Jira PMS-130: To Do → In Progress → In Review (dán link PR) → Done (chỉ sau merge). Card lớn nên tách sub-task: Router/API client/Login; Project list; Kanban; Form + risk.
7. Conflict (ví dụ `package.json`, `package-lock.json`): giữ thay đổi cả hai bên, `git fetch`, rebase/merge `origin/develop`, chạy lại `npm ci` và build. Không `reset --hard`/`push --force` khi chưa hỏi.

## 10. Cách AI làm việc với Bảo Anh

- Đọc file thật trong `web/` trước khi sửa; không bịa tên package/phiên bản. Thêm dependency mới chỉ khi cần (hiện cần thêm `react-router-dom`) và nói rõ lý do.
- Không mở rộng phạm vi: việc ngoài mục 4 phải hỏi Bảo Anh (Sprint UI, Dashboard, Notification là Tuần sau).
- Không sửa backend (Hiếu: Task/Host; Hải: Projects/Sprints; Phương: RiskAI) và `ai-service`/CI (Hòa). Cần đổi API thì báo Bảo Anh để trao đổi với người phụ trách.
- Không chạy lệnh phá hủy (`rm -rf`, xóa `node_modules` ngoài `web/`, reset git) khi chưa được đồng ý.
- Báo cáo thật: build/lint lỗi thì nói rõ nguyên nhân; không nói "xong" khi chưa chạy `npm run build` và thử giao diện.

## 11. Tài liệu tham chiếu

- `docs/KE_HOACH_TUAN4_V2_VA_HUONG_DAN_JIRA.md` (kế hoạch + contract chốt).
- `docs/HUONG_DAN_BAOANH_TASK11_KHUNG_VITE.md` (cách dựng khung Vite, quy ước cổng 3000).

# PMAI Web

## Dashboard W3 / PMS-160

- Đăng nhập, chọn **Bảng báo cáo tiến độ** trong sidebar hoặc **Tổng quan** trên bảng công việc.
- `/dashboard` cho phép chọn dự án (có phân trang); `/projects/:projectId/dashboard` là trang báo cáo riêng, có thể mở trực tiếp hoặc tải lại URL.
- Dữ liệu đọc từ `GET /api/projects/{projectId}/dashboard`: tổng task, trạng thái, quá hạn, tỷ lệ hoàn thành, bảng theo người (bao gồm chưa giao) và Sprint đang chạy. Số liệu được tổng hợp trên backend, không phụ thuộc giới hạn 100 task hoặc bộ lọc Kanban.
- Làm mới mỗi 30 giây, khi quay lại trang và sau khi tạo/sửa/chuyển trạng thái task. Có nút làm mới, trạng thái rỗng, chưa có Sprint, lỗi 403/404 và lỗi dịch vụ; lỗi refresh không hiển thị số liệu cũ như dữ liệu hiện tại.
- `VITE_USE_MOCK=false` dùng API thật theo `.env.example`. Khi bật mock, số liệu tính từ toàn bộ task demo của dự án, hiển thị nhãn demo và không giả lập một Sprint đang chạy.
- Trang W3 hiển thị tiến độ Sprint từ backend; chưa có giao diện tạo/bắt đầu/kết thúc Sprint.

Chạy `npm test` và `npm run build` trong thư mục `web`. Test xác minh hợp đồng API, tổng lớn hơn 100, dữ liệu rỗng, task chưa giao, Sprint rỗng/không có Sprint, route riêng và lỗi refresh. CI chạy lint, test và build.

## React + TypeScript + Vite

This template provides a minimal setup to get React working in Vite with HMR and some Oxlint rules.

Currently, two official plugins are available:

- [@vitejs/plugin-react](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react) uses [Oxc](https://oxc.rs)
- [@vitejs/plugin-react-swc](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react-swc) uses [SWC](https://swc.rs/)

## React Compiler

The React Compiler is not enabled on this template because of its impact on dev & build performances. To add it, see [this documentation](https://react.dev/learn/react-compiler/installation).

## Expanding the Oxlint configuration

If you are developing a production application, we recommend enabling type-aware lint rules by installing `oxlint-tsgolint` and editing `.oxlintrc.json`:

```json
{
  "$schema": "./node_modules/oxlint/configuration_schema.json",
  "plugins": ["react", "typescript", "oxc"],
  "options": {
    "typeAware": true
  },
  "rules": {
    "react/rules-of-hooks": "error",
    "react/only-export-components": ["warn", { "allowConstantExport": true }]
  }
}
```

See the [Oxlint rules documentation](https://oxc.rs/docs/guide/usage/linter/rules) for the full list of rules and categories.

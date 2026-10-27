"""Smoke test RiskAI qua Host thật: GET /api/tasks/{taskId}/risk.

Đi qua toàn bộ pipeline thật (routing, JWT, DI của các module, PostgreSQL, ai-service):
  đăng ký 2 user -> đăng nhập -> user A tạo dự án -> tạo task (hạn +3 ngày) -> chuyển InProgress
  -> GET risk: thành viên 200, người ngoài dự án 403, task không tồn tại 404, không token 401
  -> tắt ai-service rồi gọi lại: 503.

Cần chạy sẵn: PostgreSQL đã migrate, API (dotnet run) và ai-service (uvicorn, cổng 8000).
Chỉ dùng thư viện chuẩn của Python.

    python tests/smoke/riskai_smoke.py
    python tests/smoke/riskai_smoke.py --api http://localhost:5046 --ai http://localhost:8000
    python tests/smoke/riskai_smoke.py --stop-ai "docker compose -f infra/docker/docker-compose.yml stop ai-service"
    python tests/smoke/riskai_smoke.py --no-interactive   # bỏ qua bước 503
"""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
import time
import uuid
from datetime import datetime, timedelta, timezone
from urllib import error, request

ALLOWED_RISKS = {"low", "medium", "high"}
AI_UNAVAILABLE_TITLE = "Dịch vụ AI tạm thời không khả dụng"
PASSWORD = "Smoke@12345"


class SmokeError(Exception):
    """Lỗi khiến không thể chạy tiếp các bước sau."""


class Report:
    def __init__(self) -> None:
        self.rows: list[tuple[str, bool | None, str]] = []

    def check(self, name: str, ok: bool, detail: str = "") -> bool:
        self.rows.append((name, ok, detail))
        print(f"[{'PASS' if ok else 'FAIL'}] {name}" + (f" | {detail}" if detail else ""))
        return ok

    def skip(self, name: str, reason: str) -> None:
        self.rows.append((name, None, reason))
        print(f"[SKIP] {name} | {reason}")


def call(method: str, url: str, body: object = None, token: str | None = None, timeout: float = 15):
    """Gửi request JSON; trả (status, body). Lỗi HTTP vẫn trả status; không kết nối được thì ném SmokeError."""
    data = None if body is None else json.dumps(body).encode("utf-8")
    req = request.Request(url, data=data, method=method)
    req.add_header("Accept", "application/json")
    if data is not None:
        req.add_header("Content-Type", "application/json")
    if token:
        req.add_header("Authorization", f"Bearer {token}")
    try:
        with request.urlopen(req, timeout=timeout) as resp:
            return resp.status, _parse(resp.read())
    except error.HTTPError as exc:
        return exc.code, _parse(exc.read())
    except (error.URLError, OSError) as exc:
        raise SmokeError(f"Không kết nối được {url}: {getattr(exc, 'reason', exc)}") from exc


def _parse(raw: bytes):
    text = raw.decode("utf-8", errors="replace")
    if not text:
        return None
    try:
        return json.loads(text)
    except json.JSONDecodeError:
        return text


def _short(body: object) -> str:
    text = body if isinstance(body, str) else json.dumps(body, ensure_ascii=False)
    return text if len(text) <= 300 else text[:300] + "..."


def _is_up(url: str) -> bool:
    try:
        status, _ = call("GET", url, timeout=3)
        return status == 200
    except SmokeError:
        return False


def register_and_login(api: str, email: str, display_name: str) -> str:
    status, body = call("POST", f"{api}/api/auth/register",
                        {"email": email, "password": PASSWORD, "displayName": display_name})
    if status != 201:
        raise SmokeError(f"Đăng ký {email} thất bại: HTTP {status} {_short(body)}")

    status, body = call("POST", f"{api}/api/auth/login", {"email": email, "password": PASSWORD})
    if status != 200 or not isinstance(body, dict) or not body.get("accessToken"):
        raise SmokeError(f"Đăng nhập {email} thất bại: HTTP {status} {_short(body)}")
    return body["accessToken"]


def run(api: str, ai: str, args: argparse.Namespace, report: Report) -> None:
    if not _is_up(f"{ai}/health/ready"):
        raise SmokeError(f"ai-service chưa chạy ở {ai}. Chạy: cd ai-service && uvicorn app.main:app --port 8000")
    if not _is_up(f"{api}/health"):
        raise SmokeError(f"API chưa chạy ở {api}. Chạy: dotnet run --project src/Host/ProjectManagementAI.Api")

    suffix = datetime.now(timezone.utc).strftime("%Y%m%d%H%M%S") + "-" + uuid.uuid4().hex[:6]
    owner = register_and_login(api, f"smoke-owner-{suffix}@example.com", "Smoke Owner")
    outsider = register_and_login(api, f"smoke-outsider-{suffix}@example.com", "Smoke Outsider")
    report.check("Đăng ký + đăng nhập 2 user", True)

    status, body = call("POST", f"{api}/api/projects",
                        {"name": f"Smoke RiskAI {suffix}", "description": "Smoke test RiskAI"}, owner)
    if status != 201:
        raise SmokeError(f"Tạo dự án thất bại: HTTP {status} {_short(body)}")
    project_id = body["id"]
    report.check("Tạo dự án", True, project_id)

    due = (datetime.now(timezone.utc) + timedelta(days=3, hours=1)).isoformat(timespec="seconds")
    status, body = call("POST", f"{api}/api/projects/{project_id}/tasks",
                        {"title": "Smoke task RiskAI", "description": None, "priority": "Medium",
                         "assigneeId": None, "dueDate": due.replace("+00:00", "Z")}, owner)
    if status != 201:
        raise SmokeError(
            f"Tạo task thất bại: HTTP {status} {_short(body)}. "
            "Host đã có Task API thật chưa (PR TaskManagement đã merge vào develop chưa)?")
    task_id = body["id"]
    report.check("Tạo task thật qua Task API", True, f"{task_id}, hạn {due}")

    status, body = call("PATCH", f"{api}/api/tasks/{task_id}/status", {"status": "InProgress"}, owner)
    report.check("Chuyển task sang InProgress", status == 200, f"HTTP {status}")

    risk_url = f"{api}/api/tasks/{task_id}/risk"

    status, body = call("GET", risk_url, token=owner)
    ok = (status == 200 and isinstance(body, dict) and body.get("taskId") == task_id
          and body.get("risk") in ALLOWED_RISKS and body.get("source") == "ai-service")
    report.check("Thành viên dự án lấy risk -> 200", ok, f"HTTP {status} {_short(body)}")

    status, body = call("GET", risk_url, token=outsider)
    report.check("User không thuộc dự án -> 403", status == 403, f"HTTP {status}")

    status, body = call("GET", f"{api}/api/tasks/{uuid.uuid4()}/risk", token=owner)
    report.check("Task không tồn tại -> 404", status == 404, f"HTTP {status}")

    status, body = call("GET", risk_url)
    report.check("Không có token -> 401", status == 401, f"HTTP {status}")

    if args.stop_ai:
        print(f"\nTắt ai-service bằng lệnh: {args.stop_ai}")
        subprocess.run(args.stop_ai, shell=True, check=False)
    elif args.no_interactive:
        report.skip("ai-service tắt -> 503", "chạy với --no-interactive")
        return
    else:
        input("\n>>> Tắt ai-service (Ctrl+C ở cửa sổ đang chạy uvicorn) rồi nhấn Enter để kiểm tra 503...")

    deadline = time.monotonic() + 15
    while _is_up(f"{ai}/health/ready") and time.monotonic() < deadline:
        time.sleep(0.5)
    if _is_up(f"{ai}/health/ready"):
        report.check("ai-service tắt -> 503", False, f"ai-service vẫn đang chạy ở {ai}")
        return

    status, body = call("GET", risk_url, token=owner)
    ok = status == 503 and isinstance(body, dict) and body.get("title") == AI_UNAVAILABLE_TITLE
    report.check("ai-service tắt -> 503", ok, f"HTTP {status} {_short(body)}")


def summarize(report: Report) -> int:
    passed = sum(1 for _, ok, _ in report.rows if ok is True)
    failed = sum(1 for _, ok, _ in report.rows if ok is False)
    skipped = sum(1 for _, ok, _ in report.rows if ok is None)
    print(f"\nKết quả: {passed} PASS, {failed} FAIL, {skipped} SKIP")
    return 1 if failed else 0


def main(argv: list[str] | None = None) -> int:
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass

    parser = argparse.ArgumentParser(description="Smoke test GET /api/tasks/{taskId}/risk qua Host thật.")
    parser.add_argument("--api", default="http://localhost:5046", help="URL của API (mặc định %(default)s)")
    parser.add_argument("--ai", default="http://localhost:8000", help="URL của ai-service (mặc định %(default)s)")
    parser.add_argument("--stop-ai", metavar="LỆNH", help="lệnh shell để tắt ai-service cho bước 503 (thay cho bấm Enter)")
    parser.add_argument("--no-interactive", action="store_true", help="bỏ qua bước 503 nếu không có --stop-ai")
    args = parser.parse_args(argv)

    api, ai = args.api.rstrip("/"), args.ai.rstrip("/")
    print(f"Smoke test RiskAI | {datetime.now().isoformat(timespec='seconds')} | API {api} | ai-service {ai}\n")

    report = Report()
    try:
        run(api, ai, args, report)
    except SmokeError as exc:
        report.check("Chạy hết các bước", False, str(exc))
    except KeyboardInterrupt:
        report.check("Chạy hết các bước", False, "bị dừng bằng Ctrl+C")
    return summarize(report)


if __name__ == "__main__":
    sys.exit(main())

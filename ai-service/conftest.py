"""Cấu hình pytest cho ai-service.

Trước khi chạy test, luôn huấn luyện một mô hình mới vào thư mục tạm và trỏ MODEL_PATH tới đó:
test chạy được cả khi chưa có models/risk_model.joblib và luôn kiểm đúng code hiện tại (không dùng nhầm file cũ).
"""

from __future__ import annotations

import os
import shutil
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

_model_dir: Path | None = None


def pytest_configure(config):
    global _model_dir
    from scripts.train import build, save

    _model_dir = Path(tempfile.mkdtemp(prefix="risk-model-"))
    model_file = _model_dir / "risk_model.joblib"
    # Lưu cả khi không đạt cổng chất lượng để tests/test_model_quality.py báo lỗi cụ thể
    save(build(), model_file)
    os.environ["MODEL_PATH"] = str(model_file)


def pytest_unconfigure(config):
    if _model_dir is not None:
        shutil.rmtree(_model_dir, ignore_errors=True)

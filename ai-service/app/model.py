"""Nạp mô hình đã huấn luyện và dự đoán mức rủi ro.

File mô hình (joblib) do scripts/train.py tạo, gồm hai bộ phân loại nhị phân HistGradientBoosting
có ràng buộc đơn điệu: P(rủi ro >= medium) và P(rủi ro >= high). Mức rủi ro là mức "trung vị" của
phân phối dự đoán, nên chắc chắn đơn điệu theo từng đặc trưng (gần hạn hơn / tiến độ thấp hơn /
nhiều giờ hơn thì rủi ro không giảm).

Chỉ nạp file mô hình do chính scripts/train.py tạo ra: joblib dùng pickle, không nạp file lạ.
"""

from __future__ import annotations

import logging
import os
import threading
from dataclasses import dataclass
from pathlib import Path

import joblib
import numpy as np

from app.features import FEATURE_NAMES, build_features

logger = logging.getLogger(__name__)

MODEL_FORMAT = 1
DEFAULT_MODEL_PATH = Path(__file__).resolve().parents[1] / "models" / "risk_model.joblib"


def model_path() -> Path:
    """Đường dẫn file mô hình: biến môi trường MODEL_PATH, mặc định ai-service/models/risk_model.joblib."""
    return Path(os.environ.get("MODEL_PATH") or DEFAULT_MODEL_PATH)


@dataclass(frozen=True)
class Prediction:
    risk: str
    probability: float


class RiskModel:
    def __init__(self, payload: dict) -> None:
        if payload.get("format") != MODEL_FORMAT:
            raise ValueError(f"File mô hình có format {payload.get('format')!r}, cần {MODEL_FORMAT}.")
        if tuple(payload.get("feature_names", ())) != FEATURE_NAMES:
            raise ValueError("Danh sách đặc trưng trong file mô hình khác app/features.py; hãy train lại.")
        self.version: str = payload["model_version"]
        self.metrics: dict = payload.get("metrics", {})
        self._ge_medium = payload["models"]["ge_medium"]
        self._ge_high = payload["models"]["ge_high"]

    def predict_many(self, rows: list[list[float]]) -> list[Prediction]:
        x = np.asarray(rows, dtype=float)
        p_medium = self._ge_medium.predict_proba(x)[:, 1]
        p_high = np.minimum(self._ge_high.predict_proba(x)[:, 1], p_medium)

        results = []
        for medium, high in zip(p_medium, p_high):
            probabilities = {"low": 1.0 - medium, "medium": medium - high, "high": high}
            risk = "high" if high >= 0.5 else "medium" if medium >= 0.5 else "low"
            results.append(Prediction(risk=risk, probability=round(float(probabilities[risk]), 4)))
        return results

    def predict(
        self,
        estimated_hours: float | None,
        days_until_due: int | None,
        progress_percent: float | None,
    ) -> Prediction:
        return self.predict_many([build_features(estimated_hours, days_until_due, progress_percent)])[0]


class ModelStore:
    """Giữ mô hình đã nạp. Chưa có file thì lần gọi sau thử nạp lại, nên train xong không cần khởi động lại."""

    def __init__(self, path: Path | None = None) -> None:
        self._path = path
        self._model: RiskModel | None = None
        self._lock = threading.Lock()

    def get(self) -> RiskModel | None:
        if self._model is not None:
            return self._model
        with self._lock:
            if self._model is None:
                self._model = self._load()
            return self._model

    def _load(self) -> RiskModel | None:
        path = self._path or model_path()
        if not path.is_file():
            logger.warning("Chưa có file mô hình %s. Chạy: python scripts/train.py", path)
            return None
        try:
            model = RiskModel(joblib.load(path))
        except Exception:  # file hỏng hoặc khác phiên bản -> coi như chưa sẵn sàng, không làm sập service
            logger.exception("Không nạp được mô hình từ %s", path)
            return None
        logger.info("Đã nạp mô hình %s từ %s", model.version, path)
        return model

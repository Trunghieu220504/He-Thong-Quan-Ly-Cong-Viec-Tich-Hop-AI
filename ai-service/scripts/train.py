"""Huấn luyện mô hình rủi ro trễ hạn trên dữ liệu MÔ PHỎNG và lưu ra file joblib.

    cd ai-service
    python scripts/train.py                              # -> models/risk_model.joblib
    python scripts/train.py --output /tmp/risk_model.joblib

Chỉ lưu file khi đạt QUALITY_GATES trên tập kiểm tra cố định; không đạt thì thoát mã 1 (Docker build dừng).
File mô hình không commit vào git (xem .gitignore); Dockerfile và pytest tự chạy script này.
"""

from __future__ import annotations

import argparse
import sys
from datetime import datetime, timezone
from pathlib import Path

import joblib
import numpy as np
import sklearn
from sklearn.ensemble import HistGradientBoostingClassifier

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from app.features import FEATURE_NAMES, MONOTONIC_CST, build_features  # noqa: E402
from app.model import DEFAULT_MODEL_PATH, MODEL_FORMAT, RiskModel  # noqa: E402
from scripts.generate_dataset import (  # noqa: E402
    LABEL_NOISE,
    RISK_LEVELS,
    TEST_ROWS,
    TEST_SEED,
    TRAIN_ROWS,
    TRAIN_SEED,
    generate,
)

MODEL_VERSION = "hgb-ordinal-sim-1"

# Cổng chất lượng trên tập kiểm tra cố định (seed TEST_SEED). Nhãn có 5% nhiễu nên không thể đạt 100%.
QUALITY_GATES = {
    "accuracy": 0.90,
    "recall_high": 0.90,    # không được bỏ sót task rủi ro cao
    "recall_medium": 0.60,
}


def to_matrix(records: list[dict]) -> tuple[np.ndarray, np.ndarray]:
    x = np.asarray(
        [build_features(r["estimated_hours"], r["days_until_due"], r["progress_percent"]) for r in records],
        dtype=float,
    )
    y = np.asarray([RISK_LEVELS.index(r["risk"]) for r in records])
    return x, y


def fit_binary(x: np.ndarray, y: np.ndarray) -> HistGradientBoostingClassifier:
    model = HistGradientBoostingClassifier(
        monotonic_cst=list(MONOTONIC_CST),
        max_iter=200,
        learning_rate=0.1,
        random_state=TRAIN_SEED,
    )
    return model.fit(x, y)


def evaluate(model: RiskModel, records: list[dict]) -> dict:
    """accuracy và recall từng mức trên `records`."""
    x, y = to_matrix(records)
    predicted = np.asarray([RISK_LEVELS.index(p.risk) for p in model.predict_many(x.tolist())])
    metrics = {"accuracy": round(float(np.mean(predicted == y)), 4)}
    for i, level in enumerate(RISK_LEVELS):
        metrics[f"recall_{level}"] = round(float(np.mean(predicted[y == i] == i)), 4)
    return metrics


def failed_gates(metrics: dict) -> list[str]:
    return [f"{name} = {metrics[name]} < {minimum}" for name, minimum in QUALITY_GATES.items() if metrics[name] < minimum]


def build() -> dict:
    """Huấn luyện trên dữ liệu mô phỏng (seed TRAIN_SEED) và đánh giá trên tập kiểm tra cố định."""
    x_train, y_train = to_matrix(generate(TRAIN_ROWS, TRAIN_SEED))
    payload = {
        "format": MODEL_FORMAT,
        "model_version": MODEL_VERSION,
        "feature_names": list(FEATURE_NAMES),
        "classes": list(RISK_LEVELS),
        "models": {
            "ge_medium": fit_binary(x_train, (y_train >= 1).astype(int)),
            "ge_high": fit_binary(x_train, (y_train >= 2).astype(int)),
        },
        "sklearn_version": sklearn.__version__,
        "trained_at_utc": datetime.now(timezone.utc).isoformat(timespec="seconds"),
    }
    payload["metrics"] = {
        **evaluate(RiskModel(payload), generate(TEST_ROWS, TEST_SEED)),
        "train_rows": TRAIN_ROWS,
        "train_seed": TRAIN_SEED,
        "test_rows": TEST_ROWS,
        "test_seed": TEST_SEED,
        "label_noise": LABEL_NOISE,
        "data": "simulated",
    }
    return payload


def save(payload: dict, output: Path) -> None:
    output.parent.mkdir(parents=True, exist_ok=True)
    joblib.dump(payload, output)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Huấn luyện mô hình rủi ro trễ hạn (dữ liệu mô phỏng).")
    parser.add_argument("--output", type=Path, default=DEFAULT_MODEL_PATH)
    args = parser.parse_args(argv)

    payload = build()
    metrics = payload["metrics"]
    print(f"Mô hình {MODEL_VERSION}, dữ liệu MÔ PHỎNG: {TRAIN_ROWS} dòng huấn luyện (seed {TRAIN_SEED}), "
          f"{TEST_ROWS} dòng kiểm tra cố định (seed {TEST_SEED})")
    print("Kết quả trên tập kiểm tra: " + ", ".join(
        f"{name} {metrics[name]:.4f}" for name in ("accuracy", "recall_low", "recall_medium", "recall_high")))

    failures = failed_gates(metrics)
    if failures:
        print("KHÔNG ĐẠT cổng chất lượng, không lưu mô hình: " + "; ".join(failures), file=sys.stderr)
        return 1

    save(payload, args.output)
    print(f"Đạt cổng chất lượng {QUALITY_GATES} -> đã lưu {args.output}")
    return 0


if __name__ == "__main__":
    # Windows terminals may default to a legacy encoding; CLI messages are UTF-8.
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8")
    sys.exit(main())

"""Sinh dữ liệu MÔ PHỎNG để huấn luyện mô hình rủi ro trễ hạn.

Nhãn được gán bằng quy tắc viết trong risk_by_rule() bên dưới, cộng thêm một ít nhiễu.
Đây là giả định của nhóm, KHÔNG phải hành vi thật của dự án: độ chính xác của mô hình chỉ cho biết
mô hình học lại quy tắc này tốt đến đâu.

    cd ai-service
    python scripts/generate_dataset.py                    # -> data/simulated_tasks.csv
    python scripts/generate_dataset.py --rows 1000 --seed 7 --output /tmp/tasks.csv
"""

from __future__ import annotations

import argparse
import csv
import math
import sys
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from app.features import pressure, remaining_hours  # noqa: E402

RISK_LEVELS = ("low", "medium", "high")

TRAIN_SEED = 42
TEST_SEED = 2026  # tập kiểm tra cố định, khác seed huấn luyện
TRAIN_ROWS = 6000
TEST_ROWS = 2000

# Ngưỡng của quy tắc gán nhãn: việc còn lại chiếm >= 40% quỹ giờ tới hạn thì cần chú ý, >= 100% thì không kịp
MEDIUM_PRESSURE = 0.4
HIGH_PRESSURE = 1.0
# Tỉ lệ mẫu bị dịch nhãn một mức, mô phỏng ước lượng sai hoặc sự cố ngoài kế hoạch
LABEL_NOISE = 0.05


def risk_by_rule(
    estimated_hours: float | None,
    days_until_due: int | None,
    progress_percent: float | None,
) -> str:
    """Quy tắc gán nhãn của dữ liệu mô phỏng (chưa có nhiễu).

    1. Đã xong (tiến độ >= 100%)                  -> low
    2. Không có hạn chót                          -> low   (không thể trễ hạn)
    3. Đã quá hạn mà chưa xong                    -> high
    4. pressure = giờ còn phải làm / giờ còn lại tới hạn (6 giờ/ngày, thiếu giờ ước tính = 8 giờ)
       pressure >= 1.0 -> high; >= 0.4 -> medium; còn lại -> low
    """
    progress = 0.0 if progress_percent is None else float(progress_percent)
    if progress >= 100:
        return "low"
    if days_until_due is None:
        return "low"
    if days_until_due < 0:
        return "high"
    value = pressure(remaining_hours(estimated_hours, progress), days_until_due)
    if value >= HIGH_PRESSURE:
        return "high"
    if value >= MEDIUM_PRESSURE:
        return "medium"
    return "low"


def generate(rows: int, seed: int, label_noise: float = LABEL_NOISE) -> list[dict]:
    """Sinh `rows` task mô phỏng; cùng seed luôn cho cùng dữ liệu."""
    rng = np.random.default_rng(seed)

    # Giờ ước tính: phần lớn 3..40 giờ, trung vị 12 giờ, làm tròn 0,5 giờ
    hours = np.clip(np.round(rng.lognormal(mean=math.log(12), sigma=0.9, size=rows) * 2) / 2, 0.5, 200)
    hours_missing = rng.random(rows) < 0.30  # .NET gửi null khi task chưa có giờ ước tính

    # Tiến độ: Todo (0), InProgress khi chỉ biết trạng thái (50), Done (100), còn lại nhập tay 0..99
    kind = rng.choice(4, size=rows, p=[0.30, 0.15, 0.10, 0.45])
    progress = np.select(
        [kind == 0, kind == 1, kind == 2],
        [0, 50, 100],
        default=rng.integers(0, 20, size=rows) * 5,
    )

    # Hạn chót: 10% không có; còn lại 12% đã quá hạn (-5..-1), 50% sắp tới (0..7), 38% còn xa (8..45 ngày)
    days_missing = rng.random(rows) < 0.10
    bucket = rng.random(rows)
    days = np.select(
        [bucket < 0.12, bucket < 0.62],
        [rng.integers(-5, 0, size=rows), rng.integers(0, 8, size=rows)],
        default=rng.integers(8, 46, size=rows),
    )

    noise_draw = rng.random(rows)
    noise_step = rng.choice([-1, 1], size=rows)

    records = []
    for i in range(rows):
        estimated = None if hours_missing[i] else float(hours[i])
        due = None if days_missing[i] else int(days[i])
        prog = float(progress[i])
        level = RISK_LEVELS.index(risk_by_rule(estimated, due, prog))
        if noise_draw[i] < label_noise:
            level = min(max(level + int(noise_step[i]), 0), len(RISK_LEVELS) - 1)
        records.append({
            "estimated_hours": estimated,
            "days_until_due": due,
            "progress_percent": prog,
            "risk": RISK_LEVELS[level],
        })
    return records


def write_csv(records: list[dict], output: Path) -> None:
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("w", newline="", encoding="utf-8") as file:
        writer = csv.DictWriter(file, fieldnames=["estimated_hours", "days_until_due", "progress_percent", "risk"])
        writer.writeheader()
        writer.writerows(records)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Sinh dữ liệu mô phỏng cho mô hình rủi ro trễ hạn.")
    parser.add_argument("--rows", type=int, default=TRAIN_ROWS)
    parser.add_argument("--seed", type=int, default=TRAIN_SEED)
    parser.add_argument("--output", type=Path, default=ROOT / "data" / "simulated_tasks.csv")
    args = parser.parse_args(argv)

    records = generate(args.rows, args.seed)
    write_csv(records, args.output)
    counts = {level: sum(r["risk"] == level for r in records) for level in RISK_LEVELS}
    print(f"Đã sinh {len(records)} dòng (seed {args.seed}) -> {args.output} | {counts}")
    return 0


if __name__ == "__main__":
    # Windows terminals may default to a legacy encoding; CLI messages are UTF-8.
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8")
    sys.exit(main())

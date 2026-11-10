"""Đặc trưng đầu vào của mô hình rủi ro trễ hạn.

Dùng chung cho huấn luyện (scripts/train.py) và dự đoán (app/main.py) để thứ tự cột luôn giống nhau.
Giá trị thiếu (None) được đổi thành NaN; HistGradientBoosting tự học cách xử lý NaN.
"""

from __future__ import annotations

import math

# Task chưa có giờ ước tính được coi như một ngày công.
DEFAULT_ESTIMATED_HOURS = 8.0
# Số giờ thực tế có thể dành cho một task trong một ngày.
FOCUS_HOURS_PER_DAY = 6.0

FEATURE_NAMES = (
    "estimated_hours",
    "days_until_due",
    "progress_percent",
    "remaining_hours",
    "pressure",
)

# Ràng buộc đơn điệu, cùng thứ tự với FEATURE_NAMES:
# +1 = giá trị tăng thì rủi ro không giảm, -1 = giá trị tăng thì rủi ro không tăng.
MONOTONIC_CST = (1, -1, -1, 1, 1)


def remaining_hours(estimated_hours: float | None, progress_percent: float | None) -> float:
    """Số giờ còn phải làm. Thiếu giờ ước tính -> mặc định 8 giờ; thiếu tiến độ -> coi như 0%."""
    hours = DEFAULT_ESTIMATED_HOURS if estimated_hours is None else float(estimated_hours)
    progress = 0.0 if progress_percent is None else min(max(float(progress_percent), 0.0), 100.0)
    return hours * (1.0 - progress / 100.0)


def pressure(remaining: float, days_until_due: int | None) -> float:
    """Tỉ lệ giờ còn phải làm / giờ còn lại tới hạn. Không có hạn -> NaN; quá hạn tính như còn nửa ngày."""
    if days_until_due is None:
        return math.nan
    available_hours = (max(days_until_due, 0) + 0.5) * FOCUS_HOURS_PER_DAY
    return remaining / available_hours


def build_features(
    estimated_hours: float | None,
    days_until_due: int | None,
    progress_percent: float | None,
) -> list[float]:
    """Một dòng đặc trưng theo đúng thứ tự FEATURE_NAMES."""
    remaining = remaining_hours(estimated_hours, progress_percent)
    return [
        math.nan if estimated_hours is None else float(estimated_hours),
        math.nan if days_until_due is None else float(days_until_due),
        math.nan if progress_percent is None else float(progress_percent),
        remaining,
        pressure(remaining, days_until_due),
    ]

"""Cổng chất lượng của mô hình (M2): độ chính xác trên tập kiểm tra cố định và tính đơn điệu.

Mô hình được conftest.py huấn luyện mới trước khi chạy test, trên dữ liệu MÔ PHỎNG.
"""

import pytest

from app.features import build_features
from app.model import ModelStore
from scripts.generate_dataset import TEST_ROWS, TEST_SEED, generate, risk_by_rule
from scripts.train import QUALITY_GATES, evaluate

LEVEL = {"low": 0, "medium": 1, "high": 2}
HOURS = [None, 0.5, 2, 4, 8, 16, 40, 120]
PROGRESS = [0, 10, 25, 50, 75, 90, 100]
DAYS = list(range(-10, 46))


@pytest.fixture(scope="module")
def model():
    loaded = ModelStore().get()
    assert loaded is not None, "conftest.py phải huấn luyện mô hình trước khi chạy test"
    return loaded


def levels(model, rows):
    return [LEVEL[p.risk] for p in model.predict_many(rows)]


def test_quality_gates_on_fixed_test_set(model):
    metrics = evaluate(model, generate(TEST_ROWS, TEST_SEED))
    failures = [f"{name} = {metrics[name]} < {minimum}" for name, minimum in QUALITY_GATES.items()
                if metrics[name] < minimum]
    assert not failures, f"Không đạt cổng chất lượng: {failures}"


@pytest.mark.parametrize("hours", [None, 2, 8, 40])
def test_overdue_with_no_progress_is_high(model, hours):
    assert model.predict(hours, -2, 0).risk == "high"


@pytest.mark.parametrize("hours", [None, 8, 40])
def test_far_deadline_and_almost_done_is_low(model, hours):
    assert model.predict(hours, 30, 90).risk == "low"


def test_done_task_is_low_even_if_overdue(model):
    assert model.predict(40, -3, 100).risk == "low"


def test_task_without_due_date_is_low(model):
    assert model.predict(40, None, 0).risk == "low"


def test_risk_never_decreases_as_deadline_approaches(model):
    for hours in HOURS:
        for progress in PROGRESS:
            far_to_overdue = [build_features(hours, days, progress) for days in reversed(DAYS)]
            result = levels(model, far_to_overdue)
            assert result == sorted(result), f"giờ={hours}, tiến độ={progress}%: {result}"


def test_risk_never_increases_with_more_progress(model):
    for hours in HOURS:
        for days in DAYS:
            rows = [build_features(hours, days, progress) for progress in range(0, 101, 5)]
            result = levels(model, rows)
            assert result == sorted(result, reverse=True), f"giờ={hours}, còn {days} ngày: {result}"


def test_risk_never_decreases_with_more_estimated_hours(model):
    for days in DAYS:
        for progress in PROGRESS:
            rows = [build_features(hours, days, progress) for hours in HOURS[1:]]
            result = levels(model, rows)
            assert result == sorted(result), f"còn {days} ngày, tiến độ={progress}%: {result}"


def test_probability_is_between_0_and_1(model):
    rows = [build_features(h, d, p) for h in HOURS for d in DAYS[::5] for p in PROGRESS]
    assert all(0 <= prediction.probability <= 1 for prediction in model.predict_many(rows))


@pytest.mark.parametrize(
    ("hours", "days", "progress", "expected"),
    [
        (8, 3, 20, "low"),         # còn 6,4 giờ / 21 giờ tới hạn
        (12, 2, 50, "medium"),     # còn 6 giờ / 15 giờ = 0,4
        (None, 0, 0, "high"),      # 8 giờ mặc định / 3 giờ còn lại hôm nay
        (40, -1, 99, "high"),      # quá hạn mà chưa xong
        (40, -1, 100, "low"),      # đã xong
        (None, None, 0, "low"),    # không có hạn chót
    ],
)
def test_labeling_rule_examples(hours, days, progress, expected):
    assert risk_by_rule(hours, days, progress) == expected


def test_simulated_dataset_is_reproducible():
    assert generate(200, 7) == generate(200, 7)
    assert generate(200, 7) != generate(200, 8)

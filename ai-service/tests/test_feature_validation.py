import pytest
from pydantic import ValidationError
from app.schemas import PredictDelayRequest

@pytest.mark.parametrize("field", ["estimated_hours", "progress_percent"])
@pytest.mark.parametrize("value", [float("nan"), float("inf"), float("-inf")])
def test_nonfinite_features_are_rejected(field, value):
    with pytest.raises(ValidationError):
        PredictDelayRequest(**{field: value})

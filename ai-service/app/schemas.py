from typing import Literal

from pydantic import BaseModel, Field

RiskLevel = Literal["low", "medium", "high"]


class PredictDelayRequest(BaseModel):
    """Đặc trưng của một task. Tất cả đều tùy chọn vì đây mới là bản mock."""

    task_id: str | None = None
    estimated_hours: float | None = Field(default=None, ge=0)
    days_until_due: int | None = None
    progress_percent: float | None = Field(default=None, ge=0, le=100)


class PredictDelayResponse(BaseModel):
    risk: RiskLevel

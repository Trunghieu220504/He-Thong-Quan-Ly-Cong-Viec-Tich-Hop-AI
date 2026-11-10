from typing import Literal

from pydantic import BaseModel, ConfigDict, Field

RiskLevel = Literal["low", "medium", "high"]


class PredictDelayRequest(BaseModel):
    """Đặc trưng của một task (snake_case, khớp RiskAI .NET). Trường nào thiếu thì mô hình tự xử lý."""

    task_id: str | None = None
    estimated_hours: float | None = Field(default=None, ge=0, allow_inf_nan=False)
    days_until_due: int | None = None
    progress_percent: float | None = Field(default=None, ge=0, le=100, allow_inf_nan=False)


class PredictDelayResponse(BaseModel):
    model_config = ConfigDict(protected_namespaces=())

    risk: RiskLevel
    probability: float = Field(ge=0, le=1, description="Xác suất mô hình gán cho mức rủi ro trả về")
    model_version: str = Field(description="Phiên bản mô hình; hậu tố 'sim' = huấn luyện trên dữ liệu mô phỏng")

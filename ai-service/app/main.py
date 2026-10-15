from fastapi import FastAPI

from app.schemas import PredictDelayRequest, PredictDelayResponse

app = FastAPI(title="Project Management AI - AI Service", version="0.1.0")


@app.get("/health/ready")
def health_ready() -> dict[str, str]:
    return {"status": "ready"}


@app.post("/predict-delay", response_model=PredictDelayResponse)
def predict_delay(request: PredictDelayRequest) -> PredictDelayResponse:
    # MOCK: luôn trả "low". Mô hình scikit-learn thật sẽ làm ở tuần sau.
    return PredictDelayResponse(risk="low")

from contextlib import asynccontextmanager

from fastapi import FastAPI, HTTPException
from fastapi.responses import JSONResponse

from app.model import ModelStore
from app.schemas import PredictDelayRequest, PredictDelayResponse

store = ModelStore()


@asynccontextmanager
async def lifespan(_: FastAPI):
    store.get()  # nạp sẵn mô hình khi khởi động; chưa có file thì /health/ready trả 503
    yield


app = FastAPI(title="Project Management AI - AI Service", version="0.2.0", lifespan=lifespan)


@app.get("/health/ready")
def health_ready():
    if store.get() is None:
        return JSONResponse(status_code=503, content={"status": "not_ready", "model_loaded": False})
    return {"status": "ready", "model_loaded": True}


@app.post("/predict-delay", response_model=PredictDelayResponse)
def predict_delay(request: PredictDelayRequest) -> PredictDelayResponse:
    """Dự đoán rủi ro trễ hạn của một task. Chỉ đọc đặc trưng gửi lên, không sửa dữ liệu task."""
    model = store.get()
    if model is None:
        raise HTTPException(status_code=503, detail="Mô hình chưa được nạp. Chạy: python scripts/train.py")

    prediction = model.predict(request.estimated_hours, request.days_until_due, request.progress_percent)
    return PredictDelayResponse(
        risk=prediction.risk,
        probability=prediction.probability,
        model_version=model.version,
    )

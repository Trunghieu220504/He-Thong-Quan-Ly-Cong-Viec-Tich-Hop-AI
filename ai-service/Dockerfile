FROM python:3.12-slim

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    MODEL_PATH=/app/models/risk_model.joblib

WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY app ./app
COPY scripts ./scripts

# Huấn luyện mô hình ngay lúc build (dữ liệu mô phỏng, seed cố định); không đạt cổng chất lượng thì build dừng
RUN python scripts/train.py --output "$MODEL_PATH"

RUN useradd --create-home appuser
USER appuser

EXPOSE 8000

# /health/ready chỉ trả 200 khi mô hình đã được nạp
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD python -c "import urllib.request; urllib.request.urlopen('http://localhost:8000/health/ready')" || exit 1

CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]

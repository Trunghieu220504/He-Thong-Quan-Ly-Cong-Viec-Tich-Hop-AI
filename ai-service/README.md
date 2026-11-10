# ai-service — dự đoán rủi ro trễ hạn của task

FastAPI service được module RiskAI (.NET) gọi qua `POST /predict-delay`. Service chỉ đọc đặc trưng được gửi lên, không đọc hay sửa dữ liệu task.

> **Lưu ý: mô hình được huấn luyện trên dữ liệu MÔ PHỎNG.**
> Nhãn huấn luyện do quy tắc trong `scripts/generate_dataset.py` gán ra (hàm `risk_by_rule`), cộng 5% nhiễu.
> Độ chính xác bên dưới chỉ cho biết mô hình học lại quy tắc đó tốt đến đâu, **không** phải độ chính xác khi dự đoán dự án thật.
> Không trình bày kết quả này như "dự đoán chính xác". Kế hoạch: huấn luyện lại bằng dữ liệu thật xuất từ DB ở Tuần 6.

## API

`POST /predict-delay` — request giữ nguyên contract Tuần 4 (snake_case); trường nào thiếu cũng được:

```json
{ "task_id": "3fa85f64-...", "estimated_hours": 12.5, "days_until_due": 3, "progress_percent": 40 }
```

Response (chỉ thêm field so với Tuần 4; .NET bỏ qua field chưa dùng):

```json
{ "risk": "low", "probability": 0.9556, "model_version": "hgb-ordinal-sim-1" }
```

- `risk`: `low` | `medium` | `high`
- `probability`: xác suất (0..1) mô hình gán cho mức `risk` trả về
- `model_version`: hậu tố `sim` = huấn luyện trên dữ liệu mô phỏng

`GET /health/ready` — `200 {"status": "ready", "model_loaded": true}` chỉ khi đã nạp được mô hình; chưa có hoặc hỏng file mô hình thì `503 {"status": "not_ready", "model_loaded": false}`, và `/predict-delay` cũng trả 503 (RiskAI .NET chuyển thành 503 cho web).

## Mô hình

| Thành phần | Mô tả |
|---|---|
| Đặc trưng (`app/features.py`) | `estimated_hours`, `days_until_due`, `progress_percent`, `remaining_hours` = giờ ước tính × (1 − tiến độ), `pressure` = giờ còn phải làm / giờ còn lại tới hạn (6 giờ/ngày). Thiếu giờ ước tính thì tính 8 giờ; giá trị thiếu khác là NaN |
| Quy tắc gán nhãn (mô phỏng) | đã xong → low; không có hạn → low; quá hạn → high; `pressure` ≥ 1,0 → high, ≥ 0,4 → medium, còn lại → low |
| Dữ liệu | 6.000 task huấn luyện (seed 42), 2.000 task kiểm tra cố định (seed 2026), sinh lại y hệt mỗi lần |
| Thuật toán | Hai `HistGradientBoostingClassifier` cho P(rủi ro ≥ medium) và P(rủi ro ≥ high), có ràng buộc đơn điệu: gần hạn hơn, tiến độ thấp hơn hoặc nhiều giờ hơn thì rủi ro không giảm |
| Cổng chất lượng | trên tập kiểm tra cố định: accuracy ≥ 0,90; recall `high` ≥ 0,90; recall `medium` ≥ 0,60 (khai báo ở `QUALITY_GATES` trong `scripts/train.py`) |

File mô hình (`models/risk_model.joblib`) và dữ liệu (`data/`) **không commit**: sinh lại được bằng script, seed cố định. Chỉ nạp file mô hình do `scripts/train.py` tạo (joblib dùng pickle).

## Chạy trên máy

```bash
cd ai-service
python -m pip install -r requirements.txt -r requirements-dev.txt
python scripts/train.py                      # huấn luyện + kiểm cổng chất lượng -> models/risk_model.joblib
python -m uvicorn app.main:app --port 8000
```

- `python scripts/generate_dataset.py` ghi dữ liệu mô phỏng ra `data/simulated_tasks.csv` để xem.
- Biến môi trường `MODEL_PATH` đổi đường dẫn file mô hình.

## Test

```bash
cd ai-service
python -m pytest
```

`conftest.py` luôn huấn luyện một mô hình mới vào thư mục tạm trước khi chạy test, nên test chạy được khi chưa có `models/` và luôn kiểm đúng code hiện tại. Gồm:

- `tests/test_model_quality.py`: cổng chất lượng; quá hạn + 0% tiến độ → high; còn nhiều ngày + 90% → low; rủi ro đơn điệu theo hạn chót, tiến độ và giờ ước tính.
- `tests/test_dotnet_contract.py`: khoá contract với RiskAI .NET.
- `tests/test_api.py`: endpoint và hành vi khi chưa có mô hình.

## Docker

`Dockerfile` chạy `scripts/train.py` lúc build: không đạt cổng chất lượng thì build dừng. Container chỉ báo healthy khi `/health/ready` trả 200, tức là mô hình đã nạp.

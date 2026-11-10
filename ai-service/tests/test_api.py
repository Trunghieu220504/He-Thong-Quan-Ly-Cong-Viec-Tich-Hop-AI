from fastapi.testclient import TestClient

from app import main
from app.main import app
from app.model import ModelStore
from scripts.train import MODEL_VERSION

client = TestClient(app)


def test_health_ready_reports_model_loaded():
    response = client.get("/health/ready")
    assert response.status_code == 200
    assert response.json() == {"status": "ready", "model_loaded": True}


def test_predict_delay_returns_risk_probability_and_model_version():
    response = client.post(
        "/predict-delay",
        json={"task_id": "T-1", "estimated_hours": 8, "days_until_due": 3, "progress_percent": 20},
    )
    assert response.status_code == 200
    body = response.json()
    assert body["risk"] == "low"  # còn 6,4 giờ việc trong 21 giờ tới hạn
    assert 0 <= body["probability"] <= 1
    assert body["model_version"] == MODEL_VERSION


def test_predict_delay_accepts_empty_body():
    response = client.post("/predict-delay", json={})
    assert response.status_code == 200
    assert response.json()["risk"] == "low"  # không có hạn chót thì không thể trễ hạn


def test_predict_delay_rejects_invalid_progress():
    response = client.post("/predict-delay", json={"progress_percent": 150})
    assert response.status_code == 422


def test_predict_delay_rejects_negative_hours():
    response = client.post("/predict-delay", json={"estimated_hours": -1})
    assert response.status_code == 422


def test_without_model_file_service_is_not_ready(monkeypatch, tmp_path):
    monkeypatch.setattr(main, "store", ModelStore(tmp_path / "missing.joblib"))

    health = client.get("/health/ready")
    assert health.status_code == 503
    assert health.json() == {"status": "not_ready", "model_loaded": False}
    assert client.post("/predict-delay", json={}).status_code == 503


def test_corrupt_model_file_is_not_ready(monkeypatch, tmp_path):
    broken = tmp_path / "broken.joblib"
    broken.write_bytes(b"khong-phai-mo-hinh")
    monkeypatch.setattr(main, "store", ModelStore(broken))

    assert client.get("/health/ready").status_code == 503
    assert client.post("/predict-delay", json={}).status_code == 503


def test_scikit_learn_is_installed():
    import sklearn

    assert sklearn.__version__

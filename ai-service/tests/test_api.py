from fastapi.testclient import TestClient

from app.main import app

client = TestClient(app)


def test_health_ready_returns_200():
    response = client.get("/health/ready")
    assert response.status_code == 200
    assert response.json() == {"status": "ready"}


def test_predict_delay_returns_low_risk():
    response = client.post(
        "/predict-delay",
        json={"task_id": "T-1", "estimated_hours": 8, "days_until_due": 3, "progress_percent": 20},
    )
    assert response.status_code == 200
    assert response.json() == {"risk": "low"}


def test_predict_delay_accepts_empty_body():
    response = client.post("/predict-delay", json={})
    assert response.status_code == 200
    assert response.json() == {"risk": "low"}


def test_predict_delay_rejects_invalid_progress():
    response = client.post("/predict-delay", json={"progress_percent": 150})
    assert response.status_code == 422


def test_scikit_learn_is_installed():
    import sklearn

    assert sklearn.__version__

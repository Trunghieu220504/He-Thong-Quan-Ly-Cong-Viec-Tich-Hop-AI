"""Khoá hợp đồng giữa RiskAI .NET (HttpRiskAiClient) và ai-service.

Payload dưới đây đúng dạng JSON mà .NET gửi: snake_case, task_id là GUID dạng chuỗi,
days_until_due là số nguyên (âm khi đã quá hạn), estimated_hours có thể null.
Đổi schema ở app/schemas.py thì phải đổi cả RiskAI.Contracts.RiskAiRequest.
"""

from fastapi.testclient import TestClient

from app.main import app

client = TestClient(app)


def test_accepts_payload_sent_by_dotnet_client():
    response = client.post(
        "/predict-delay",
        json={
            "task_id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            "estimated_hours": None,
            "days_until_due": -2,
            "progress_percent": 50,
        },
    )

    assert response.status_code == 200
    assert response.json()["risk"] in {"low", "medium", "high"}


def test_days_until_due_must_be_integer():
    response = client.post("/predict-delay", json={"days_until_due": 2.5})

    assert response.status_code == 422

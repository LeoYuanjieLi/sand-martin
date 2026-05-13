import pytest
import respx
import httpx
import json
from sand_martin.server import (
    get_canvas_state,
    create_node,
    update_node,
    delete_node,
    connect_nodes,
    disconnect_nodes,
    HOST_URL
)

@pytest.mark.asyncio
@respx.mock
async def test_get_canvas_state():
    # Arrange
    expected_response = {"nodes": [{"id": "1", "name": "Test"}]}
    respx.get(f"{HOST_URL}/state").mock(return_value=httpx.Response(200, json=expected_response))

    # Act
    result = await get_canvas_state()

    # Assert
    assert json.loads(result) == expected_response
    assert respx.get(f"{HOST_URL}/state").called

@pytest.mark.asyncio
@respx.mock
async def test_create_node():
    # Arrange
    respx.post(f"{HOST_URL}/create").mock(return_value=httpx.Response(200, json={"status": "success", "id": "guid-123"}))

    # Act
    result = await create_node(type="Circle", name="My Circle", canvas_x=100, canvas_y=200)

    # Assert
    assert json.loads(result)["status"] == "success"
    assert json.loads(result)["id"] == "guid-123"
    
    request = respx.calls.last.request
    request_data = json.loads(request.content)
    assert request_data["type"] == "Circle"
    assert request_data["canvasX"] == 100

@pytest.mark.asyncio
@respx.mock
async def test_update_node():
    # Arrange
    node_id = "guid-123"
    respx.patch(f"{HOST_URL}/update/{node_id}").mock(return_value=httpx.Response(200, json={"status": "success"}))

    # Act
    result = await update_node(node_id=node_id, name="New Name", canvas_x=150)

    # Assert
    assert json.loads(result)["status"] == "success"
    
    request = respx.calls.last.request
    request_data = json.loads(request.content)
    assert request_data["name"] == "New Name"
    assert request_data["canvasX"] == 150
    assert "canvasY" not in request_data

@pytest.mark.asyncio
@respx.mock
async def test_delete_node():
    # Arrange
    node_id = "guid-123"
    respx.delete(f"{HOST_URL}/node/{node_id}").mock(return_value=httpx.Response(200, json={"status": "success"}))

    # Act
    result = await delete_node(node_id=node_id)

    # Assert
    assert json.loads(result)["status"] == "success"
    assert respx.delete(f"{HOST_URL}/node/{node_id}").called

@pytest.mark.asyncio
@respx.mock
async def test_connect_nodes():
    # Arrange
    respx.post(f"{HOST_URL}/connection").mock(return_value=httpx.Response(200, json={"status": "success"}))

    # Act
    result = await connect_nodes(source_id="src-1", target_id="tgt-1", source_output_index=1, target_input_index=2)

    # Assert
    assert json.loads(result)["status"] == "success"
    
    request = respx.calls.last.request
    request_data = json.loads(request.content)
    assert request_data["source_id"] == "src-1"
    assert request_data["target_id"] == "tgt-1"
    assert request_data["source_output_index"] == 1
    assert request_data["target_input_index"] == 2

@pytest.mark.asyncio
@respx.mock
async def test_disconnect_nodes():
    # Arrange
    respx.post(f"{HOST_URL}/disconnect").mock(return_value=httpx.Response(200, json={"status": "success"}))

    # Act
    result = await disconnect_nodes(source_id="src-1", target_id="tgt-1")

    # Assert
    assert json.loads(result)["status"] == "success"
    
    request = respx.calls.last.request
    request_data = json.loads(request.content)
    assert request_data["source_id"] == "src-1"
    assert request_data["target_id"] == "tgt-1"

@pytest.mark.asyncio
@respx.mock
async def test_make_request_error_handling():
    # Arrange
    respx.get(f"{HOST_URL}/state").mock(return_value=httpx.Response(500))

    # Act
    result = await get_canvas_state()

    # Assert
    response = json.loads(result)
    assert response["status"] == "error"
    assert "HTTP error" in response["message"]

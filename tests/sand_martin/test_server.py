import pytest
import respx
import httpx
import json
import os
from sand_martin.server import (
    get_canvas_state,
    create_node,
    create_script_node,
    update_node,
    delete_node,
    connect_nodes,
    disconnect_nodes,
    add_description,
    HOST_URL
)

@pytest.fixture(autouse=True)
def mock_auth_token(monkeypatch):
    """Set the SAND_MARTIN_TOKEN environment variable for all tests."""
    monkeypatch.setenv("SAND_MARTIN_TOKEN", "test-token-123456789012345678901234")

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
async def test_create_script_node_uses_rhino_template_and_validates():
    template_node_id = "template-guid"
    node_id = "script-guid"
    template = """public class Script_Instance : GH_ScriptInstance
{
    private void RunScript(object inputValue, ref object result)
    {
        result = null;
    }
}
"""
    initial_details = {
        "id": template_node_id,
        "parameters": {"Code": {"v": template, "r": False}},
        "inputs": [{"name": "inputValue", "index": 0}],
        "outputs": [{"name": "result", "index": 0}]
    }
    expected_source = template.replace(
        "        result = null;",
        "        result = inputValue;"
    )
    verified_details = {
        "id": node_id,
        "parameters": {
            "Code": {"v": expected_source, "r": False},
            "RuntimeMessageLevel": {"v": 0, "r": True},
            "IsSDKMode": {"v": True, "r": True}
        },
        "inputs": [{"name": "inputValue", "index": 0}],
        "outputs": [{"name": "result", "index": 0}]
    }

    respx.post(f"{HOST_URL}/create").mock(
        side_effect=[
            httpx.Response(200, json={"status": "success", "id": template_node_id}),
            httpx.Response(200, json={"status": "success", "id": node_id})
        ]
    )
    respx.get(f"{HOST_URL}/node/{template_node_id}").mock(
        return_value=httpx.Response(200, json=initial_details)
    )
    respx.delete(f"{HOST_URL}/node/{template_node_id}").mock(
        return_value=httpx.Response(200, json={"status": "success", "id": template_node_id})
    )
    respx.get(f"{HOST_URL}/node/{node_id}").mock(
        return_value=httpx.Response(200, json=verified_details)
    )

    result = json.loads(await create_script_node(
        name="Generated Script",
        canvas_x=300,
        canvas_y=400,
        script_body="result = inputValue;"
    ))

    assert result["status"] == "success"
    assert result["id"] == node_id
    assert result["runtime_message_level"] == 0
    assert result["is_sdk_mode"] is True

    create_request = json.loads(respx.calls[0].request.content)
    assert create_request["type"] == "CSharpComponent"
    assert create_request["parameters"] == {}

    final_create_request = json.loads(respx.calls[3].request.content)
    injected_code = final_create_request["parameters"]["Code"]
    assert "private void RunScript(object inputValue, ref object result)" in injected_code
    assert "        result = inputValue;" in injected_code
    assert "result = null;" not in injected_code

@pytest.mark.asyncio
@respx.mock
async def test_create_script_node_cleans_up_when_template_is_missing():
    node_id = "template-guid"
    respx.post(f"{HOST_URL}/create").mock(
        return_value=httpx.Response(200, json={"status": "success", "id": node_id})
    )
    respx.get(f"{HOST_URL}/node/{node_id}").mock(
        return_value=httpx.Response(200, json={"id": node_id, "parameters": {}})
    )
    delete_route = respx.delete(f"{HOST_URL}/node/{node_id}").mock(
        return_value=httpx.Response(200, json={"status": "success", "id": node_id})
    )

    result = json.loads(await create_script_node(
        name="Broken Script",
        canvas_x=0,
        canvas_y=0,
        script_body="a = x;"
    ))

    assert result["status"] == "error"
    assert "editable code" in result["message"]
    assert delete_route.called

@pytest.mark.asyncio
@respx.mock
async def test_create_script_node_supports_python_template():
    template_node_id = "python-template-guid"
    node_id = "python-script-guid"
    template = '"""Grasshopper Script"""\na = "Hello Python 3 in Grasshopper!"\nprint(a)\n'
    initial_details = {
        "id": template_node_id,
        "parameters": {"Code": {"v": template, "r": False}},
        "inputs": [{"name": "x", "index": 0}, {"name": "y", "index": 1}],
        "outputs": [{"name": "a", "index": 0}]
    }
    expected_source = "a = f'{x}: {y}'\n"
    verified_details = {
        "id": node_id,
        "parameters": {
            "Code": {"v": expected_source, "r": False},
            "RuntimeMessageLevel": {"v": 0, "r": True},
            "IsSDKMode": {"v": False, "r": True}
        },
        "inputs": [{"name": "x", "index": 0}, {"name": "y", "index": 1}],
        "outputs": [{"name": "a", "index": 0}]
    }

    respx.post(f"{HOST_URL}/create").mock(
        side_effect=[
            httpx.Response(200, json={"status": "success", "id": template_node_id}),
            httpx.Response(200, json={"status": "success", "id": node_id})
        ]
    )
    respx.get(f"{HOST_URL}/node/{template_node_id}").mock(
        return_value=httpx.Response(200, json=initial_details)
    )
    respx.delete(f"{HOST_URL}/node/{template_node_id}").mock(
        return_value=httpx.Response(
            200,
            json={"status": "success", "id": template_node_id}
        )
    )
    respx.get(f"{HOST_URL}/node/{node_id}").mock(
        return_value=httpx.Response(200, json=verified_details)
    )

    result = json.loads(await create_script_node(
        name="Generated Python",
        canvas_x=300,
        canvas_y=400,
        script_body="a = f'{x}: {y}'",
        language="python"
    ))

    assert result["status"] == "success"
    create_request = json.loads(respx.calls[0].request.content)
    assert create_request["type"] == "Python3Component"

    final_create_request = json.loads(respx.calls[3].request.content)
    injected_code = final_create_request["parameters"]["Code"]
    assert injected_code == "a = f'{x}: {y}'\n"

@pytest.mark.asyncio
@respx.mock
async def test_create_script_node_rejects_unpersisted_source_and_cleans_up():
    template_node_id = "template-guid"
    node_id = "script-guid"
    template = """public class Script_Instance : GH_ScriptInstance
{
    private void RunScript(object x, ref object a)
    {
        a = null;
    }
}
"""
    respx.post(f"{HOST_URL}/create").mock(
        side_effect=[
            httpx.Response(200, json={"status": "success", "id": template_node_id}),
            httpx.Response(200, json={"status": "success", "id": node_id})
        ]
    )
    respx.get(f"{HOST_URL}/node/{template_node_id}").mock(
        return_value=httpx.Response(
            200,
            json={"id": template_node_id, "parameters": {"Code": {"v": template}}}
        )
    )
    respx.delete(f"{HOST_URL}/node/{template_node_id}").mock(
        return_value=httpx.Response(200, json={"status": "success"})
    )
    respx.get(f"{HOST_URL}/node/{node_id}").mock(
        return_value=httpx.Response(200, json={
            "id": node_id,
            "parameters": {
                "Code": {"v": template},
                "RuntimeMessageLevel": {"v": 0},
                "IsSDKMode": {"v": True}
            }
        })
    )
    final_delete = respx.delete(f"{HOST_URL}/node/{node_id}").mock(
        return_value=httpx.Response(200, json={"status": "success"})
    )

    result = json.loads(await create_script_node(
        name="Generated Script",
        canvas_x=300,
        canvas_y=400,
        script_body="a = x;"
    ))

    assert result["status"] == "error"
    assert "did not persist" in result["message"]
    assert final_delete.called

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

@pytest.mark.asyncio
@respx.mock
async def test_add_description_semantic():
    # Arrange: Mock a small graph
    canvas_state = {
        "nodes": [
            {"id": "1", "name": "Input1", "inputs": [], "outputs": [], "x": 0, "y": 0},
            {"id": "2", "name": "Input2", "inputs": [], "outputs": [], "x": 0, "y": 0}
        ]
    }
    semantic_clusters = json.dumps([{"name": "Inputs", "node_ids": ["1", "2"]}])

    respx.get(f"{HOST_URL}/state").mock(return_value=httpx.Response(200, json=canvas_state))
    respx.patch(f"{HOST_URL}/update/1").mock(return_value=httpx.Response(200, json={"status": "success"}))
    respx.patch(f"{HOST_URL}/update/2").mock(return_value=httpx.Response(200, json={"status": "success"}))
    respx.post(f"{HOST_URL}/create").mock(return_value=httpx.Response(200, json={"status": "success"}))

    # Act
    result_json = await add_description(semantic_clusters=semantic_clusters)
    result = json.loads(result_json)

    # Assert
    assert result["status"] == "success"
    assert "master annotation" in result["message"]

    # Verify group creation was called
    create_calls = [c for c in respx.calls if c.request.url == f"{HOST_URL}/create"]
    assert len(create_calls) >= 1

    group_req = json.loads(create_calls[0].request.content)
    assert group_req["type"] == "Scribble"
    assert group_req["canvasX"] == 0
    assert group_req["canvasY"] == -160
    assert group_req["parameters"]["Text"] == "SCRIPT DOCUMENTATION:\n• Inputs"

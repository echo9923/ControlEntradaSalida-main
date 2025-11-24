#!/usr/bin/env python3


import base64
import json
import sys
from pathlib import Path

import grpc


def _read_face_bytes(face_path: Path) -> tuple[str, str]:
    data = face_path.read_bytes()
    encoded = base64.b64encode(data).decode("ascii")
    return encoded, face_path.suffix.lstrip(".").lower() or "jpg"


def build_payload(config: dict) -> str:
    person = {
        "employee_id": config["employee_id"],
        "name": config.get("name", ""),
        "gender": config.get("gender", "unknown"),
        "enabled": not config.get("disabled", False),
        "valid_from": config.get("valid_from"),
        "valid_to": config.get("valid_to"),
    }

    face_path = config.get("face_path")
    if face_path:
        face_bytes, face_format = _read_face_bytes(Path(face_path))
        person["face_image_base64"] = face_bytes
        person["face_image_format"] = face_format

    payload = {"people": [person]}
    return json.dumps(payload, ensure_ascii=False)


def call_sync_persons(server: str, payload: str, timeout: float) -> dict:
    method = "/permission.PermissionSyncService/SyncPersons"
    with grpc.insecure_channel(server) as channel:
        stub = channel.unary_unary(method)
        response_bytes = stub(payload.encode("utf-8"), timeout=timeout)
    return json.loads(response_bytes.decode("utf-8"))


SERVER_CONFIG = {

    "server": "127.0.0.1:5001",

    "timeout": 10.0,
}

PERSON_CONFIG = {
    "employee_id": "00000004",
    "name": "韩立",
    "gender": "male",  
    "valid_from": "2024-01-01T00:00:00",
    "valid_to": "2035-12-31T23:59:59",
    "face_path": "tools/111.jpg",  
    "disabled": False,
}


def main() -> None:
    payload = build_payload(PERSON_CONFIG)

    try:
        result = call_sync_persons(SERVER_CONFIG["server"], payload, SERVER_CONFIG["timeout"])
    except grpc.RpcError as rpc_error:
        print(f"[ERROR] gRPC 调用失败：{rpc_error.code().name} - {rpc_error.details()}", file=sys.stderr)
        sys.exit(2)

    print("=== 请求载荷 ===")
    print(payload)
    print("\n=== 服务返回 ===")
    print(json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()

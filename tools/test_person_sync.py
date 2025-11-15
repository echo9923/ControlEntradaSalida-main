#!/usr/bin/env python3
"""
简单的人员下发GRPC测试脚本。

示例：
    python tools/test_person_sync.py --server localhost:5001 \
        --employee-id E0001 --name 张三 --gender male --face sample.jpg
"""

import argparse
import base64
import json
import sys
from pathlib import Path

import grpc


def _read_face_bytes(face_path: Path) -> tuple[str, str]:
    data = face_path.read_bytes()
    encoded = base64.b64encode(data).decode("ascii")
    return encoded, face_path.suffix.lstrip(".").lower()


def build_payload(args: argparse.Namespace) -> str:
    person = {
        "employee_id": args.employee_id,
        "name": args.name or "",
        "gender": args.gender,
        "enabled": not args.disable,
        "valid_from": args.valid_from,
        "valid_to": args.valid_to,
    }

    if args.face:
        face_bytes, face_format = _read_face_bytes(Path(args.face))
        person["face_image_base64"] = face_bytes
        if face_format:
            person["face_image_format"] = face_format

    payload = {"people": [person]}
    return json.dumps(payload, ensure_ascii=False)


def call_sync_persons(server: str, payload: str, timeout: float) -> dict:
    method = "/permission.PermissionSyncService/SyncPersons"
    with grpc.insecure_channel(server) as channel:
        stub = channel.unary_unary(method)
        response_bytes = stub(payload.encode("utf-8"), timeout=timeout)
    return json.loads(response_bytes.decode("utf-8"))


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="门禁人员+人脸GRPC测试工具")
    parser.add_argument("--server", default="127.0.0.1:5001", help="GRPC服务地址 host:port")
    parser.add_argument("--employee-id", required=True, help="人员工号/ID")
    parser.add_argument("--name", help="人员姓名")
    parser.add_argument("--gender", default="unknown", choices=["male", "female", "unknown"], help="性别")
    parser.add_argument("--valid-from", default="2024-01-01T00:00:00", help="开始有效时间，ISO8601")
    parser.add_argument("--valid-to", default="2035-12-31T23:59:59", help="结束有效时间，ISO8601")
    parser.add_argument("--face", help="人脸图片路径（jpg/png），可选")
    parser.add_argument("--disable", action="store_true", help="是否将该人员标记为禁用")
    parser.add_argument("--timeout", type=float, default=10.0, help="GRPC调用超时时间（秒）")
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    payload = build_payload(args)

    try:
        result = call_sync_persons(args.server, payload, args.timeout)
    except grpc.RpcError as rpc_error:
        print(f"[ERROR] gRPC 调用失败：{rpc_error.code().name} - {rpc_error.details()}", file=sys.stderr)
        sys.exit(2)

    print("=== 请求载荷 ===")
    print(payload)
    print("\n=== 服务返回 ===")
    print(json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()

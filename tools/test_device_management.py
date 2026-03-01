#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""门禁设备管理 gRPC 接口测试工具。

说明：
- 接口：device.AccessControlService（string + JSON 载荷）
- 默认服务地址：127.0.0.1:5001
- 若服务端配置了 Service.GrpcManagementApiKey，需要通过 --api-key 传入。

安全：
- 对新增/删除/断开/重连等“可能影响生产”的操作，若 server 不是 localhost/127.0.0.1/::1，
  需要显式加 --allow-non-localhost 才会执行。

用法示例：
  python tools/test_device_management.py status --refresh
  python tools/test_device_management.py add --device-id 101 --name "测试门禁" --ip 10.0.0.10 --password "admin123" --connect-now
  python tools/test_device_management.py disconnect --device-id 101
  python tools/test_device_management.py reconnect --device-id 101 --force
  python tools/test_device_management.py delete --device-id 101
"""

from __future__ import annotations

import argparse
import json
import sys
from typing import Any, Dict, List, Optional, Tuple

import grpc


DEFAULT_SERVER = "127.0.0.1:5001"
DEFAULT_TIMEOUT = 10.0


def _parse_server(server: str) -> Tuple[str, int]:
    server = (server or "").strip()
    if not server:
        raise ValueError("server 不能为空")

    # 允许形如 "127.0.0.1:5001" 或 "localhost:5001" 或 "[::1]:5001"
    if server.startswith("["):
        # IPv6
        if "]" not in server:
            raise ValueError("IPv6 地址格式错误，应为 [::1]:5001")
        host_part, rest = server.split("]", 1)
        host = host_part.lstrip("[")
        if not rest.startswith(":"):
            raise ValueError("端口缺失，应为 [::1]:5001")
        port_str = rest.lstrip(":")
    else:
        if ":" not in server:
            raise ValueError("端口缺失，应为 host:port")
        host, port_str = server.rsplit(":", 1)

    try:
        port = int(port_str)
    except ValueError as exc:
        raise ValueError("端口必须为整数") from exc

    if port <= 0 or port > 65535:
        raise ValueError("端口必须在 1-65535 范围内")

    return host.strip(), port


def _is_local_host(host: str) -> bool:
    host = (host or "").strip().lower()
    return host in {"127.0.0.1", "localhost", "::1"}


def _parse_grpc_error(error: grpc.RpcError) -> Dict[str, Any]:
    status_name = None
    try:
        status_name = error.code().name
    except Exception:
        status_name = None

    details = None
    try:
        details = error.details()
    except Exception:
        details = None

    parsed_detail = None
    if details:
        try:
            parsed_detail = json.loads(details)
        except Exception:
            parsed_detail = None

    payload: Dict[str, Any]
    if isinstance(parsed_detail, dict):
        payload = parsed_detail
    else:
        payload = {
            "success": False,
            "code": "RPC_ERROR",
            "message": details or str(error),
        }

    payload["grpcStatus"] = status_name or "UNKNOWN"
    return payload


def _grpc_call(
    server: str,
    method: str,
    payload: Dict[str, Any],
    timeout: float,
    api_key: Optional[str],
) -> Dict[str, Any]:
    payload_str = json.dumps(payload, ensure_ascii=False)
    metadata = []
    if api_key:
        metadata.append(("x-api-key", api_key))

    with grpc.insecure_channel(server) as channel:
        stub = channel.unary_unary(method)
        try:
            resp = stub(payload_str.encode("utf-8"), timeout=timeout, metadata=metadata)
        except grpc.RpcError as exc:
            return _parse_grpc_error(exc)

    try:
        return json.loads(resp.decode("utf-8"))
    except Exception:
        return {
            "success": False,
            "code": "INVALID_RESPONSE",
            "message": "响应不是有效 JSON",
            "raw": resp.decode("utf-8", errors="ignore"),
        }


def _print_json(obj: Any) -> None:
    text = json.dumps(obj, ensure_ascii=False, indent=2, sort_keys=False)
    print(text)


def _require_safe_target(server: str, allow_non_localhost: bool) -> None:
    host, _ = _parse_server(server)
    if _is_local_host(host):
        return

    if allow_non_localhost:
        return

    raise SystemExit(
        "安全保护：当前 server 非本机地址。\n"
        "如确认要对非本机服务执行新增/删除/断开/重连，请加参数 --allow-non-localhost。"
    )


def main(argv: Optional[List[str]] = None) -> int:
    parser = argparse.ArgumentParser(description="门禁设备管理 gRPC 接口测试工具")
    parser.add_argument("--server", default=DEFAULT_SERVER, help="gRPC 服务地址，默认 127.0.0.1:5001")
    parser.add_argument("--api-key", default="", help="x-api-key（若服务端启用鉴权则必填）")
    parser.add_argument("--timeout", type=float, default=DEFAULT_TIMEOUT, help="单次调用超时（秒）")
    parser.add_argument(
        "--allow-non-localhost",
        action="store_true",
        help="允许对非本机 server 执行可能影响生产的操作（新增/删除/断开/重连）",
    )

    subparsers = parser.add_subparsers(dest="cmd", required=True)

    p_status = subparsers.add_parser("status", help="查询设备状态")
    p_status.add_argument("--device-id", type=int, default=0)
    p_status.add_argument("--device-ids", default="", help="逗号分隔的 deviceId 列表")
    p_status.add_argument("--ip", default="", help="按 IP 查询")
    p_status.add_argument("--include-disabled", action="store_true", help="包含禁用设备（默认包含）")
    p_status.add_argument("--exclude-disabled", action="store_true", help="排除禁用设备")
    p_status.add_argument("--refresh", action="store_true", help="先刷新一次设备状态")

    p_add = subparsers.add_parser("add", help="新增设备")
    p_add.add_argument("--device-id", type=int, required=True)
    p_add.add_argument("--name", required=True)
    p_add.add_argument("--ip", required=True)
    p_add.add_argument("--port", default="8000")
    p_add.add_argument("--username", default="admin")
    p_add.add_argument("--password", required=True)
    p_add.add_argument("--description", default="")
    p_add.add_argument("--disabled", action="store_true", help="新增后置为禁用")
    p_add.add_argument("--connect-now", action="store_true", help="新增后立即尝试连接")

    p_delete = subparsers.add_parser("delete", help="删除设备")
    p_delete.add_argument("--device-id", type=int, required=True)
    p_delete.add_argument("--disconnect-first", action="store_true", help="删除前先断开（默认断开）")
    p_delete.add_argument("--no-disconnect-first", action="store_true", help="删除前不主动断开")

    p_disconnect = subparsers.add_parser("disconnect", help="断开设备")
    p_disconnect.add_argument("--device-id", type=int, required=True)

    p_reconnect = subparsers.add_parser("reconnect", help="重连设备")
    p_reconnect.add_argument("--device-id", type=int, required=True)
    p_reconnect.add_argument("--force", action="store_true", help="强制先断开再连接")

    args = parser.parse_args(argv)

    server = args.server
    api_key = args.api_key.strip() or None

    if args.cmd == "status":
        payload: Dict[str, Any] = {}
        if args.device_id and args.device_id > 0:
            payload["deviceId"] = args.device_id
        if args.device_ids.strip():
            ids = [int(x) for x in args.device_ids.split(",") if x.strip().isdigit()]
            if ids:
                payload["deviceIds"] = ids
        if args.ip.strip():
            payload["ipAddress"] = args.ip.strip()

        if args.exclude_disabled:
            payload["includeDisabled"] = False
        elif args.include_disabled:
            payload["includeDisabled"] = True

        if args.refresh:
            payload["refresh"] = True

        result = _grpc_call(server, "/device.AccessControlService/GetDeviceStatus", payload, args.timeout, api_key)
        _print_json(result)
        return 0 if result.get("success") else 2

    if args.cmd == "add":
        _require_safe_target(server, args.allow_non_localhost)
        payload = {
            "deviceId": args.device_id,
            "deviceName": args.name,
            "ipAddress": args.ip,
            "port": args.port,
            "username": args.username,
            "password": args.password,
            "description": args.description,
            "enabled": not args.disabled,
            "connectNow": bool(args.connect_now),
        }
        result = _grpc_call(server, "/device.AccessControlService/AddDevice", payload, args.timeout, api_key)
        _print_json(result)
        return 0 if result.get("success") else 2

    if args.cmd == "delete":
        _require_safe_target(server, args.allow_non_localhost)
        disconnect_first = True
        if args.no_disconnect_first:
            disconnect_first = False
        elif args.disconnect_first:
            disconnect_first = True

        payload = {
            "deviceId": args.device_id,
            "disconnectFirst": disconnect_first,
        }
        result = _grpc_call(server, "/device.AccessControlService/DeleteDevice", payload, args.timeout, api_key)
        _print_json(result)
        return 0 if result.get("success") else 2

    if args.cmd == "disconnect":
        _require_safe_target(server, args.allow_non_localhost)
        payload = {"deviceId": args.device_id}
        result = _grpc_call(server, "/device.AccessControlService/DisconnectDevice", payload, args.timeout, api_key)
        _print_json(result)
        return 0 if result.get("success") else 2

    if args.cmd == "reconnect":
        _require_safe_target(server, args.allow_non_localhost)
        payload = {"deviceId": args.device_id, "force": bool(args.force)}
        result = _grpc_call(server, "/device.AccessControlService/ReconnectDevice", payload, args.timeout, api_key)
        _print_json(result)
        return 0 if result.get("success") else 2

    raise SystemExit(f"未知命令: {args.cmd}")


if __name__ == "__main__":
    sys.exit(main())

#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
基于现有 gRPC 接口（permission.PermissionSyncService）的人脸录入全流程测试工具。
功能：权限下发、人员+人脸下发、人员删除、人脸删除、人脸查询、抓拍采集（CaptureFaceStream），UI 使用 Qt。

依赖：
  - grpcio
  - PyQt5 （若未安装，会尝试使用 PySide6）

运行：
  python tools/test_person_sync.py
"""

from __future__ import annotations

import base64
import json
import sys
import traceback
from dataclasses import dataclass
from pathlib import Path
from typing import Callable, Optional, List, Dict, Any

import grpc

try:  # 优先使用 PyQt5
    from PyQt5 import QtCore, QtGui, QtWidgets
except ImportError:  # pragma: no cover - 备用
    from PySide6 import QtCore, QtGui, QtWidgets  # type: ignore


# -------------------- 配置默认值 --------------------

DEFAULT_SERVER = "127.0.0.1:5001"
DEFAULT_TIMEOUT = 10.0
DEFAULT_PERMISSION_CODE = 1


# -------------------- 数据模型 --------------------

@dataclass
class PersonConfig:
    employee_id: str
    name: str = ""
    gender: str = "unknown"
    enabled: bool = True
    valid_from: Optional[str] = None
    valid_to: Optional[str] = None
    face_path: Optional[str] = None


# -------------------- gRPC 调用封装 --------------------

def _grpc_call(server: str, method: str, payload: dict | list, timeout: float) -> dict:
    payload_str = json.dumps(payload, ensure_ascii=False)
    with grpc.insecure_channel(server) as channel:
        stub = channel.unary_unary(method)
        resp = stub(payload_str.encode("utf-8"), timeout=timeout)
    return json.loads(resp.decode("utf-8"))


def _grpc_stream(server: str, method: str, payload: dict | list, timeout: float) -> List[Dict[str, Any]]:
    payload_str = json.dumps(payload, ensure_ascii=False)
    results: List[Dict[str, Any]] = []
    with grpc.insecure_channel(server) as channel:
        stub = channel.unary_stream(method)
        for msg in stub(payload_str.encode("utf-8"), timeout=timeout):
            try:
                results.append(json.loads(msg.decode("utf-8")))
            except Exception:
                results.append({"raw": msg.decode("utf-8", errors="ignore")})
    return results


def build_sync_persons_payload(cfg: PersonConfig) -> dict:
    person = {
        "employee_id": cfg.employee_id,
        "name": cfg.name,
        "gender": cfg.gender or "unknown",
        "enabled": cfg.enabled,
        "valid_from": cfg.valid_from,
        "valid_to": cfg.valid_to,
    }

    if cfg.face_path:
        face_bytes, face_format = _read_face(Path(cfg.face_path))
        person["face_image_base64"] = face_bytes
        person["face_image_format"] = face_format

    return {"people": [person]}


def build_permission_payload(employee_id: str, permission_code: int = 1) -> list:
    return [{"employee_id": employee_id, "permission_code": permission_code}]


def build_face_id_payload(employee_id: str) -> dict:
    return {"items": [{"employee_id": employee_id}]}


def build_status_payload(task_id: str) -> dict:
    return {"taskId": task_id}


def _read_face(path: Path) -> tuple[str, str]:
    if not path.is_file():
        raise FileNotFoundError(f"找不到人脸图片文件：{path}")
    data = path.read_bytes()
    if len(data) > 200 * 1024:
        raise ValueError("人脸图片大小超过 200KB 限制")
    encoded = base64.b64encode(data).decode("ascii")
    return encoded, path.suffix.lstrip(".").lower() or "jpg"


# -------------------- Qt 异步执行器 --------------------

class GrpcWorker(QtCore.QRunnable):
    def __init__(self, fn: Callable[[], dict], action: str, payload_preview: str):
        super().__init__()
        self.fn = fn
        self.action = action
        self.payload_preview = payload_preview
        self.signals = WorkerSignals()

    @QtCore.pyqtSlot()
    def run(self) -> None:
        try:
            result = self.fn()
            self.signals.success.emit(self.action, self.payload_preview, result)
        except Exception as exc:  # pragma: no cover - UI 异常通过信号传递
            err = f"{exc}\n{traceback.format_exc()}"
            self.signals.error.emit(self.action, self.payload_preview, err)


class WorkerSignals(QtCore.QObject):
    success = QtCore.pyqtSignal(str, str, dict)
    error = QtCore.pyqtSignal(str, str, str)


# -------------------- 主窗口 --------------------

class MainWindow(QtWidgets.QMainWindow):
    def __init__(self) -> None:
        super().__init__()
        self.setWindowTitle("人脸录入 gRPC 集成测试")
        self.resize(960, 640)
        self.last_task_id: Optional[str] = None
        self.pool = QtCore.QThreadPool.globalInstance()
        self._build_ui()

    # UI 组装
    def _build_ui(self) -> None:
        central = QtWidgets.QWidget()
        self.setCentralWidget(central)
        layout = QtWidgets.QGridLayout(central)

        # 行 0：服务器配置
        layout.addWidget(QtWidgets.QLabel("gRPC 服务地址"), 0, 0)
        self.server_edit = QtWidgets.QLineEdit(DEFAULT_SERVER)
        layout.addWidget(self.server_edit, 0, 1)

        layout.addWidget(QtWidgets.QLabel("超时(s)"), 0, 2)
        self.timeout_edit = QtWidgets.QLineEdit(str(DEFAULT_TIMEOUT))
        self.timeout_edit.setFixedWidth(80)
        layout.addWidget(self.timeout_edit, 0, 3)

        layout.addWidget(QtWidgets.QLabel("权限编码"), 0, 4)
        self.permission_edit = QtWidgets.QLineEdit(str(DEFAULT_PERMISSION_CODE))
        self.permission_edit.setFixedWidth(80)
        layout.addWidget(self.permission_edit, 0, 5)

        # 行 1：人员信息
        layout.addWidget(QtWidgets.QLabel("员工ID"), 1, 0)
        self.employee_edit = QtWidgets.QLineEdit("00000004")
        layout.addWidget(self.employee_edit, 1, 1)

        layout.addWidget(QtWidgets.QLabel("姓名"), 1, 2)
        self.name_edit = QtWidgets.QLineEdit("测试用户")
        layout.addWidget(self.name_edit, 1, 3)

        layout.addWidget(QtWidgets.QLabel("性别"), 1, 4)
        self.gender_combo = QtWidgets.QComboBox()
        self.gender_combo.addItems(["unknown", "male", "female"])
        layout.addWidget(self.gender_combo, 1, 5)

        self.enabled_chk = QtWidgets.QCheckBox("启用")
        self.enabled_chk.setChecked(True)
        layout.addWidget(self.enabled_chk, 1, 6)

        # 行 2：有效期
        layout.addWidget(QtWidgets.QLabel("有效期自"), 2, 0)
        self.valid_from = QtWidgets.QLineEdit("2024-01-01T00:00:00")
        layout.addWidget(self.valid_from, 2, 1)

        layout.addWidget(QtWidgets.QLabel("至"), 2, 2)
        self.valid_to = QtWidgets.QLineEdit("2035-12-31T23:59:59")
        layout.addWidget(self.valid_to, 2, 3)

        # 行 3：人脸文件
        layout.addWidget(QtWidgets.QLabel("人脸图片"), 3, 0)
        self.face_path_edit = QtWidgets.QLineEdit()
        layout.addWidget(self.face_path_edit, 3, 1, 1, 3)
        btn_browse = QtWidgets.QPushButton("选择文件")
        btn_browse.clicked.connect(self._choose_face)
        layout.addWidget(btn_browse, 3, 4)

        # 按钮行
        btn_layout = QtWidgets.QHBoxLayout()
        self.btn_sync = QtWidgets.QPushButton("下发人员+人脸")
        self.btn_sync.clicked.connect(self._on_sync_person)
        btn_layout.addWidget(self.btn_sync)

        self.btn_perm = QtWidgets.QPushButton("下发权限")
        self.btn_perm.clicked.connect(self._on_sync_permission)
        btn_layout.addWidget(self.btn_perm)

        self.btn_delete = QtWidgets.QPushButton("删除人脸")
        self.btn_delete.clicked.connect(self._on_delete_face)
        btn_layout.addWidget(self.btn_delete)

        self.btn_delete_person = QtWidgets.QPushButton("删除人员")
        self.btn_delete_person.clicked.connect(self._on_delete_person)
        btn_layout.addWidget(self.btn_delete_person)

        self.btn_get = QtWidgets.QPushButton("查询人脸")
        self.btn_get.clicked.connect(self._on_get_face)
        btn_layout.addWidget(self.btn_get)

        self.btn_capture = QtWidgets.QPushButton("抓拍采集")
        self.btn_capture.clicked.connect(self._on_capture_face)
        btn_layout.addWidget(self.btn_capture)

        self.btn_status = QtWidgets.QPushButton("查询任务状态")
        self.btn_status.clicked.connect(self._on_get_status)
        btn_layout.addWidget(self.btn_status)

        btn_clear = QtWidgets.QPushButton("清空日志")
        btn_clear.clicked.connect(lambda: self.log_view.clear())
        btn_layout.addWidget(btn_clear)

        layout.addLayout(btn_layout, 4, 0, 1, 7)

        # 图片预览
        self.preview = QtWidgets.QLabel("预览")
        self.preview.setAlignment(QtCore.Qt.AlignCenter)
        self.preview.setFixedSize(200, 200)
        self.preview.setFrameShape(QtWidgets.QFrame.Box)
        layout.addWidget(self.preview, 0, 7, 4, 1)

        # 任务信息
        self.task_label = QtWidgets.QLabel("最近任务: -")
        layout.addWidget(self.task_label, 4, 7)

        # 日志输出
        self.log_view = QtWidgets.QTextEdit()
        self.log_view.setReadOnly(True)
        layout.addWidget(self.log_view, 5, 0, 1, 8)

        self._load_preview()

    # -------------------- 事件处理 --------------------

    def _choose_face(self) -> None:
        path, _ = QtWidgets.QFileDialog.getOpenFileName(self, "选择人脸图片", str(Path.cwd()), "Images (*.jpg *.jpeg *.png)")
        if path:
            self.face_path_edit.setText(path)
            self._load_preview()

    def _load_preview(self) -> None:
        path = self.face_path_edit.text().strip()
        if not path:
            self.preview.setText("预览")
            return
        pix = QtGui.QPixmap(path)
        if pix.isNull():
            self.preview.setText("无法加载图片")
            return
        self.preview.setPixmap(pix.scaled(self.preview.size(), QtCore.Qt.KeepAspectRatio, QtCore.Qt.SmoothTransformation))

    def _log(self, text: str) -> None:
        self.log_view.append(text)

    def _run_async(self, action: str, payload_obj: dict | list, func: Callable[[], dict]) -> None:
        payload_preview = json.dumps(payload_obj, ensure_ascii=False, indent=2)
        self._log(f"[{action}] 请求:\n{payload_preview}\n")
        worker = GrpcWorker(func, action, payload_preview)
        worker.signals.success.connect(self._on_success)
        worker.signals.error.connect(self._on_error)
        self.pool.start(worker)

    def _server(self) -> str:
        return self.server_edit.text().strip() or DEFAULT_SERVER

    def _timeout(self) -> float:
        try:
            return float(self.timeout_edit.text())
        except ValueError:
            return DEFAULT_TIMEOUT

    # 组装配置
    def _current_person(self) -> PersonConfig:
        return PersonConfig(
            employee_id=self.employee_edit.text().strip(),
            name=self.name_edit.text().strip(),
            gender=self.gender_combo.currentText(),
            enabled=self.enabled_chk.isChecked(),
            valid_from=self.valid_from.text().strip() or None,
            valid_to=self.valid_to.text().strip() or None,
            face_path=self.face_path_edit.text().strip() or None,
        )

    # -------------------- 按钮动作 --------------------

    def _on_sync_person(self) -> None:
        cfg = self._current_person()
        payload = build_sync_persons_payload(cfg)

        def task() -> dict:
            return _grpc_call(self._server(), "/permission.PermissionSyncService/SyncPersons", payload, self._timeout())

        self._run_async("SyncPersons", payload, task)

    def _on_sync_permission(self) -> None:
        employee_id = self.employee_edit.text().strip()
        try:
            code = int(self.permission_edit.text().strip() or DEFAULT_PERMISSION_CODE)
        except ValueError:
            code = DEFAULT_PERMISSION_CODE
        payload = build_permission_payload(employee_id, permission_code=code)

        def task() -> dict:
            return _grpc_call(self._server(), "/permission.PermissionSyncService/SyncPermissions", payload, self._timeout())

        self._run_async("SyncPermissions", payload, task)

    def _on_delete_face(self) -> None:
        employee_id = self.employee_edit.text().strip()
        payload = build_face_id_payload(employee_id)

        def task() -> dict:
            return _grpc_call(self._server(), "/permission.PermissionSyncService/DeleteFaces", payload, self._timeout())

        self._run_async("DeleteFaces", payload, task)

    def _on_delete_person(self) -> None:
        """删除人员（从设备中彻底删除人员信息，包括人脸和权限）"""
        employee_id = self.employee_edit.text().strip()
        payload = build_face_id_payload(employee_id)

        def task() -> dict:
            return _grpc_call(self._server(), "/permission.PermissionSyncService/DeletePersons", payload, self._timeout())

        self._run_async("DeletePersons", payload, task)

    def _on_get_face(self) -> None:
        employee_id = self.employee_edit.text().strip()
        payload = build_face_id_payload(employee_id)

        def task() -> dict:
            return _grpc_call(self._server(), "/permission.PermissionSyncService/GetFaces", payload, self._timeout())

        self._run_async("GetFaces", payload, task)

    def _on_capture_face(self) -> None:
        employee_id = self.employee_edit.text().strip()
        payload = {"employee_id": employee_id}

        def task() -> dict:
            frames = _grpc_stream(self._server(), "/permission.PermissionSyncService/CaptureFaceStream", payload, self._timeout())
            # 返回第一帧并顺带写入 taskId
            first = frames[0] if frames else {}
            if "taskId" in first:
                self.last_task_id = first.get("taskId")
            return {"frames": frames}

        self._run_async("CaptureFaceStream", payload, task)

    def _on_get_status(self) -> None:
        if not self.last_task_id:
            self._log("没有可查询的 taskId，请先进行抓拍。")
            return
        payload = build_status_payload(self.last_task_id)

        def task() -> dict:
            return _grpc_call(self._server(), "/permission.PermissionSyncService/GetEnrollmentStatus", payload, self._timeout())

        self._run_async("GetEnrollmentStatus", payload, task)

    # -------------------- 结果处理 --------------------

    @QtCore.pyqtSlot(str, str, dict)
    def _on_success(self, action: str, payload: str, result: dict) -> None:
        pretty = json.dumps(result, ensure_ascii=False, indent=2)
        self._log(f"[{action}] 成功:\n{pretty}\n")

        # 如果查询返回了人脸，可尝试预览
        if action == "GetFaces":
            self._try_preview_from_items(result.get("items", []))
        if action == "CaptureFaceStream":
            frames = result.get("frames", [])
            if frames:
                self.last_task_id = frames[0].get("taskId", self.last_task_id)
                self.task_label.setText(f"最近任务: {self.last_task_id or '-'}")
                # 优先使用推荐帧
                recommended = next((f for f in frames if f.get("recommend")), None)
                target = recommended or frames[0]
                face_b64 = target.get("faceImageBase64")
                if face_b64:
                    self._update_preview(face_b64)
                    # 将抓拍结果写入本地 face_path 虚拟引用，后续 SyncPersons 可直接使用
                    self._log("抓拍成功，可直接调用 SyncPersons 下发。")
        if action == "GetEnrollmentStatus":
            self.task_label.setText(f"最近任务: {result.get('taskId', self.last_task_id) or '-'} 状态: {result.get('status')}")

    @QtCore.pyqtSlot(str, str, str)
    def _on_error(self, action: str, payload: str, err: str) -> None:
        self._log(f"[{action}] 失败:\n{err}\n")

    def _try_preview_from_items(self, items):
        for item in items or []:
            face_b64 = item.get("faceImageBase64")
            if face_b64 and self._update_preview(face_b64):
                break

    def _update_preview(self, face_b64: str) -> bool:
        try:
            raw = base64.b64decode(face_b64.split(",")[-1])
            pixmap = QtGui.QPixmap()
            if pixmap.loadFromData(raw):
                self.preview.setPixmap(
                    pixmap.scaled(self.preview.size(), QtCore.Qt.KeepAspectRatio, QtCore.Qt.SmoothTransformation)
                )
                return True
        except Exception:
            return False
        return False


# -------------------- main --------------------

def main() -> None:
    app = QtWidgets.QApplication(sys.argv)
    win = MainWindow()
    win.show()
    sys.exit(app.exec_())


if __name__ == "__main__":
    main()

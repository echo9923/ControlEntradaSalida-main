# 测试

## 范围
- 单元测试位于 `tests/ControlEntradaSalida.Tests`（如新增）。
- 集成示例放在 `tests/Integration/`，并用配置开关保护。

## 规范
- 设备/SDK 调用需要 mock（`HCNetSDK`），避免真实硬件依赖。
- 命名：`类名_方法名_期望行为`（如 `Common_Login_ReturnsUserId`）。


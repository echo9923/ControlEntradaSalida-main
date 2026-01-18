# 权限 gRPC 服务

## 范围
- 服务项目输出在 `bin/` 与 `obj/`，不要修改生成文件。
- 本目录改动需与 `docs/` 下 gRPC 错误契约文档保持一致。

## 构建/运行
- 使用根目录命令：`dotnet build ControlEntradaSalida.sln --verbosity minimal`。

## 变更规范
- 保持公开 API 稳定；契约变化需同步更新文档。
- 错误处理使用结构化错误码（见 `docs/GRPC-Error-Contract.md`）。


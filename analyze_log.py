# -*- coding: utf-8 -*-
"""分析 2026-02-24.log 日志"""
import re
import json

log_path = r'd:\codeproject\c#\门禁\ControlEntradaSalida-main\运行日志\2026-02-24.log'
with open(log_path, 'r', encoding='utf-8') as f:
    lines = f.readlines()

# 1. 首行信息
first = lines[0]
pid = int(re.search(r'\[pid:(\d+)\]', first).group(1))
up_match = re.search(r'\[up:([^\]]+)\]', first)
uptime_start = up_match.group(1) if up_match else ""
# 提取 HH:MM (格式 "YYYY-MM-DD HH:MM:SS")
tm = re.search(r'(\d{2}:\d{2}):\d{2}', first)
start_hhmm = tm.group(1) if tm else ""

# 2. 末行
last = lines[-1]
tm2 = re.search(r'(\d{2}:\d{2}):\d{2}', last)
end_hhmm = tm2.group(1) if tm2 else ""
time_range = f"{start_hhmm} ~ {end_hhmm}"

# 3. 连接丢失 per device
disconnects = {}
for line in lines:
    m = re.search(r'设备 (\d+)\(([^)]+)\) 连接丢失', line)
    if m:
        did, name = m.group(1), m.group(2)
        if did not in disconnects:
            disconnects[did] = {'name': name, 'count': 0}
        disconnects[did]['count'] += 1

# 4. 重连成功
reconnect_ok = {}
for line in lines:
    m = re.search(r'设备 (\d+) 重连成功', line)
    if m:
        did = m.group(1)
        reconnect_ok[did] = reconnect_ok.get(did, 0) + 1

# 5. 达到最大重连次数 (ReconnectManager only)
max_reconnect = {}
for line in lines:
    if '[重连管理器]' in line and '达到最大重连次数' in line:
        m = re.search(r'设备 (\d+) 达到最大重连次数', line)
        if m:
            did = m.group(1)
            max_reconnect[did] = max_reconnect.get(did, 0) + 1

# 6. [ERROR]
errors = []
for line in lines:
    if '[ERROR]' in line:
        t = line[11:16] if len(line) >= 16 else ""
        m1 = re.search(r'设备 ([^\s]+) 报警布防失败，错误码: (\d+)', line)
        if m1:
            errors.append({"time": t, "device": m1.group(1), "type": "报警布防失败", "err_code": int(m1.group(2))})
            continue
        m2 = re.search(r'设备 ([^\s]+) 历史事件补偿启动失败，错误码: (\d+)', line)
        if m2:
            errors.append({"time": t, "device": m2.group(1), "type": "历史事件补偿启动失败", "err_code": int(m2.group(2))})

# 7. 补偿通道异常
compensation_channel = sum(1 for line in lines if '补偿通道异常' in line)

# 8. 补偿启动失败 (ERROR 级别)
compensation_fail = sum(1 for line in lines if '[ERROR]' in line and '历史事件补偿启动失败' in line)

# 9. 报警布防失败 (ERROR)
alarm_fail = sum(1 for line in lines if '[ERROR]' in line and '报警布防失败' in line)

# 10. 连接请求跳过
connection_skip = sum(1 for line in lines if '连接请求跳过' in line)

# 11. 连接失败 - 无直接"连接失败"则空
connection_failures = []

# 12. 降级重试 (格式: 历史事件补偿启动失败，将尝试降级重试)
downgrade_retries = sum(1 for line in lines if '将尝试降级重试' in line)

# 构建 devices
devices = {}
device_names = {}
for line in lines:
    m = re.search(r'设备 (\d+)\(([^)]+)\) 连接丢失', line)
    if m:
        did, name = m.group(1), m.group(2)
        if did not in device_names:
            device_names[did] = name

all_device_ids = set(disconnects.keys()) | set(reconnect_ok.keys()) | set(max_reconnect.keys())
for did in sorted(all_device_ids, key=int):
    name = device_names.get(did, disconnects.get(did, {}).get('name', '') if isinstance(disconnects.get(did), dict) else '')
    disc_count = disconnects[did]['count'] if did in disconnects else 0
    devices[did] = {
        "name": name or f"设备{did}",
        "disconnects": disc_count,
        "reconnect_success": reconnect_ok.get(did, 0),
        "max_reconnect_reached": max_reconnect.get(did, 0)
    }

result = {
    "date": "2026-02-24",
    "time_range": time_range,
    "pid": pid,
    "uptime_start": uptime_start,
    "total_lines": len(lines),
    "devices": devices,
    "errors_count": len(errors),
    "errors": errors,
    "compensation_channel_errors": compensation_channel,
    "compensation_total_failures": compensation_fail,
    "alarm_setup_failures": alarm_fail,
    "connection_semaphore_timeouts": connection_skip,
    "downgrade_retries": downgrade_retries,
    "connection_failures": connection_failures
}

out = json.dumps(result, ensure_ascii=False, indent=2)
with open(log_path.replace('.log', '_analysis.json'), 'w', encoding='utf-8') as f:
    f.write(out)
print(out)

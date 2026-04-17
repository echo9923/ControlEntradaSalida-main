using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ControlEntradaSalida.Application.Abstractions;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 设备写操作离线补偿后台管理器。
    /// </summary>
    public sealed class DeviceOperationRetryManager : IDisposable
    {
        private readonly ServiceConfiguration.DeviceOperationRetryOptions options;
        private readonly DeviceOperationRetryStore store;
        private readonly PermissionRefreshManager refreshManager;
        private readonly ConcurrentDictionary<int, byte> processingDevices;

        private Timer scanTimer;
        private int started;
        private int disposed;
        private int scanRunning;

        public DeviceOperationRetryManager(
            ServiceConfiguration.DeviceOperationRetryOptions options,
            DeviceOperationRetryStore store,
            PermissionRefreshManager refreshManager)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.refreshManager = refreshManager ?? throw new ArgumentNullException(nameof(refreshManager));
            processingDevices = new ConcurrentDictionary<int, byte>();
        }

        public DeviceOperationRetryManager(
            RuntimeDeviceOperationRetryOptions options,
            DeviceOperationRetryStore store,
            PermissionRefreshManager refreshManager)
            : this(
                LegacyRuntimeConfigurationMapper.ToLegacyOptions(options),
                store,
                refreshManager)
        {
        }

        public bool Enabled => options.Enabled;

        public void Start()
        {
            if (!options.Enabled || Interlocked.CompareExchange(ref started, 1, 0) != 0)
            {
                return;
            }

            DeviceConnectionManager.Instance.DeviceConnectionStateChanged += OnDeviceConnectionStateChanged;
            TimeSpan interval = TimeSpan.FromSeconds(Math.Max(1, options.ScanIntervalSeconds));
            scanTimer = new Timer(_ => ScanDueStates(), null, interval, interval);

            ServiceLogger.Info($"设备离线写操作补偿已启动，扫描间隔 {interval.TotalSeconds} 秒。");
            _ = Task.Run(() => ScanDueStates());
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0)
            {
                return;
            }

            DeviceConnectionManager.Instance.DeviceConnectionStateChanged -= OnDeviceConnectionStateChanged;

            try
            {
                scanTimer?.Dispose();
            }
            catch (Exception ex)
            {
                ServiceLogger.Error("释放设备离线写操作补偿定时器时发生异常。", ex);
            }
            finally
            {
                scanTimer = null;
            }
        }

        private void OnDeviceConnectionStateChanged(object sender, DeviceConnectionEventArgs e)
        {
            if (!options.Enabled || e?.Device == null || !e.Success || Volatile.Read(ref disposed) == 1)
            {
                return;
            }

            _ = Task.Run(() => ProcessDevice(e.Device.Id, true));
        }

        private void ScanDueStates()
        {
            if (!options.Enabled || Volatile.Read(ref disposed) == 1)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref scanRunning, 1, 0) != 0)
            {
                return;
            }

            try
            {
                store.CleanupExpiredFailures(options.FailureRetentionDays);
                var states = store.LoadPendingStates(DateTime.Now);
                foreach (int deviceId in states.Select(s => s.DeviceId).Distinct())
                {
                    _ = Task.Run(() => ProcessDevice(deviceId, false));
                }
            }
            catch (Exception ex)
            {
                ServiceLogger.Error("扫描设备离线写操作补偿任务时发生异常。", ex);
            }
            finally
            {
                Interlocked.Exchange(ref scanRunning, 0);
            }
        }

        private void ProcessDevice(int deviceId, bool ignoreNextRetry)
        {
            if (!options.Enabled || deviceId <= 0 || Volatile.Read(ref disposed) == 1)
            {
                return;
            }

            if (!processingDevices.TryAdd(deviceId, 0))
            {
                return;
            }

            try
            {
                DeviceConnectionInfo device = DeviceConnectionManager.Instance.GetDeviceById(deviceId);
                if (device == null || !device.IsEnabled)
                {
                    HandleUnavailableDeviceStates(deviceId, device);
                    return;
                }

                var states = store.LoadPendingStates(DateTime.Now, deviceId, ignoreNextRetry);
                if (states.Count == 0)
                {
                    return;
                }

                foreach (var state in states.OrderBy(s => s.UpdatedAt).ToList())
                {
                    DeviceOperationRetryState current = store.GetState(deviceId, state.EmployeeId);
                    if (current == null || !current.HasPendingOperations || current.ExhaustedAt.HasValue)
                    {
                        continue;
                    }

                    DeviceOperationRetryExecutionResult result = refreshManager.ProcessQueuedState(current);
                    if (result.Success)
                    {
                        ServiceLogger.Info($"设备 {device.Name} 员工 {current.EmployeeId} 的离线补偿已完成。");
                        continue;
                    }

                    if (result.Retryable)
                    {
                        store.ScheduleRetry(deviceId,
                            current.EmployeeId,
                            result.ErrorMessage,
                            options.RetryIntervalSeconds,
                            options.MaxRetryAttempts);
                        ServiceLogger.Warn($"设备 {device.Name} 员工 {current.EmployeeId} 的离线补偿稍后重试：{result.ErrorMessage}");
                        continue;
                    }

                    store.MarkTerminalFailure(deviceId,
                        current.EmployeeId,
                        result.ErrorMessage,
                        options.MaxRetryAttempts);
                    ServiceLogger.Error($"设备 {device.Name} 员工 {current.EmployeeId} 的离线补偿已终止：{result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                ServiceLogger.Error($"处理设备 {deviceId} 的离线补偿任务时发生异常。", ex);
            }
            finally
            {
                processingDevices.TryRemove(deviceId, out _);
            }
        }

        private void HandleUnavailableDeviceStates(int deviceId, DeviceConnectionInfo device)
        {
            string deviceLabel = device?.Name;
            if (string.IsNullOrWhiteSpace(deviceLabel))
            {
                deviceLabel = deviceId.ToString();
            }

            string reason = device == null
                ? $"设备 {deviceLabel} 已不存在，离线补偿记录已终止。"
                : $"设备 {deviceLabel} 已停用，离线补偿记录已终止。";

            foreach (var state in store.LoadPendingStates(DateTime.Now, deviceId, ignoreNextRetry: true, includeExhausted: true))
            {
                try
                {
                    store.RemoveState(deviceId, state.EmployeeId);

                    if (state.PermissionPending && state.PermissionLevel.HasValue
                        && !state.PermissionSyncCompletionBlocked
                        && !refreshManager.CompletePermissionSyncIfNoPending(state.EmployeeId, state.PermissionLevel.Value))
                    {
                        ServiceLogger.Warn($"设备 {deviceLabel} 移除补偿任务后，员工 {state.EmployeeId} 的权限同步标记仍未更新。");
                    }

                    ServiceLogger.Warn($"{reason} 员工 {state.EmployeeId} 的补偿任务已移除。");
                }
                catch (Exception ex)
                {
                    ServiceLogger.Error($"终结设备 {deviceId} 员工 {state.EmployeeId} 的补偿任务时发生异常。", ex);
                }
            }
        }
    }

    public sealed class DeviceOperationRetryExecutionResult
    {
        public static DeviceOperationRetryExecutionResult Completed { get; } = new DeviceOperationRetryExecutionResult
        {
            Success = true
        };

        public bool Success { get; private set; }

        public bool Retryable { get; private set; }

        public string ErrorMessage { get; private set; }

        public static DeviceOperationRetryExecutionResult RetryableFailure(string errorMessage)
        {
            return new DeviceOperationRetryExecutionResult
            {
                Retryable = true,
                ErrorMessage = errorMessage
            };
        }

        public static DeviceOperationRetryExecutionResult HardFailure(string errorMessage)
        {
            return new DeviceOperationRetryExecutionResult
            {
                Retryable = false,
                ErrorMessage = errorMessage
            };
        }
    }
}

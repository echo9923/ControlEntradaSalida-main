using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;
using ControlEntradaSalida.Application.Abstractions;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 重连状态信息
    /// </summary>
    public class ReconnectState
    {
        public int DeviceId { get; set; }
        public int Attempts { get; set; } = 0;
        public DateTime LastAttempt { get; set; } = DateTime.MinValue;
        public DateTime NextRetry { get; set; } = DateTime.MinValue;
        public bool IsPermanentFailure { get; set; } = false;
        public TimeSpan CurrentDelay { get; set; } = TimeSpan.Zero;
        public bool IsInCooldown { get; set; } = false;
        public DateTime CooldownUntil { get; set; } = DateTime.MinValue;
    }

    /// <summary>
    /// 重连事件参数
    /// </summary>
    public class DeviceReconnectEventArgs : EventArgs
    {
        public int DeviceId { get; }
        public int Attempts { get; }
        public TimeSpan NextDelay { get; }
        public bool IsLastAttempt { get; }
        public string Reason { get; }

        public DeviceReconnectEventArgs(int deviceId, int attempts, TimeSpan nextDelay, bool isLastAttempt, string reason)
        {
            DeviceId = deviceId;
            Attempts = attempts;
            NextDelay = nextDelay;
            IsLastAttempt = isLastAttempt;
            Reason = reason;
        }
    }

    /// <summary>
    /// 重连管理器 - 实现指数退避算法和重连状态管理
    /// </summary>
    public class ReconnectManager : IDisposable
    {
        #region 默认配置
        
        /// <summary>
        /// 基础重连延迟时间（毫秒）
        /// </summary>
        private const int DefaultBaseDelayMs = 1000;
        
        /// <summary>
        /// 最大重连延迟时间（毫秒）
        /// </summary>
        private const int DefaultMaxDelayMs = 300000; // 5分钟
        
        /// <summary>
        /// 最大重连尝试次数
        /// </summary>
        private const int DefaultMaxReconnectAttempts = 10;
        
        /// <summary>
        /// 抖动因子（0.0 - 1.0）
        /// </summary>
        private const double DefaultJitterFactor = 0.1;
        
        /// <summary>
        /// 永久失败后的冷却时间（毫秒）
        /// </summary>
        private const int DefaultPermanentFailureCooldownMs = 600000; // 10分钟
        
        /// <summary>
        /// 重连检查间隔（毫秒）
        /// </summary>
        private const int DefaultReconnectCheckIntervalMs = 5000; // 5秒
        
        #endregion

        #region 配置项

        private int baseDelayMs = DefaultBaseDelayMs;
        private int maxDelayMs = DefaultMaxDelayMs;
        private int maxReconnectAttempts = DefaultMaxReconnectAttempts;
        private double jitterFactor = DefaultJitterFactor;
        private int permanentFailureCooldownMs = DefaultPermanentFailureCooldownMs;
        private int reconnectCheckIntervalMs = DefaultReconnectCheckIntervalMs;

        /// <summary>
        /// 最大重连尝试次数（用于外部展示/判断）。
        /// </summary>
        public int MaxReconnectAttempts => maxReconnectAttempts;

        #endregion

        #region 私有成员
        
        private readonly ConcurrentDictionary<int, ReconnectState> _reconnectStates;
        private readonly System.Timers.Timer _reconnectTimer;
        private readonly Random _random;
        private readonly object _lockObject = new object();
        private int _reconnectTimerRunning;
        private volatile bool _disposed = false;
        
        #endregion

        #region 事件
        
        /// <summary>
        /// 重连尝试开始事件
        /// </summary>
        public event EventHandler<DeviceReconnectEventArgs> ReconnectAttemptStarted;
        
        /// <summary>
        /// 重连成功事件
        /// </summary>
        public event EventHandler<DeviceReconnectEventArgs> ReconnectSucceeded;
        
        /// <summary>
        /// 重连失败事件
        /// </summary>
        public event EventHandler<DeviceReconnectEventArgs> ReconnectFailed;
        
        /// <summary>
        /// 永久失败事件
        /// </summary>
        public event EventHandler<DeviceReconnectEventArgs> PermanentFailure;

        #endregion

        #region 构造函数
        
        public ReconnectManager()
        {
            _reconnectStates = new ConcurrentDictionary<int, ReconnectState>();
            _random = new Random();
            
            // 初始化重连定时器
            _reconnectTimer = new System.Timers.Timer(reconnectCheckIntervalMs);
            _reconnectTimer.Elapsed += OnReconnectTimerElapsed;
            _reconnectTimer.AutoReset = true;
            _reconnectTimer.Start();
        }

        #endregion

        #region 公共方法

        public void ApplyConfiguration(ServiceConfiguration.ReconnectOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            lock (_lockObject)
            {
                maxReconnectAttempts = Math.Max(1, options.MaxReconnectAttempts);
                baseDelayMs = Math.Max(1, options.BaseDelayMs);
                maxDelayMs = Math.Max(baseDelayMs, options.MaxDelayMs);
                permanentFailureCooldownMs = Math.Max(0, options.PermanentFailureCooldownMs);
                reconnectCheckIntervalMs = Math.Max(500, options.ReconnectCheckIntervalMs);

                _reconnectTimer.Interval = reconnectCheckIntervalMs;
            }
        }

        public void ApplyConfiguration(RuntimeReconnectOptions options)
        {
            ApplyConfiguration(LegacyRuntimeConfigurationMapper.ToLegacyOptions(options));
        }
        
        /// <summary>
        /// 安排设备重连
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="reason">重连原因</param>
        public void ScheduleReconnect(int deviceId, string reason = "")
        {
            if (_disposed) return;

            lock (_lockObject)
            {
                var state = _reconnectStates.GetOrAdd(deviceId, _ => new ReconnectState { DeviceId = deviceId });

                // 修复：检查是否在冷却期或已有待执行的重连任务
                if (state.IsInCooldown && DateTime.Now < state.CooldownUntil)
                {
                    ServiceLogger.Debug($"[重连管理器] 设备 {deviceId} 在冷却期内，跳过重连调度。");
                    return; // 还在冷却期，不处理
                }

                // 修复：检查是否已有待执行的重连任务（避免重复调度）
                if (state.NextRetry != DateTime.MinValue && DateTime.Now < state.NextRetry)
                {
                    ServiceLogger.Debug($"[重连管理器] 设备 {deviceId} 已有待执行重连任务，跳过重复调度。");
                    return; // 已有待执行的重连任务，不重复调度
                }

                // 重置冷却状态
                state.IsInCooldown = false;

                // 检查是否已达到最大重连次数
                if (state.Attempts >= maxReconnectAttempts)
                {
                    if (!state.IsPermanentFailure)
                    {
                        state.IsPermanentFailure = true;
                        state.IsInCooldown = true;
                        state.CooldownUntil = DateTime.Now.AddMilliseconds(permanentFailureCooldownMs);

                        ServiceLogger.Warn($"[重连管理器] 设备 {deviceId} 达到最大重连次数，进入冷却期。");
                        OnPermanentFailure(new DeviceReconnectEventArgs(
                            deviceId, state.Attempts, TimeSpan.Zero, true,
                            $"已达到最大重连次数({maxReconnectAttempts})，进入冷却期"));
                    }
                    return;
                }

                // 计算下次重连延迟
                var delay = GetNextRetryDelay(state.Attempts);
                state.CurrentDelay = delay;
                state.NextRetry = DateTime.Now.Add(delay);
                state.Attempts++;
                state.LastAttempt = DateTime.Now;

                ServiceLogger.Info($"[重连管理器] 设备 {deviceId} 安排第 {state.Attempts} 次重连，延迟 {delay.TotalSeconds} 秒。");

                // 触发重连尝试开始事件
                OnReconnectAttemptStarted(new DeviceReconnectEventArgs(
                    deviceId, state.Attempts, delay,
                    state.Attempts >= maxReconnectAttempts, reason));
            }
        }

        /// <summary>
        /// 重置设备重连状态（连接成功时调用）
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        public void ResetReconnectState(int deviceId)
        {
            lock (_lockObject)
            {
                if (_reconnectStates.TryGetValue(deviceId, out var state))
                {
                    var wasInFailure = state.IsPermanentFailure || state.Attempts > 0;

                    ServiceLogger.Info($"[重连管理器] 重置设备 {deviceId} 重连状态 - 之前尝试次数: {state.Attempts}。");

                    state.Attempts = 0;
                    state.IsPermanentFailure = false;
                    state.IsInCooldown = false;
                    state.CooldownUntil = DateTime.MinValue;
                    state.NextRetry = DateTime.MinValue;
                    state.CurrentDelay = TimeSpan.Zero;

                    if (wasInFailure)
                    {
                        ServiceLogger.Info($"[重连管理器] 设备 {deviceId} 连接恢复成功。");
                        OnReconnectSucceeded(new DeviceReconnectEventArgs(
                            deviceId, 0, TimeSpan.Zero, false, "连接恢复成功"));
                    }
                }
                else
                {
                    // 修复：即使状态不存在，也要确保创建一个干净的状态记录
                    ServiceLogger.Debug($"[重连管理器] 为设备 {deviceId} 创建新的重连状态记录。");
                    _reconnectStates.TryAdd(deviceId, new ReconnectState
                    {
                        DeviceId = deviceId,
                        Attempts = 0,
                        IsPermanentFailure = false,
                        IsInCooldown = false,
                        CooldownUntil = DateTime.MinValue,
                        NextRetry = DateTime.MinValue,
                        CurrentDelay = TimeSpan.Zero
                    });
                }
            }
        }

        /// <summary>
        /// 检查设备是否在冷却期
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <returns>是否在冷却期</returns>
        public bool IsInCooldown(int deviceId)
        {
            if (_reconnectStates.TryGetValue(deviceId, out var state))
            {
                return state.IsInCooldown && DateTime.Now < state.CooldownUntil;
            }
            return false;
        }

        /// <summary>
        /// 获取设备重连状态
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <returns>重连状态</returns>
        public ReconnectState GetReconnectState(int deviceId)
        {
            return _reconnectStates.TryGetValue(deviceId, out var state) ? state : null;
        }

        /// <summary>
        /// 获取下次重连延迟时间
        /// </summary>
        /// <param name="attempts">当前重连次数</param>
        /// <returns>延迟时间</returns>
        public TimeSpan GetNextRetryDelay(int attempts)
        {
            // 指数退避算法：base_delay * 2^attempts
            var delayMs = Math.Min(baseDelayMs * Math.Pow(2, attempts), maxDelayMs);
            
            // 添加抖动因子，避免重连风暴
            var jitter = (2 * _random.NextDouble() - 1) * jitterFactor; // 默认 -0.1 到 0.1
            delayMs = delayMs * (1 + jitter);
            
            // 确保最小延迟
            delayMs = Math.Max(delayMs, baseDelayMs);
            
            return TimeSpan.FromMilliseconds(delayMs);
        }

        /// <summary>
        /// 获取需要重连的设备列表
        /// </summary>
        /// <returns>设备ID列表</returns>
        public List<int> GetPendingReconnectDevices()
        {
            var pendingDevices = new List<int>();
            var now = DateTime.Now;

            foreach (var kvp in _reconnectStates)
            {
                var state = kvp.Value;
                
                // 跳过永久失败且在冷却期的设备
                if (state.IsPermanentFailure && state.IsInCooldown && now < state.CooldownUntil)
                {
                    continue;
                }
                
                // 重置永久失败状态（冷却期结束）
                if (state.IsPermanentFailure && state.IsInCooldown && now >= state.CooldownUntil)
                {
                    lock (_lockObject)
                    {
                        state.IsPermanentFailure = false;
                        state.IsInCooldown = false;
                        state.Attempts = 0; // 重置重连次数
                    }
                }
                
                // 检查是否到了重连时间
                if (!state.IsPermanentFailure && state.NextRetry != DateTime.MinValue && now >= state.NextRetry)
                {
                    pendingDevices.Add(kvp.Key);
                }
            }

            return pendingDevices;
        }

        /// <summary>
        /// 清理设备重连状态
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        public void RemoveDevice(int deviceId)
        {
            _reconnectStates.TryRemove(deviceId, out _);
        }

        #endregion

        #region 私有方法
        
        /// <summary>
        /// 重连定时器处理
        /// </summary>
        private void OnReconnectTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (_disposed)
            {
                return;
            }

            // 避免 async void 定时器重入
            if (Interlocked.Exchange(ref _reconnectTimerRunning, 1) == 1)
            {
                return;
            }

            _ = Task.Run(() =>
            {
                try
                {
                    var pendingDevices = GetPendingReconnectDevices();

                    if (pendingDevices.Count > 0)
                    {
                        // 通知设备连接管理器处理待重连设备
                        ProcessPendingReconnects?.Invoke(pendingDevices);
                    }
                }
                catch (Exception ex)
                {
                    // 记录错误但不中断定时器
                    ServiceLogger.Error("重连定时器处理异常。", ex);
                }
                finally
                {
                    Interlocked.Exchange(ref _reconnectTimerRunning, 0);
                }
            });
        }

        #endregion

        #region 事件触发方法
        
        protected virtual void OnReconnectAttemptStarted(DeviceReconnectEventArgs e)
        {
            ReconnectAttemptStarted?.Invoke(this, e);
        }

        protected virtual void OnReconnectSucceeded(DeviceReconnectEventArgs e)
        {
            ReconnectSucceeded?.Invoke(this, e);
        }

        protected virtual void OnReconnectFailed(DeviceReconnectEventArgs e)
        {
            ReconnectFailed?.Invoke(this, e);
        }

        protected virtual void OnPermanentFailure(DeviceReconnectEventArgs e)
        {
            PermanentFailure?.Invoke(this, e);
        }

        #endregion

        #region 委托和回调
        
        /// <summary>
        /// 处理待重连设备的委托
        /// </summary>
        public Action<List<int>> ProcessPendingReconnects { get; set; }

        #endregion

        #region IDisposable实现
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _reconnectTimer?.Stop();
                _reconnectTimer?.Dispose();
                _reconnectStates?.Clear();
                _disposed = true;
            }
        }

        #endregion
    }
}

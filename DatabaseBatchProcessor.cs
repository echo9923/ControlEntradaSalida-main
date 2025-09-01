using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 批量处理结果
    /// </summary>
    public class BatchProcessResult
    {
        /// <summary>
        /// 处理是否成功
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// 成功处理的记录数量
        /// </summary>
        public int ProcessedCount { get; set; }
        
        /// <summary>
        /// 失败的记录数量
        /// </summary>
        public int FailedCount { get; set; }
        
        /// <summary>
        /// 处理耗时（毫秒）
        /// </summary>
        public long ElapsedMilliseconds { get; set; }
        
        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// 失败的事件列表（用于重试）
        /// </summary>
        public List<AccessLogEvent> FailedEvents { get; set; } = new List<AccessLogEvent>();
    }

    /// <summary>
    /// 数据库批量处理器 - 优化门禁事件数据库写入性能
    /// 
    /// 核心功能：
    /// 1. 批量SQL生成和执行
    /// 2. 事务控制确保数据一致性  
    /// 3. 错误处理和重试机制
    /// 4. 性能监控和优化
    /// 
    /// 设计原则：
    /// - 批量操作减少数据库连接开销
    /// - 事务保证数据完整性
    /// - 参数化查询防止SQL注入
    /// - 异常隔离避免单个错误影响整批数据
    /// </summary>
    public class DatabaseBatchProcessor : IDisposable
    {
        private readonly string _connectionString;
        private volatile bool _disposed = false;
        private long _totalBatchesProcessed = 0;
        private long _totalEventsProcessed = 0;
        private long _totalProcessingTime = 0;

        /// <summary>
        /// 初始化批量处理器
        /// </summary>
        /// <param name="connectionString">数据库连接字符串</param>
        public DatabaseBatchProcessor(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <summary>
        /// 批量写入门禁事件到数据库
        /// </summary>
        /// <param name="events">事件列表</param>
        /// <returns>批量处理结果</returns>
        public async Task<BatchProcessResult> ProcessBatchAsync(AccessLogEvent[] events)
        {
            if (_disposed) 
                return new BatchProcessResult { Success = false, ErrorMessage = "BatchProcessor已释放" };
            
            if (events == null || events.Length == 0)
                return new BatchProcessResult { Success = true, ProcessedCount = 0 };

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new BatchProcessResult();

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        try
                        {
                            result.ProcessedCount = await ExecuteBatchInsertAsync(connection, transaction, events);
                            await transaction.CommitAsync();
                            result.Success = true;
                            
                            // 更新统计信息
                            System.Threading.Interlocked.Increment(ref _totalBatchesProcessed);
                            System.Threading.Interlocked.Add(ref _totalEventsProcessed, result.ProcessedCount);
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            result.Success = false;
                            result.ErrorMessage = $"批量写入失败: {ex.Message}";
                            result.FailedEvents.AddRange(events);
                            
                            Console.WriteLine($"[ERROR] 批量数据库写入失败: {ex.Message}");
                            
                            // 如果批量失败，尝试逐个写入以隔离错误
                            await ProcessIndividuallyAsync(connection, events, result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"数据库连接失败: {ex.Message}";
                result.FailedEvents.AddRange(events);
                Console.WriteLine($"[ERROR] 数据库连接异常: {ex.Message}");
            }
            finally
            {
                stopwatch.Stop();
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                System.Threading.Interlocked.Add(ref _totalProcessingTime, result.ElapsedMilliseconds);
                
                // 输出性能日志
                LogPerformance(result, events.Length);
            }

            return result;
        }

        /// <summary>
        /// 执行批量插入SQL
        /// </summary>
        /// <param name="connection">数据库连接</param>
        /// <param name="transaction">数据库事务</param>
        /// <param name="events">事件列表</param>
        /// <returns>成功插入的记录数</returns>
        private async Task<int> ExecuteBatchInsertAsync(MySqlConnection connection, MySqlTransaction transaction, AccessLogEvent[] events)
        {
            const int maxParametersPerBatch = 1000; // MySQL参数数量限制
            const int parametersPerRecord = 7; // 每条记录的参数数量
            int maxRecordsPerBatch = maxParametersPerBatch / parametersPerRecord;
            
            int totalProcessed = 0;
            
            // 分批处理以避免参数过多
            for (int i = 0; i < events.Length; i += maxRecordsPerBatch)
            {
                int batchSize = Math.Min(maxRecordsPerBatch, events.Length - i);
                var batchEvents = new ArraySegment<AccessLogEvent>(events, i, batchSize);
                
                string sql = BuildBatchInsertSql(batchSize);
                
                using (var command = new MySqlCommand(sql, connection, transaction))
                {
                    AddBatchParameters(command, batchEvents);
                    int affected = await command.ExecuteNonQueryAsync();
                    totalProcessed += affected;
                }
            }
            
            return totalProcessed;
        }

        /// <summary>
        /// 构建批量插入SQL语句
        /// </summary>
        /// <param name="recordCount">记录数量</param>
        /// <returns>批量插入SQL</returns>
        private string BuildBatchInsertSql(int recordCount)
        {
            var sql = new StringBuilder();
            sql.AppendLine("INSERT INTO access_logs (log_number, log_date, log_time, employee_id, device_id, event_type, created_at) VALUES");
            
            for (int i = 0; i < recordCount; i++)
            {
                if (i > 0) sql.AppendLine(",");
                sql.Append($"(@log_number{i}, @log_date{i}, @log_time{i}, @employee_id{i}, @device_id{i}, @event_type{i}, @created_at{i})");
            }
            
            return sql.ToString();
        }

        /// <summary>
        /// 添加批量插入参数
        /// </summary>
        /// <param name="command">MySQL命令</param>
        /// <param name="events">事件列表</param>
        private void AddBatchParameters(MySqlCommand command, ArraySegment<AccessLogEvent> events)
        {
            for (int i = 0; i < events.Count; i++)
            {
                var evt = events.Array[events.Offset + i];
                
                command.Parameters.AddWithValue($"@log_number{i}", evt.LogNumber);
                command.Parameters.AddWithValue($"@log_date{i}", evt.EventTime.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue($"@log_time{i}", evt.EventTime.ToString("HH:mm:ss"));
                command.Parameters.AddWithValue($"@employee_id{i}", evt.EmployeeId ?? "");
                command.Parameters.AddWithValue($"@device_id{i}", evt.DeviceId);
                command.Parameters.AddWithValue($"@event_type{i}", evt.EventType ?? "");
                command.Parameters.AddWithValue($"@created_at{i}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
        }

        /// <summary>
        /// 逐个处理事件（错误隔离）
        /// </summary>
        /// <param name="connection">数据库连接</param>
        /// <param name="events">事件列表</param>
        /// <param name="result">处理结果</param>
        private async Task ProcessIndividuallyAsync(MySqlConnection connection, AccessLogEvent[] events, BatchProcessResult result)
        {
            const string singleInsertSql = @"
                INSERT INTO access_logs (log_number, log_date, log_time, employee_id, device_id, event_type, created_at) 
                VALUES (@log_number, @log_date, @log_time, @employee_id, @device_id, @event_type, @created_at)";

            result.FailedEvents.Clear(); // 清空失败列表，重新统计
            int individualSuccess = 0;

            foreach (var evt in events)
            {
                try
                {
                    using (var command = new MySqlCommand(singleInsertSql, connection))
                    {
                        command.Parameters.AddWithValue("@log_number", evt.LogNumber);
                        command.Parameters.AddWithValue("@log_date", evt.EventTime.ToString("yyyy-MM-dd"));
                        command.Parameters.AddWithValue("@log_time", evt.EventTime.ToString("HH:mm:ss"));
                        command.Parameters.AddWithValue("@employee_id", evt.EmployeeId ?? "");
                        command.Parameters.AddWithValue("@device_id", evt.DeviceId);
                        command.Parameters.AddWithValue("@event_type", evt.EventType ?? "");
                        command.Parameters.AddWithValue("@created_at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                        await command.ExecuteNonQueryAsync();
                        individualSuccess++;
                    }
                }
                catch (Exception ex)
                {
                    result.FailedEvents.Add(evt);
                    Console.WriteLine($"[ERROR] 单条记录写入失败: {evt.GetDeduplicationKey()}, 错误: {ex.Message}");
                }
            }

            result.ProcessedCount = individualSuccess;
            result.FailedCount = result.FailedEvents.Count;
            result.Success = individualSuccess > 0; // 至少成功一条就算部分成功

            Console.WriteLine($"[INDIVIDUAL] 逐个处理完成: 成功{individualSuccess}条, 失败{result.FailedCount}条");
        }

        /// <summary>
        /// 记录性能日志
        /// </summary>
        /// <param name="result">处理结果</param>
        /// <param name="totalEvents">总事件数</param>
        private void LogPerformance(BatchProcessResult result, int totalEvents)
        {
            double throughput = result.ElapsedMilliseconds > 0 ? 
                (double)result.ProcessedCount / result.ElapsedMilliseconds * 1000 : 0;

            string logMessage = $"[BATCH] 处理结果: 成功={result.ProcessedCount}, 失败={result.FailedCount}, " +
                               $"耗时={result.ElapsedMilliseconds}ms, 吞吐量={throughput:F1}条/秒";

            Console.WriteLine(logMessage);

            // 性能告警
            if (result.ElapsedMilliseconds > 5000) // 超过5秒告警
            {
                Console.WriteLine($"[WARNING] 批量处理耗时过长: {result.ElapsedMilliseconds}ms, 建议检查数据库性能");
            }

            if (throughput < 100) // 吞吐量低于100条/秒告警
            {
                Console.WriteLine($"[WARNING] 数据库写入吞吐量偏低: {throughput:F1}条/秒, 建议优化数据库配置");
            }
        }

        /// <summary>
        /// 获取处理器统计信息
        /// </summary>
        /// <returns>统计信息字符串</returns>
        public string GetStatistics()
        {
            long totalBatches = _totalBatchesProcessed;
            long totalEvents = _totalEventsProcessed; 
            long totalTime = _totalProcessingTime;

            double avgBatchSize = totalBatches > 0 ? (double)totalEvents / totalBatches : 0;
            double avgProcessingTime = totalBatches > 0 ? (double)totalTime / totalBatches : 0;
            double overallThroughput = totalTime > 0 ? (double)totalEvents / totalTime * 1000 : 0;

            return $"批量处理器统计: 总批次={totalBatches}, 总事件={totalEvents}, " +
                   $"平均批次大小={avgBatchSize:F1}, 平均耗时={avgProcessingTime:F1}ms, " +
                   $"整体吞吐量={overallThroughput:F1}条/秒";
        }

        /// <summary>
        /// 测试数据库连接
        /// </summary>
        /// <returns>连接是否成功</returns>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    return connection.State == ConnectionState.Open;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] 数据库连接测试失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 重置统计信息
        /// </summary>
        public void ResetStatistics()
        {
            _totalBatchesProcessed = 0;
            _totalEventsProcessed = 0;
            _totalProcessingTime = 0;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Console.WriteLine("[DISPOSE] 数据库批量处理器已释放资源");
            }
        }
    }
}
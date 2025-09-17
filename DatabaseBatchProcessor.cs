using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 鎵归噺澶勭悊缁撴灉
    /// </summary>
    public class BatchProcessResult
    {
        /// <summary>
        /// 澶勭悊鏄惁鎴愬姛
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// 鎴愬姛澶勭悊鐨勮褰曟暟閲?
        /// </summary>
        public int ProcessedCount { get; set; }
        
        /// <summary>
        /// 澶辫触鐨勮褰曟暟閲?
        /// </summary>
        public int FailedCount { get; set; }
        
        /// <summary>
        /// 澶勭悊鑰楁椂锛堟绉掞級
        /// </summary>
        public long ElapsedMilliseconds { get; set; }
        
        /// <summary>
        /// 閿欒淇℃伅
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// 澶辫触鐨勪簨浠跺垪琛紙鐢ㄤ簬閲嶈瘯锛?
        /// </summary>
        public List<AccessLogEvent> FailedEvents { get; set; } = new List<AccessLogEvent>();
    }

    /// <summary>
    /// 鏁版嵁搴撴壒閲忓鐞嗗櫒 - 浼樺寲闂ㄧ浜嬩欢鏁版嵁搴撳啓鍏ユ€ц兘
    /// 
    /// 鏍稿績鍔熻兘锛?
    /// 1. 鎵归噺SQL鐢熸垚鍜屾墽琛?
    /// 2. 浜嬪姟鎺у埗纭繚鏁版嵁涓€鑷存€? 
    /// 3. 閿欒澶勭悊鍜岄噸璇曟満鍒?
    /// 4. 鎬ц兘鐩戞帶鍜屼紭鍖?
    /// 
    /// 璁捐鍘熷垯锛?
    /// - 鎵归噺鎿嶄綔鍑忓皯鏁版嵁搴撹繛鎺ュ紑閿€
    /// - 浜嬪姟淇濊瘉鏁版嵁瀹屾暣鎬?
    /// - 鍙傛暟鍖栨煡璇㈤槻姝QL娉ㄥ叆
    /// - 寮傚父闅旂閬垮厤鍗曚釜閿欒褰卞搷鏁存壒鏁版嵁
    /// </summary>
    public class DatabaseBatchProcessor : IDisposable
    {
        private readonly string _connectionString;
        private volatile bool _disposed = false;
        private long _totalBatchesProcessed = 0;
        private long _totalEventsProcessed = 0;
        private long _totalProcessingTime = 0;

        /// <summary>
        /// 鍒濆鍖栨壒閲忓鐞嗗櫒
        /// </summary>
        /// <param name="connectionString">鏁版嵁搴撹繛鎺ュ瓧绗︿覆</param>
        public DatabaseBatchProcessor(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <summary>
        /// 鎵归噺鍐欏叆闂ㄧ浜嬩欢鍒版暟鎹簱
        /// </summary>
        /// <param name="events">浜嬩欢鍒楄〃</param>
        /// <returns>鎵归噺澶勭悊缁撴灉</returns>
        public async Task<BatchProcessResult> ProcessBatchAsync(AccessLogEvent[] events)
        {
            if (_disposed)
                return new BatchProcessResult { Success = false, ErrorMessage = "BatchProcessor 已释放" };
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
                            
                            // 鏇存柊缁熻淇℃伅
                            System.Threading.Interlocked.Increment(ref _totalBatchesProcessed);
                            System.Threading.Interlocked.Add(ref _totalEventsProcessed, result.ProcessedCount);
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            result.Success = false;
                            result.ErrorMessage = $"鎵归噺鍐欏叆澶辫触: {ex.Message}";
                            result.FailedEvents.AddRange(events);
                            
                            Console.WriteLine($"[ERROR] 鎵归噺鏁版嵁搴撳啓鍏ュけ璐? {ex.Message}");
                            
                            // 濡傛灉鎵归噺澶辫触锛屽皾璇曢€愪釜鍐欏叆浠ラ殧绂婚敊璇?
                            await ProcessIndividuallyAsync(connection, events, result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"鏁版嵁搴撹繛鎺ュけ璐? {ex.Message}";
                result.FailedEvents.AddRange(events);
                Console.WriteLine($"[ERROR] 鏁版嵁搴撹繛鎺ュ紓甯? {ex.Message}");
            }
            finally
            {
                stopwatch.Stop();
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                System.Threading.Interlocked.Add(ref _totalProcessingTime, result.ElapsedMilliseconds);
                
                // 杈撳嚭鎬ц兘鏃ュ織
                LogPerformance(result, events.Length);
            }

            return result;
        }

        /// <summary>
        /// 鎵ц鎵归噺鎻掑叆SQL
        /// </summary>
        /// <param name="connection">鏁版嵁搴撹繛鎺?/param>
        /// <param name="transaction">鏁版嵁搴撲簨鍔?/param>
        /// <param name="events">浜嬩欢鍒楄〃</param>
        /// <returns>鎴愬姛鎻掑叆鐨勮褰曟暟</returns>
        private async Task<int> ExecuteBatchInsertAsync(MySqlConnection connection, MySqlTransaction transaction, AccessLogEvent[] events)
        {
            const int maxParametersPerBatch = 1000; // MySQL鍙傛暟鏁伴噺闄愬埗
            const int parametersPerRecord = 8; // 每条记录的参数数量
            int maxRecordsPerBatch = maxParametersPerBatch / parametersPerRecord;
            
            int totalProcessed = 0;
            
            // 鍒嗘壒澶勭悊浠ラ伩鍏嶅弬鏁拌繃澶?
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
        /// 鏋勫缓鎵归噺鎻掑叆SQL璇彞
        /// </summary>
        /// <param name="recordCount">璁板綍鏁伴噺</param>
        /// <returns>鎵归噺鎻掑叆SQL</returns>
        private string BuildBatchInsertSql(int recordCount)
        {
            var sql = new StringBuilder();
            sql.AppendLine("INSERT INTO access_logs (sequence_number, employee_number, employee_name, device_number, device_name, event_type, event_time, remote_host_address) VALUES");
            
            for (int i = 0; i < recordCount; i++)
            {
                if (i > 0) sql.AppendLine(",");
                sql.Append($"(@sequence_number{i}, @employee_number{i}, @employee_name{i}, @device_number{i}, @device_name{i}, @event_type{i}, @event_time{i}, @remote_host_address{i})");
            }
            
            return sql.ToString();
        }

        /// <summary>
        /// 娣诲姞鎵归噺鎻掑叆鍙傛暟
        /// </summary>
        /// <param name="command">MySQL鍛戒护</param>
        /// <param name="events">浜嬩欢鍒楄〃</param>
        private void AddBatchParameters(MySqlCommand command, ArraySegment<AccessLogEvent> events)
        {
            for (int i = 0; i < events.Count; i++)
            {
                var evt = events.Array[events.Offset + i];
                
                command.Parameters.AddWithValue($"@sequence_number{i}", evt.SequenceNumber);
                command.Parameters.AddWithValue($"@employee_number{i}", evt.EmployeeNumber ?? string.Empty);
                command.Parameters.AddWithValue($"@employee_name{i}", evt.EmployeeName ?? string.Empty);
                command.Parameters.AddWithValue($"@device_number{i}", evt.DeviceNumber);
                command.Parameters.AddWithValue($"@device_name{i}", evt.DeviceName ?? string.Empty);
                command.Parameters.AddWithValue($"@event_type{i}", evt.EventType ?? string.Empty);
                command.Parameters.AddWithValue($"@event_time{i}", evt.EventTime.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue($"@remote_host_address{i}", evt.RemoteHostAddress ?? string.Empty);
            }
        }

        /// <summary>
        /// 閫愪釜澶勭悊浜嬩欢锛堥敊璇殧绂伙級
        /// </summary>
        /// <param name="connection">鏁版嵁搴撹繛鎺?/param>
        /// <param name="events">浜嬩欢鍒楄〃</param>
        /// <param name="result">澶勭悊缁撴灉</param>
        private async Task ProcessIndividuallyAsync(MySqlConnection connection, AccessLogEvent[] events, BatchProcessResult result)
        {
            const string singleInsertSql = @"
                INSERT INTO access_logs (sequence_number, employee_number, employee_name, device_number, device_name, event_type, event_time, remote_host_address) 
                VALUES (@sequence_number, @employee_number, @employee_name, @device_number, @device_name, @event_type, @event_time, @remote_host_address)";

            result.FailedEvents.Clear(); // 娓呯┖澶辫触鍒楄〃锛岄噸鏂
            int individualSuccess = 0;

            foreach (var evt in events)
            {
                try
                {
                    using (var command = new MySqlCommand(singleInsertSql, connection))
                    {
                        command.Parameters.AddWithValue("@sequence_number", evt.SequenceNumber);
                        command.Parameters.AddWithValue("@employee_number", evt.EmployeeNumber ?? string.Empty);
                        command.Parameters.AddWithValue("@employee_name", evt.EmployeeName ?? string.Empty);
                        command.Parameters.AddWithValue("@device_number", evt.DeviceNumber);
                        command.Parameters.AddWithValue("@device_name", evt.DeviceName ?? string.Empty);
                        command.Parameters.AddWithValue("@event_type", evt.EventType ?? string.Empty);
                        command.Parameters.AddWithValue("@event_time", evt.EventTime.ToString("yyyy-MM-dd HH:mm:ss"));

                        command.Parameters.AddWithValue("@remote_host_address", evt.RemoteHostAddress ?? string.Empty);

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
            result.Success = individualSuccess > 0; // 至少成功一条则视为部分成功

            Console.WriteLine($"[INDIVIDUAL] 单个处理完成: 成功{individualSuccess}条 失败{result.FailedCount}条");
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
                Console.WriteLine($"[WARNING] 数据库写入吞吐量较低: {throughput:F1}条/秒，建议优化数据库配置");
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

            return $"批量处理器统计: 总批次数={totalBatches}, 总事件数={totalEvents}, " +
                   $"平均批量大小={avgBatchSize:F1}, 平均耗时={avgProcessingTime:F1}ms, " +
                   $"总体吞吐量={overallThroughput:F1}条/秒";
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
                Console.WriteLine("[DISPOSE] 数据库批处理器已释放资源");
            }
        }
    }
}




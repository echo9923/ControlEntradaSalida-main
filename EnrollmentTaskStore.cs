using System;
using System.Collections.Concurrent;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 简单的内存级任务状态存储，用于采集/下发状态查询。
    /// </summary>
    public static class EnrollmentTaskStore
    {
        private static readonly ConcurrentDictionary<string, EnrollmentTaskStatus> Store =
            new ConcurrentDictionary<string, EnrollmentTaskStatus>(StringComparer.OrdinalIgnoreCase);

        public static string CreateTask(string employeeId, string action)
        {
            string id = Guid.NewGuid().ToString("N");
            Store[id] = new EnrollmentTaskStatus
            {
                TaskId = id,
                EmployeeId = employeeId,
                Action = action,
                Status = "Processing",
                Message = "任务已创建"
            };
            return id;
        }

        public static void Complete(string taskId, bool success, string message = null, string errorCode = null)
        {
            if (!Store.TryGetValue(taskId, out EnrollmentTaskStatus status))
            {
                return;
            }

            status.Status = success ? "Succeeded" : "Failed";
            status.Message = message ?? status.Message;
            status.ErrorCode = success ? null : (errorCode ?? "UNKNOWN");
            Store[taskId] = status;
        }

        public static EnrollmentTaskStatus Get(string taskId)
        {
            Store.TryGetValue(taskId, out EnrollmentTaskStatus status);
            return status;
        }
    }

    public sealed class EnrollmentTaskStatus
    {
        public string TaskId { get; set; }
        public string EmployeeId { get; set; }
        public string Action { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string ErrorCode { get; set; }
    }
}

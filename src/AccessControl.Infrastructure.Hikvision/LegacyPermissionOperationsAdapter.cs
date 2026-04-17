using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ControlEntradaSalida.Application.Abstractions;
using ControlEntradaSalida.Application.Models;
using ControlEntradaSalida.Domain.Common;

namespace ControlEntradaSalida.Infrastructure.Hikvision
{
    public sealed class LegacyPermissionOperationsAdapter : ILegacyPermissionOperations
    {
        private readonly global::ControlEntradaSalida.PermissionRefreshManager refreshManager;

        public LegacyPermissionOperationsAdapter(global::ControlEntradaSalida.PermissionRefreshManager refreshManager)
        {
            this.refreshManager = refreshManager;
        }

        public Task<OperationResult> SyncPermissionsAsync(
            IReadOnlyList<PermissionUpdateCommandItem> items,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            var updates = items.Select(item => new global::ControlEntradaSalida.PermissionUpdateInfo(item.EmployeeId, item.PermissionCode)).ToList();
            global::ControlEntradaSalida.PermissionRefreshSummary summary = refreshManager.RefreshPermissionsForEmployees(updates);
            return Task.FromResult(BuildPermissionResult(summary));
        }

        public Task<OperationResult> SyncPersonsAsync(
            IReadOnlyList<PersonSyncCommandItem> items,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            var persons = items.Select(item => new global::ControlEntradaSalida.PersonSyncRequest
            {
                EmployeeId = item.EmployeeId,
                FullName = item.FullName,
                Gender = item.Gender,
                Enabled = item.Enabled,
                ValidFrom = item.ValidFrom,
                ValidTo = item.ValidTo,
                FaceImageBytes = item.FaceImageBytes,
                FaceImageFormat = item.FaceImageFormat
            }).ToList();

            global::ControlEntradaSalida.PersonSyncSummary summary = refreshManager.SyncPersonsToConnectedDevices(persons);
            return Task.FromResult(BuildPersonResult(summary));
        }

        public Task<OperationResult> DeleteFacesAsync(
            IReadOnlyList<string> employeeIds,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            global::ControlEntradaSalida.FaceOperationSummary summary = refreshManager.DeleteFacesOnDevices(employeeIds);
            return Task.FromResult(BuildFaceResult(summary, "人脸删除完成。", "部分离线设备已排队重试人脸删除。", "人脸删除部分失败。", "人脸删除失败。"));
        }

        public Task<OperationResult> DeletePersonsAsync(
            IReadOnlyList<string> employeeIds,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            global::ControlEntradaSalida.PersonDeleteSummary summary = refreshManager.DeletePersonsFromDevices(employeeIds);
            return Task.FromResult(BuildDeletePersonResult(summary));
        }

        public Task<OperationResult> GetFacesAsync(
            IReadOnlyList<string> employeeIds,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            global::ControlEntradaSalida.FaceOperationSummary summary = refreshManager.GetFacesFromDevices(employeeIds);
            return Task.FromResult(BuildFaceResult(summary, "人脸查询完成。", "部分离线设备已排队重试人脸查询。", "人脸查询部分失败。", "人脸查询失败。"));
        }

        public Task<OperationResult> GetEnrollmentStatusAsync(
            EnrollmentStatusQuery query,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            global::ControlEntradaSalida.EnrollmentTaskStatus status = global::ControlEntradaSalida.EnrollmentTaskStore.Get(query.TaskId);
            if (status == null)
            {
                return Task.FromResult(OperationResult.Failure(
                    global::ControlEntradaSalida.GrpcErrorCodes.NotFound,
                    string.Format(CultureInfo.InvariantCulture, "任务 {0} 不存在或已过期。", query.TaskId),
                    new[] { string.Format(CultureInfo.InvariantCulture, "任务 {0} 不存在或已过期。", query.TaskId) }));
            }

            return Task.FromResult(OperationResult.Success(
                global::ControlEntradaSalida.GrpcErrorCodes.Ok,
                "查询成功。",
                new
                {
                    taskId = status.TaskId,
                    employeeId = status.EmployeeId,
                    action = status.Action,
                    status = status.Status,
                    message = status.Message,
                    errorCode = status.ErrorCode
                }));
        }

        public Task<IReadOnlyList<OperationResult>> CaptureFaceStreamAsync(
            CaptureFaceStreamCommand command,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            string taskId = global::ControlEntradaSalida.EnrollmentTaskStore.CreateTask(command.EmployeeId, "CaptureFaceStream");
            global::ControlEntradaSalida.FaceCaptureResult capture = refreshManager.CaptureFaceFromEnrollmentDevice();

            if (!capture.Success)
            {
                global::ControlEntradaSalida.EnrollmentTaskStore.Complete(taskId, false, capture.ErrorMessage, "CAPTURE_FAILED");
                IReadOnlyList<OperationResult> failedFrames = new[]
                {
                    OperationResult.Failure(
                        ResolveCaptureErrorCode(capture),
                        capture.ErrorMessage,
                        new[] { capture.ErrorMessage },
                        CreateErrorDetails(command.EmployeeId, capture),
                        new
                        {
                            taskId,
                            employeeId = command.EmployeeId,
                            status = "Failed",
                            message = capture.ErrorMessage,
                            errorCode = "CAPTURE_FAILED"
                        })
                };

                return Task.FromResult(failedFrames);
            }

            global::ControlEntradaSalida.EnrollmentTaskStore.Complete(taskId, true, "采集完成");
            IReadOnlyList<OperationResult> frames = new[]
            {
                OperationResult.Success(
                    global::ControlEntradaSalida.GrpcErrorCodes.Ok,
                    "采集成功。",
                    new
                    {
                        taskId,
                        employeeId = command.EmployeeId,
                        frameIndex = 1,
                        faceImageBase64 = capture.FaceImageBase64,
                        faceImageFormat = capture.Format,
                        qualityScore = (int?)null,
                        recommend = true
                    })
            };

            return Task.FromResult(frames);
        }

        private static OperationResult BuildPermissionResult(global::ControlEntradaSalida.PermissionRefreshSummary summary)
        {
            ResolveSummaryMeta(
                summary.UsersUpdated + summary.UsersSkipped,
                summary.UsersFailed,
                summary.QueuedCount,
                summary.Errors.Count > 0,
                "权限刷新完成。",
                "部分离线设备已排队重试权限刷新。",
                "权限刷新部分失败。",
                "权限刷新失败。",
                out bool success,
                out string code,
                out string message);

            return CreateResult(
                success,
                code,
                message,
                summary.Errors,
                summary.ErrorDetails,
                new
                {
                    total = summary.TotalUsers,
                    updated = summary.UsersUpdated,
                    skipped = summary.UsersSkipped,
                    failed = summary.UsersFailed,
                    queued = summary.QueuedCount,
                    queuedDetails = summary.QueuedDetails
                });
        }

        private static OperationResult BuildPersonResult(global::ControlEntradaSalida.PersonSyncSummary summary)
        {
            ResolveSummaryMeta(
                summary.SuccessfulPersons,
                summary.FailedPersons,
                summary.QueuedCount,
                summary.Errors.Count > 0,
                "人员下发完成。",
                "部分离线设备已排队重试人员下发。",
                "人员下发部分失败。",
                "人员下发失败。",
                out bool success,
                out string code,
                out string message);

            return CreateResult(
                success,
                code,
                message,
                summary.Errors,
                summary.ErrorDetails,
                new
                {
                    total = summary.TotalPersons,
                    succeeded = summary.SuccessfulPersons,
                    failed = summary.FailedPersons,
                    queued = summary.QueuedCount,
                    facesUploaded = summary.FacesUploaded,
                    targetDevices = summary.TargetDevices,
                    queuedDetails = summary.QueuedDetails
                });
        }

        private static OperationResult BuildFaceResult(
            global::ControlEntradaSalida.FaceOperationSummary summary,
            string successMessage,
            string queuedMessage,
            string partialMessage,
            string failedMessage)
        {
            ResolveSummaryMeta(
                summary.Succeeded,
                summary.Failed,
                summary.QueuedCount,
                summary.Errors.Count > 0,
                successMessage,
                queuedMessage,
                partialMessage,
                failedMessage,
                out bool success,
                out string code,
                out string message);

            return CreateResult(
                success,
                code,
                message,
                summary.Errors,
                summary.ErrorDetails,
                new
                {
                    total = summary.Total,
                    succeeded = summary.Succeeded,
                    failed = summary.Failed,
                    queued = summary.QueuedCount,
                    targetDevices = summary.TargetDevices,
                    queuedDetails = summary.QueuedDetails,
                    items = summary.Items.Select(item => new
                    {
                        employeeId = item.EmployeeId,
                        success = item.Success,
                        faceImageBase64 = item.FaceImageBase64,
                        rawResponse = item.RawResponse,
                        error = item.Error
                    }).ToArray()
                });
        }

        private static OperationResult BuildDeletePersonResult(global::ControlEntradaSalida.PersonDeleteSummary summary)
        {
            ResolveSummaryMeta(
                summary.Succeeded,
                summary.Failed,
                summary.QueuedCount,
                summary.Errors.Count > 0,
                "人员删除完成。",
                "部分离线设备已排队重试人员删除。",
                "人员删除部分失败。",
                "人员删除失败。",
                out bool success,
                out string code,
                out string message);

            return CreateResult(
                success,
                code,
                message,
                summary.Errors,
                summary.ErrorDetails,
                new
                {
                    total = summary.Total,
                    succeeded = summary.Succeeded,
                    failed = summary.Failed,
                    queued = summary.QueuedCount,
                    targetDevices = summary.TargetDevices,
                    queuedDetails = summary.QueuedDetails,
                    items = summary.Items.Select(item => new
                    {
                        employeeId = item.EmployeeId,
                        success = item.Success,
                        successDevices = item.SuccessDevices,
                        failedDevices = item.FailedDevices,
                        deviceErrors = item.DeviceErrors.ToArray()
                    }).ToArray()
                });
        }

        private static OperationResult CreateResult(
            bool success,
            string code,
            string message,
            IEnumerable<string> errors,
            IEnumerable<global::ControlEntradaSalida.GrpcErrorDetail> details,
            object payload)
        {
            var mappedDetails = details?.Select(detail => new OperationErrorDetail
            {
                EmployeeId = detail.EmployeeId,
                DeviceId = detail.DeviceId,
                DeviceName = detail.DeviceName,
                DeviceIp = detail.DeviceIp,
                Code = detail.Code,
                Message = detail.Message,
                RawResponse = detail.RawResponse
            });

            return success
                ? OperationResult.Success(code, message, payload, errors, mappedDetails)
                : OperationResult.Failure(code, message, errors, mappedDetails, payload);
        }

        private static IEnumerable<OperationErrorDetail> CreateErrorDetails(string employeeId, global::ControlEntradaSalida.FaceCaptureResult capture)
        {
            if (!capture.DeviceId.HasValue && string.IsNullOrWhiteSpace(capture.DeviceName) && string.IsNullOrWhiteSpace(capture.DeviceIp))
            {
                return Array.Empty<OperationErrorDetail>();
            }

            return new[]
            {
                new OperationErrorDetail
                {
                    EmployeeId = employeeId,
                    DeviceId = capture.DeviceId,
                    DeviceName = capture.DeviceName,
                    DeviceIp = capture.DeviceIp,
                    Code = ResolveCaptureErrorCode(capture),
                    Message = capture.ErrorMessage
                }
            };
        }

        private static string ResolveCaptureErrorCode(global::ControlEntradaSalida.FaceCaptureResult capture)
        {
            string message = capture?.ErrorMessage ?? string.Empty;
            if (message.IndexOf("200KB", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return global::ControlEntradaSalida.GrpcErrorCodes.FaceTooLarge;
            }

            return global::ControlEntradaSalida.GrpcErrorCodes.DeviceError;
        }

        private static void ResolveSummaryMeta(
            int succeeded,
            int failed,
            int queued,
            bool hasErrors,
            string successMessage,
            string queuedMessage,
            string partialMessage,
            string failedMessage,
            out bool success,
            out string code,
            out string message)
        {
            if (failed <= 0 && succeeded <= 0 && queued <= 0 && hasErrors)
            {
                success = false;
                code = global::ControlEntradaSalida.GrpcErrorCodes.Failed;
                message = failedMessage;
                return;
            }

            if (failed <= 0 && queued > 0)
            {
                success = false;
                code = global::ControlEntradaSalida.GrpcErrorCodes.PartialSuccess;
                message = queuedMessage;
                return;
            }

            if (failed <= 0)
            {
                success = true;
                code = global::ControlEntradaSalida.GrpcErrorCodes.Ok;
                message = successMessage;
                return;
            }

            if (succeeded > 0 || queued > 0)
            {
                success = false;
                code = global::ControlEntradaSalida.GrpcErrorCodes.PartialSuccess;
                message = partialMessage;
                return;
            }

            success = false;
            code = global::ControlEntradaSalida.GrpcErrorCodes.Failed;
            message = failedMessage;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 归纳离线补偿状态迁移，避免不同路径遗漏字段清理。
    /// </summary>
    internal static class DeviceOperationRetryStateBehavior
    {
        internal static void ApplyQueuedPersonRetry(DeviceOperationRetryState state, string personPayload, bool hasFace, string facePayload)
        {
            state.PersonPending = true;
            state.PersonPayload = personPayload;
            state.DeleteFacePending = false;
            state.DeletePersonPending = false;

            if (hasFace)
            {
                state.FacePending = true;
                state.FacePayload = facePayload;
            }
            else
            {
                state.FacePending = false;
                state.FacePayload = null;
            }
        }

        internal static void ApplyPersonSuccessAndClearFaceRetry(DeviceOperationRetryState state)
        {
            state.PersonPending = false;
            state.PersonPayload = null;
            state.FacePending = false;
            state.FacePayload = null;
            state.DeleteFacePending = false;
            state.DeletePersonPending = false;
        }

        internal static void ApplyQueuedDeleteFaceRetry(DeviceOperationRetryState state)
        {
            state.FacePending = false;
            state.FacePayload = null;
            state.DeleteFacePending = true;
            state.DeletePersonPending = false;
        }

        internal static void ApplyDeleteFaceSuccess(DeviceOperationRetryState state, bool clearDeletePersonPending)
        {
            state.FacePending = false;
            state.FacePayload = null;
            state.DeleteFacePending = false;

            if (clearDeletePersonPending)
            {
                state.DeletePersonPending = false;
            }
        }
    }

    /// <summary>
    /// 统一设备连接策略，区分前台请求与后台补偿是否允许主动重连。
    /// </summary>
    internal static class DeviceConnectionRetryPolicy
    {
        internal static bool IsDeviceReady(bool isConnected, int userId)
        {
            return isConnected && userId >= 0;
        }

        internal static bool ShouldAttemptReconnect(bool isConnected, int userId, bool isReconnecting, bool allowReconnect)
        {
            if (IsDeviceReady(isConnected, userId))
            {
                return false;
            }

            return allowReconnect && !isReconnecting;
        }
    }

    /// <summary>
    /// 统一识别可进入离线补偿队列的瞬时设备通信失败。
    /// </summary>
    internal static class DeviceOperationRetryFailurePolicy
    {
        private static readonly HashSet<uint> RetryableSdkErrorCodes = new HashSet<uint>
        {
            1,
            7,
            8,
            9,
            10,
            12,
            13,
            15,
            20
        };

        private static readonly string[] RetryableKeywords =
        {
            "设备未连接",
            "连接失败",
            "发送失败",
            "接收数据失败",
            "等待超时",
            "超时",
            "设备忙",
            "socket",
            "timeout",
            "disconnected",
            "connection",
            "network"
        };

        private static readonly Regex SdkErrorCodeRegex = new Regex(
            @"(?:error\s*code\s*=\s*|错误码[:：]?\s*)(\d+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        internal static bool IsRetryableSdkError(uint errorCode)
        {
            return RetryableSdkErrorCodes.Contains(errorCode);
        }

        internal static bool IsRetryableTransportFailure(string primaryMessage, string secondaryMessage = null)
        {
            if (TryExtractSdkErrorCode(primaryMessage, out uint primaryCode))
            {
                return IsRetryableSdkError(primaryCode);
            }

            if (TryExtractSdkErrorCode(secondaryMessage, out uint secondaryCode))
            {
                return IsRetryableSdkError(secondaryCode);
            }

            return ContainsRetryableKeyword(primaryMessage) || ContainsRetryableKeyword(secondaryMessage);
        }

        internal static bool IsRetryableRemoteConfigStatus(int status, string responseContent)
        {
            if (status == (int)HCNetSDK.NET_SDK_SENDWITHRECV_STATUS.NET_SDK_CONFIG_STATUS_NEEDWAIT
                || status == (int)HCNetSDK.NET_SDK_SENDWITHRECV_STATUS.NET_SDK_CONFIG_STATUS_EXCEPTION)
            {
                return true;
            }

            if (status == (int)HCNetSDK.NET_SDK_SENDWITHRECV_STATUS.NET_SDK_CONFIG_STATUS_FAILED)
            {
                return string.IsNullOrWhiteSpace(responseContent) || IsRetryableTransportFailure(responseContent);
            }

            return false;
        }

        internal static bool TryExtractSdkErrorCode(string raw, out uint errorCode)
        {
            errorCode = 0;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            Match match = SdkErrorCodeRegex.Match(raw);
            if (!match.Success)
            {
                return false;
            }

            return uint.TryParse(match.Groups[1].Value, out errorCode);
        }

        private static bool ContainsRetryableKeyword(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            foreach (string keyword in RetryableKeywords)
            {
                if (raw.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// 删除类接口的幂等语义判断。
    /// </summary>
    internal static class DeviceDeleteResponsePolicy
    {
        private static readonly string[] MissingKeywords =
        {
            "not found",
            "notfound",
            "not exist",
            "notexist",
            "does not exist",
            "no such",
            "不存在",
            "未找到",
            "未录入",
            "record not exist"
        };

        internal static bool IsDeleteFaceAlreadyAbsent(string responseContent)
        {
            if (string.IsNullOrWhiteSpace(responseContent))
            {
                return false;
            }

            if (ContainsMissingKeyword(responseContent))
            {
                return true;
            }

            try
            {
                JToken root = JToken.Parse(responseContent);
                return ContainsMissingKeyword(root.Value<string>("statusString"))
                    || ContainsMissingKeyword(root.Value<string>("subStatusCode"))
                    || ContainsMissingKeyword(root.Value<string>("errorMsg"))
                    || ContainsMissingKeyword(root.Value<string>("statusCode"));
            }
            catch
            {
                return false;
            }
        }

        private static bool ContainsMissingKeyword(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            foreach (string keyword in MissingKeywords)
            {
                if (raw.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

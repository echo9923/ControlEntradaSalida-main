using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ControlEntradaSalida.Tests
{
    [TestClass]
    public sealed class DeviceOperationRetryBehaviorTests
    {
        [TestMethod]
        public void QueuePersonRetry_NoFace_ClearsLegacyFaceRetryState()
        {
            var state = new ControlEntradaSalida.DeviceOperationRetryState
            {
                FacePending = true,
                FacePayload = "legacy-face",
                DeleteFacePending = true,
                DeletePersonPending = true
            };

            ControlEntradaSalida.DeviceOperationRetryStateBehavior.ApplyQueuedPersonRetry(
                state,
                personPayload: "person-v2",
                hasFace: false,
                facePayload: null);

            Assert.IsTrue(state.PersonPending);
            Assert.AreEqual("person-v2", state.PersonPayload);
            Assert.IsFalse(state.FacePending);
            Assert.IsNull(state.FacePayload);
            Assert.IsFalse(state.DeleteFacePending);
            Assert.IsFalse(state.DeletePersonPending);
        }

        [TestMethod]
        public void QueuePersonRetry_WithFace_PreservesFaceRetryPayload()
        {
            var state = new ControlEntradaSalida.DeviceOperationRetryState
            {
                FacePending = true,
                FacePayload = "legacy-face"
            };

            ControlEntradaSalida.DeviceOperationRetryStateBehavior.ApplyQueuedPersonRetry(
                state,
                personPayload: "person-v2",
                hasFace: true,
                facePayload: "face-v2");

            Assert.IsTrue(state.PersonPending);
            Assert.AreEqual("person-v2", state.PersonPayload);
            Assert.IsTrue(state.FacePending);
            Assert.AreEqual("face-v2", state.FacePayload);
            Assert.IsFalse(state.DeleteFacePending);
            Assert.IsFalse(state.DeletePersonPending);
        }

        [TestMethod]
        public void MarkPersonAppliedAndClearFaceRetry_ClearsLegacyFaceRetryState()
        {
            var state = new ControlEntradaSalida.DeviceOperationRetryState
            {
                PersonPending = true,
                PersonPayload = "person-v2",
                FacePending = true,
                FacePayload = "face-v2",
                DeleteFacePending = true,
                DeletePersonPending = true
            };

            ControlEntradaSalida.DeviceOperationRetryStateBehavior.ApplyPersonSuccessAndClearFaceRetry(state);

            Assert.IsFalse(state.PersonPending);
            Assert.IsNull(state.PersonPayload);
            Assert.IsFalse(state.FacePending);
            Assert.IsNull(state.FacePayload);
            Assert.IsFalse(state.DeleteFacePending);
            Assert.IsFalse(state.DeletePersonPending);
        }

        [TestMethod]
        public void QueueDeleteFaceRetry_ClearsStaleDeletePersonPending()
        {
            var state = new ControlEntradaSalida.DeviceOperationRetryState
            {
                DeleteFacePending = false,
                DeletePersonPending = true,
                FacePending = true,
                FacePayload = "legacy-face"
            };

            ControlEntradaSalida.DeviceOperationRetryStateBehavior.ApplyQueuedDeleteFaceRetry(state);

            Assert.IsTrue(state.DeleteFacePending);
            Assert.IsFalse(state.DeletePersonPending);
            Assert.IsFalse(state.FacePending);
            Assert.IsNull(state.FacePayload);
        }

        [TestMethod]
        public void DeleteFaceSuccess_ForFaceOnlyDelete_ClearsStaleDeletePersonPending()
        {
            var state = new ControlEntradaSalida.DeviceOperationRetryState
            {
                DeleteFacePending = true,
                DeletePersonPending = true,
                FacePending = true,
                FacePayload = "legacy-face"
            };

            ControlEntradaSalida.DeviceOperationRetryStateBehavior.ApplyDeleteFaceSuccess(
                state,
                clearDeletePersonPending: true);

            Assert.IsFalse(state.DeleteFacePending);
            Assert.IsFalse(state.DeletePersonPending);
            Assert.IsFalse(state.FacePending);
            Assert.IsNull(state.FacePayload);
        }

        [TestMethod]
        public void DeleteFaceSuccess_ForFullDelete_PreservesDeletePersonPending()
        {
            var state = new ControlEntradaSalida.DeviceOperationRetryState
            {
                DeleteFacePending = true,
                DeletePersonPending = true,
                FacePending = true,
                FacePayload = "legacy-face"
            };

            ControlEntradaSalida.DeviceOperationRetryStateBehavior.ApplyDeleteFaceSuccess(
                state,
                clearDeletePersonPending: false);

            Assert.IsFalse(state.DeleteFacePending);
            Assert.IsTrue(state.DeletePersonPending);
            Assert.IsFalse(state.FacePending);
            Assert.IsNull(state.FacePayload);
        }

        [TestMethod]
        public void QueueableWrite_WhenDeviceOffline_DoesNotAttemptImmediateReconnect()
        {
            bool shouldReconnect = ControlEntradaSalida.DeviceConnectionRetryPolicy.ShouldAttemptReconnect(
                isConnected: false,
                userId: -1,
                isReconnecting: false,
                allowReconnect: false);

            Assert.IsFalse(shouldReconnect);
        }

        [TestMethod]
        public void ProcessQueuedState_WhenReplaying_AllowsReconnect()
        {
            bool shouldReconnect = ControlEntradaSalida.DeviceConnectionRetryPolicy.ShouldAttemptReconnect(
                isConnected: false,
                userId: -1,
                isReconnecting: false,
                allowReconnect: true);

            Assert.IsTrue(shouldReconnect);
        }

        [TestMethod]
        public void CaptureOrQueryFace_WhenOffline_StillUsesImmediateConnectBehavior()
        {
            bool offlineReconnect = ControlEntradaSalida.DeviceConnectionRetryPolicy.ShouldAttemptReconnect(
                isConnected: false,
                userId: -1,
                isReconnecting: false,
                allowReconnect: true);
            bool reconnectWhileBusy = ControlEntradaSalida.DeviceConnectionRetryPolicy.ShouldAttemptReconnect(
                isConnected: false,
                userId: -1,
                isReconnecting: true,
                allowReconnect: true);
            bool readyDeviceReconnect = ControlEntradaSalida.DeviceConnectionRetryPolicy.ShouldAttemptReconnect(
                isConnected: true,
                userId: 9,
                isReconnecting: false,
                allowReconnect: true);

            Assert.IsTrue(offlineReconnect);
            Assert.IsFalse(reconnectWhileBusy);
            Assert.IsFalse(readyDeviceReconnect);
        }

        [TestMethod]
        public void RetryableTransportFailure_WithTransientSdkError_ReturnsTrue()
        {
            bool retryable = ControlEntradaSalida.DeviceOperationRetryFailurePolicy.IsRetryableTransportFailure(
                "NET_DVR_STDXMLConfig failed, error code= 9");

            Assert.IsTrue(retryable);
        }

        [TestMethod]
        public void RetryableTransportFailure_WithParameterError_ReturnsFalse()
        {
            bool retryable = ControlEntradaSalida.DeviceOperationRetryFailurePolicy.IsRetryableTransportFailure(
                "NET_DVR_STDXMLConfig failed, error code= 18");

            Assert.IsFalse(retryable);
        }

        [TestMethod]
        public void RetryableRemoteConfigStatus_NeedWait_ReturnsTrue()
        {
            bool retryable = ControlEntradaSalida.DeviceOperationRetryFailurePolicy.IsRetryableRemoteConfigStatus(
                (int)ControlEntradaSalida.HCNetSDK.NET_SDK_SENDWITHRECV_STATUS.NET_SDK_CONFIG_STATUS_NEEDWAIT,
                string.Empty);

            Assert.IsTrue(retryable);
        }

        [TestMethod]
        public void DeleteFaceAlreadyAbsent_WithMissingKeywords_ReturnsTrue()
        {
            bool alreadyAbsent = ControlEntradaSalida.DeviceDeleteResponsePolicy.IsDeleteFaceAlreadyAbsent(
                "{\"statusCode\":0,\"errorMsg\":\"face not found\"}");

            Assert.IsTrue(alreadyAbsent);
        }

        [TestMethod]
        public void DeleteFaceAlreadyAbsent_WithDifferentFailure_ReturnsFalse()
        {
            bool alreadyAbsent = ControlEntradaSalida.DeviceDeleteResponsePolicy.IsDeleteFaceAlreadyAbsent(
                "{\"statusCode\":0,\"errorMsg\":\"permission denied\"}");

            Assert.IsFalse(alreadyAbsent);
        }

        [TestMethod]
        public void ResolveSummaryMeta_QueuedOnly_ReturnsPartialSuccess()
        {
            object[] args =
            {
                0,
                0,
                1,
                false,
                "操作完成。",
                "任务已进入补偿队列。",
                "部分失败。",
                "操作失败。",
                null,
                null,
                null
            };

            InvokeResolveSummaryMeta(args);

            Assert.IsFalse((bool)args[8]);
            Assert.AreEqual(ControlEntradaSalida.GrpcErrorCodes.PartialSuccess, args[9]);
            Assert.AreEqual("任务已进入补偿队列。", args[10]);
        }

        [TestMethod]
        public void ResolveSummaryMeta_AllSucceeded_ReturnsOk()
        {
            object[] args =
            {
                2,
                0,
                0,
                false,
                "操作完成。",
                "任务已进入补偿队列。",
                "部分失败。",
                "操作失败。",
                null,
                null,
                null
            };

            InvokeResolveSummaryMeta(args);

            Assert.IsTrue((bool)args[8]);
            Assert.AreEqual(ControlEntradaSalida.GrpcErrorCodes.Ok, args[9]);
            Assert.AreEqual("操作完成。", args[10]);
        }

        private static void InvokeResolveSummaryMeta(object[] args)
        {
            MethodInfo method = typeof(ControlEntradaSalida.PermissionUpdateGrpcServer)
                .GetMethod("ResolveSummaryMeta", BindingFlags.NonPublic | BindingFlags.Static);

            Assert.IsNotNull(method, "未找到 ResolveSummaryMeta 私有方法。");
            method.Invoke(null, args);
        }
    }
}

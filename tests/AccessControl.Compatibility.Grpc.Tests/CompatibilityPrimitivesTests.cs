using ControlEntradaSalida.Compatibility.Grpc;
using ControlEntradaSalida.Compatibility.Grpc.Parsing;
using ControlEntradaSalida.Domain.Common;
using Grpc.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;

namespace AccessControl.Compatibility.Grpc.Tests
{
    [TestClass]
    public sealed class CompatibilityPrimitivesTests
    {
        [TestMethod]
        public void GrpcEnvelopeFactory_Create_WritesStandardEnvelope()
        {
            var result = OperationResult.Success(
                code: "ok",
                message: "done",
                payload: new
                {
                    total = 1
                });

            string json = GrpcEnvelopeFactory.Create("req-1", result);
            JObject root = JObject.Parse(json);

            Assert.AreEqual("req-1", root.Value<string>("requestId"));
            Assert.IsTrue(root.Value<bool>("success"));
            Assert.AreEqual("ok", root.Value<string>("code"));
            Assert.AreEqual("done", root.Value<string>("message"));
            Assert.AreEqual(1, root.Value<int>("total"));
        }

        [TestMethod]
        public void GrpcErrorMapper_MapToStatusCode_UsesExpectedGrpcStatus()
        {
            Assert.AreEqual(StatusCode.InvalidArgument, GrpcErrorMapper.MapToStatusCode("invalid_argument"));
            Assert.AreEqual(StatusCode.NotFound, GrpcErrorMapper.MapToStatusCode("not_found"));
            Assert.AreEqual(StatusCode.Unauthenticated, GrpcErrorMapper.MapToStatusCode("unauthenticated"));
        }

        [TestMethod]
        public void SyncPermissionsRequestParser_Parse_HandlesItemsAlias()
        {
            string request = "{\"items\":[{\"employee_id\":\"EMP-1\",\"permission_code\":3}]}";

            var items = SyncPermissionsRequestParser.Parse(request);

            Assert.HasCount(1, items);
            Assert.AreEqual("EMP-1", items[0].EmployeeId);
            Assert.AreEqual(3, items[0].PermissionCode);
        }

        [TestMethod]
        public void SyncPersonsRequestParser_Parse_StripsDataUriPrefixFromFaceImage()
        {
            string request = "{\"items\":[{\"employeeId\":\"EMP-2\",\"faceImageBase64\":\"data:image/jpeg;base64,QQ==\"}]}";

            var items = SyncPersonsRequestParser.Parse(request);

            Assert.HasCount(1, items);
            CollectionAssert.AreEqual(new byte[] { 65 }, items[0].FaceImageBytes);
        }

        [TestMethod]
        public void PayloadMasker_Mask_ScrubsSensitiveFields()
        {
            string masked = PayloadMasker.Mask("{\"password\":\"secret\",\"faceImageBase64\":\"abc\",\"normal\":\"ok\"}");
            JObject root = JObject.Parse(masked);

            Assert.AreEqual("***", root.Value<string>("password"));
            Assert.AreEqual("***", root.Value<string>("faceImageBase64"));
            Assert.AreEqual("ok", root.Value<string>("normal"));
        }

        [TestMethod]
        public void PermissionSyncCompatibilityService_MethodDefinitions_PreserveLegacyServiceContract()
        {
            AssertMethod(typeof(PermissionSyncCompatibilityService), "SyncPermissionsMethod", "permission.PermissionSyncService", "SyncPermissions", MethodType.Unary);
            AssertMethod(typeof(PermissionSyncCompatibilityService), "SyncPersonsMethod", "permission.PermissionSyncService", "SyncPersons", MethodType.Unary);
            AssertMethod(typeof(PermissionSyncCompatibilityService), "DeleteFacesMethod", "permission.PermissionSyncService", "DeleteFaces", MethodType.Unary);
            AssertMethod(typeof(PermissionSyncCompatibilityService), "DeletePersonsMethod", "permission.PermissionSyncService", "DeletePersons", MethodType.Unary);
            AssertMethod(typeof(PermissionSyncCompatibilityService), "GetFacesMethod", "permission.PermissionSyncService", "GetFaces", MethodType.Unary);
            AssertMethod(typeof(PermissionSyncCompatibilityService), "GetEnrollmentStatusMethod", "permission.PermissionSyncService", "GetEnrollmentStatus", MethodType.Unary);
            AssertMethod(typeof(PermissionSyncCompatibilityService), "CaptureFaceStreamMethod", "permission.PermissionSyncService", "CaptureFaceStream", MethodType.ServerStreaming);
        }

        [TestMethod]
        public void DeviceManagementCompatibilityService_MethodDefinitions_PreserveLegacyServiceContract()
        {
            AssertMethod(typeof(DeviceManagementCompatibilityService), "GetDeviceStatusMethod", "device.AccessControlService", "GetDeviceStatus", MethodType.Unary);
            AssertMethod(typeof(DeviceManagementCompatibilityService), "AddDeviceMethod", "device.AccessControlService", "AddDevice", MethodType.Unary);
            AssertMethod(typeof(DeviceManagementCompatibilityService), "DeleteDeviceMethod", "device.AccessControlService", "DeleteDevice", MethodType.Unary);
            AssertMethod(typeof(DeviceManagementCompatibilityService), "DisconnectDeviceMethod", "device.AccessControlService", "DisconnectDevice", MethodType.Unary);
            AssertMethod(typeof(DeviceManagementCompatibilityService), "ReconnectDeviceMethod", "device.AccessControlService", "ReconnectDevice", MethodType.Unary);
        }

        private static void AssertMethod(Type ownerType, string fieldName, string expectedServiceName, string expectedMethodName, MethodType expectedMethodType)
        {
            FieldInfo field = ownerType.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);

            var method = (Method<string, string>)field.GetValue(null);
            Assert.IsNotNull(method);
            Assert.AreEqual(expectedServiceName, method.ServiceName);
            Assert.AreEqual(expectedMethodName, method.Name);
            Assert.AreEqual(expectedMethodType, method.Type);
        }
    }
}

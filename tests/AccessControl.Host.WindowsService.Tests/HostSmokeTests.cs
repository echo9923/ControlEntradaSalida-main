using System.Collections.Generic;
using ControlEntradaSalida.Application.Abstractions;
using ControlEntradaSalida.Host.WindowsService;
using ControlEntradaSalida.Infrastructure.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AccessControl.Host.WindowsService.Tests
{
    [TestClass]
    public sealed class HostSmokeTests
    {
        [TestMethod]
        public void HostModeResolver_Resolve_ReturnsInteractiveWhenConsoleSwitchProvided()
        {
            HostRunMode mode = HostModeResolver.Resolve(isUserInteractive: false, args: new[] { "--console" });

            Assert.AreEqual(HostRunMode.Interactive, mode);
        }

        [TestMethod]
        public void CompositeServiceRuntime_Stop_StopsComponentsInReverseOrder()
        {
            var trace = new List<string>();
            var runtime = new CompositeServiceRuntime(new[]
            {
                new FakeHostedComponent("one", trace),
                new FakeHostedComponent("two", trace)
            });

            runtime.Start();
            runtime.Stop();

            CollectionAssert.AreEqual(
                new[]
                {
                    "start:one",
                    "start:two",
                    "stop:two",
                    "stop:one"
                },
                trace);
        }

        [TestMethod]
        public void LegacyConfigurationProvider_Current_ReturnsStronglyTypedRuntimeConfiguration()
        {
            IConfigurationProvider provider = new LegacyConfigurationProvider();

            RuntimeServiceConfiguration configuration = provider.Current;

            Assert.IsNotNull(configuration);
            Assert.IsGreaterThan(0, configuration.GrpcListenPort);
            Assert.IsNotNull(configuration.FaceEvent);
            Assert.IsNotNull(configuration.DeviceConnection);
            Assert.IsNotNull(configuration.Reconnect);
            Assert.IsNotNull(configuration.DeviceOperationRetry);
        }

        private sealed class FakeHostedComponent : IHostedComponent
        {
            private readonly string name;
            private readonly List<string> trace;

            public FakeHostedComponent(string name, List<string> trace)
            {
                this.name = name;
                this.trace = trace;
            }

            public void Start()
            {
                trace.Add("start:" + name);
            }

            public void Stop()
            {
                trace.Add("stop:" + name);
            }
        }
    }
}

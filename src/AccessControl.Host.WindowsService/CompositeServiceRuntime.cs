using System;
using System.Collections.Generic;
using System.Linq;

namespace ControlEntradaSalida.Host.WindowsService
{
    public sealed class CompositeServiceRuntime : IDisposable
    {
        private readonly IReadOnlyList<IHostedComponent> components;

        public CompositeServiceRuntime(IEnumerable<IHostedComponent> components)
        {
            this.components = (components ?? Enumerable.Empty<IHostedComponent>()).ToArray();
        }

        public void Start()
        {
            foreach (IHostedComponent component in components)
            {
                component.Start();
            }
        }

        public void Stop()
        {
            for (int index = components.Count - 1; index >= 0; index--)
            {
                components[index].Stop();
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}

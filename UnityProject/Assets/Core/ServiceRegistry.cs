using System;
using System.Collections.Generic;

namespace Game.Core
{
    public sealed class ServiceRegistry : IServiceRegistry
    {
        private readonly Dictionary<Type, object> services = new Dictionary<Type, object>();

        public void Register<T>(T service) where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (services.ContainsKey(typeof(T)))
                throw new InvalidOperationException($"{typeof(T).Name} is already registered. A contract has exactly one owner.");
            services.Add(typeof(T), service);
        }

        public T Resolve<T>() where T : class
        {
            if (TryResolve<T>(out var service)) return service;
            throw new InvalidOperationException(
                $"{typeof(T).Name} is not registered. Install the feature that owns it earlier in the boot order.");
        }

        public bool TryResolve<T>(out T service) where T : class
        {
            if (services.TryGetValue(typeof(T), out var found))
            {
                service = (T)found;
                return true;
            }
            service = null;
            return false;
        }
    }
}

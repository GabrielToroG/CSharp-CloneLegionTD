using System;
using System.Collections.Generic;

namespace LegionTDClone.CompositionRoot
{
    // Minimal IoC Container since we are not using Zenject/VContainer for this MVP
    public class Container
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public void RegisterSingleton<TInterface, TImplementation>(TImplementation implementation) where TImplementation : TInterface
        {
            _services[typeof(TInterface)] = implementation;
        }

        public void RegisterSingleton<TImplementation>(TImplementation implementation)
        {
            _services[typeof(TImplementation)] = implementation;
        }

        public T Resolve<T>()
        {
            if (_services.TryGetValue(typeof(T), out var implementation))
            {
                return (T)implementation;
            }
            throw new Exception($"Service of type {typeof(T).Name} is not registered.");
        }
    }
}

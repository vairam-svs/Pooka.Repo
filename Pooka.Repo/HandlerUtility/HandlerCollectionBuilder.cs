using Pooka.Repo.Utility;

namespace Pooka.Repo.HandlerUtility
{
    using System;
    using System.Collections.Generic;

    public class HandlerCollectionBuilder
    {
        private readonly string _handlerAssemblyName;

        private readonly string _handlersNamespace;

        private readonly Func<Type, Type> _keyFromHandlerFn;

        private readonly Dictionary<Type, Type> _registeredHandlers = new Dictionary<Type, Type>(new HandlerTypeComparer());

        private bool _handlersRead;

        public HandlerCollectionBuilder(string handlerAssemblyName, string handlersNamespace, Func<Type, Type> keyFromHandlerFn)
        {
            Param.CheckStringNotNullOrEmpty(handlerAssemblyName, nameof(handlerAssemblyName));
            Param.CheckStringNotNullOrEmpty(handlersNamespace, nameof(handlersNamespace));
            Param.CheckNotNull(keyFromHandlerFn, nameof(keyFromHandlerFn));

            _handlerAssemblyName = handlerAssemblyName;
            _handlersNamespace = handlersNamespace;
            _keyFromHandlerFn = keyFromHandlerFn;
        }

        public string HandlerAssemblyName => _handlerAssemblyName;

        public void Register(Type commandType, Type commandHandlerType)
        {
            _registeredHandlers.Add(commandType, commandHandlerType);
        }

        public Type TryGetHandlerType(Type keyType)
        {
            Type handlerType;
            EnsureHandlersRead();
            if (_registeredHandlers.TryGetValue(keyType, out handlerType))
            {
                return handlerType;
            }

            if (keyType.IsGenericType)
            {
                if (_registeredHandlers.TryGetValue(keyType.GetGenericTypeDefinition(), out handlerType))
                {
                    return handlerType;
                }
            }

            return null;
        }

        private void EnsureHandlersRead()
        {
            if (_handlersRead)
            {
                return;
            }

            lock (_registeredHandlers)
            {
                // Double lock protection
                if (!_handlersRead)
                {
                    var handlerReader = new HandlerReader(_keyFromHandlerFn, _handlerAssemblyName, _handlersNamespace);
                    handlerReader.ReadHandlers((handlerKey, handlerType) => _registeredHandlers.Add(handlerKey, handlerType));
                    _handlersRead = true;
                }
            }
        }
    }
}
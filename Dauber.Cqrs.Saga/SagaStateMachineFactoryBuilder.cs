using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Dauber.Core.Container;
using Dauber.Cqrs.Contracts;

namespace Dauber.Cqrs.Saga
{
    public class SagaStateMachineFactoryBuilder
    {
        private readonly IContainer _container;
        private IEnumerable<string> _assembliesToScan;

        public SagaStateMachineFactoryBuilder(IContainer container)
        {
            _container = container;
        }

        public SagaStateMachineFactoryBuilder WithStateMachinesInAssembiles(IEnumerable<string> assembliesToScan)
        {
            _assembliesToScan = assembliesToScan;
            return this;
        }

        public ISagaStateMachineEventDispatcher Build()
        {
            return new SagaStateMachineEventDispatcher(ScanAssemblies(_assembliesToScan), _container);
        }        

        private IDictionary<Type, Type> ScanAssemblies(IEnumerable<string> assembliesToScan)
        {
            var stateMachineTypes = new ConcurrentDictionary<Type, Type>() as IDictionary<Type, Type>;

            var assemblies = AppDomain
                .CurrentDomain
                .GetAssemblies()
                .Where(assembly => assembliesToScan.Contains(assembly.GetName().Name));

            var stateMachinesInAssemblies = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.DoesTypeImplementInterface(typeof (IStateMachine<>)))
                .ToList();

            foreach (var stateMachineType in stateMachinesInAssemblies)
            {
                if (stateMachineTypes.Values.Contains(stateMachineType))
                    continue;

                var interfaces = stateMachineType.GetInterfaces().Where(i => i.IsGenericType);
                var sagaStateTypes = interfaces.Select(i => i.GetGenericArguments()[0]).Distinct();
                
                foreach (var sagaStateType in sagaStateTypes)
                {                    
                    stateMachineTypes.Add(sagaStateType, stateMachineType);
                }
            }

            return stateMachineTypes;
        }
    }
}
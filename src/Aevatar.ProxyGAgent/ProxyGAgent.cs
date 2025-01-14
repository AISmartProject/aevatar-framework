using System.Reflection;
using System.Runtime.Loader;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.ProxyGAgent;
using Aevatar.ProxyGAgent.Sdk;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Aevatar.ProxyGAgent;

[GAgent("proxy")]
public class ProxyGAgent : GAgentBase<ProxyGAgentState, ProxyStateLogEvent, ProxyGAgentEvent, ProxyGAgentInitialization>
{
    public ProxyGAgent(ILogger<ProxyGAgent> logger) : base(logger)
    {
        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve!;
    }

    private static Assembly? OnAssemblyResolve(object sender, ResolveEventArgs args)
    {
        var folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (folderPath == null) return null;
        var assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
        if (!File.Exists(assemblyPath)) return null;
        var assembly = Assembly.LoadFrom(assemblyPath);
        return assembly;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This is a proxy GAgent for executing C# code.");
    }

    public override async Task InitializeAsync(ProxyGAgentInitialization initializeDto)
    {
        RaiseEvent(new SetEventHandlerCode
        {
            EventHandlerCode = initializeDto.EventHandlerCode
        });
        await ConfirmEvents();
    }

    [GenerateSerializer]
    public class SetEventHandlerCode : ProxyStateLogEvent
    {
        [Id(0)] public byte[] EventHandlerCode { get; set; }
    }

    protected override void GAgentTransitionState(ProxyGAgentState state, StateLogEventBase<ProxyStateLogEvent> @event)
    {
        switch (@event)
        {
            case SetEventHandlerCode setEventHandlerCode:
                state.EventHandlerCode = setEventHandlerCode.EventHandlerCode;
                break;
        }
    }

    [AllEventHandler]
    public async Task ExecuteEventHandlersAsync(EventWrapperBase eventData)
    {
        var assembly = Assembly.Load(State.EventHandlerCode);
        var handlerTypes = GetHandlerTypes(assembly);

        foreach (var handlerType in handlerTypes)
        {
            var interfaceType = GetHandlerInterfaceType(handlerType);
            var eventType = interfaceType.GetGenericArguments()[0];

            if (IsMatchingEventType(eventData, eventType))
            {
                var handlerInstance = Activator.CreateInstance(handlerType);
                var handleMethod = interfaceType.GetMethod(nameof(IGAgentEventHandler<EventBase>.HandleEventAsync));

                if (handleMethod != null)
                {
                    await InvokeHandleMethodAsync(handleMethod, handlerInstance!, eventData, eventType);
                }
            }
        }
    }

    private IEnumerable<Type> GetHandlerTypes(Assembly assembly)
    {
        var handlerInterfaceType = typeof(IGAgentEventHandler<>);
        return assembly.GetTypes()
            .Where(t => t.GetInterfaces()
                            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType) &&
                        t is { IsInterface: false, IsAbstract: false });
    }

    private Type GetHandlerInterfaceType(Type handlerType)
    {
        var handlerInterfaceType = typeof(IGAgentEventHandler<>);
        return handlerType.GetInterfaces()
            .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType);
    }

    private bool IsMatchingEventType(EventWrapperBase eventData, Type eventType)
    {
        return eventData.GetType().IsGenericType &&
               eventData.GetType().GetGenericTypeDefinition() == typeof(EventWrapper<>) &&
               ((EventWrapper<EventBase>)eventData).Event.GetType().FullName == eventType.FullName;
    }

    private async Task InvokeHandleMethodAsync(MethodInfo handleMethod, object handlerInstance,
        EventWrapperBase eventData, Type eventType)
    {
        dynamic eventWrapper = eventData;
        var eventJson = JsonConvert.SerializeObject(eventWrapper.Event);
        var eventObject = JsonConvert.DeserializeObject(eventJson, eventType);

        var result = await (Task<EventHandleResult>)handleMethod.Invoke(handlerInstance, new object[] { eventObject })!;
        if (!result.StateLogEventList.IsNullOrEmpty())
        {
            foreach (var stateLogEvent in result.StateLogEventList)
            {
                RaiseEvent(stateLogEvent);
            }
        }

        if (!result.GAgentEventBase.IsNullOrEmpty())
        {
            foreach (var eventBase in result.GAgentEventBase)
            {
                await PublishAsync(eventBase);
            }
        }
    }
}
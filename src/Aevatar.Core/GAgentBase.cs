using System.Collections.Concurrent;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans.EventSourcing;
using Orleans.Providers;
using Orleans.Streams;

namespace Aevatar.Core;

[GAgent]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public abstract class
    GAgentBase<TState, TStateLogEvent>
    : GAgentBase<TState, TStateLogEvent, EventBase, ConfigurationBase>
    where TState : StateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>;

[GAgent]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public abstract class
    GAgentBase<TState, TStateLogEvent, TEvent>
    : GAgentBase<TState, TStateLogEvent, TEvent, ConfigurationBase>
    where TState : StateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase;

[GAgent]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public abstract partial class
    GAgentBase<TState, TStateLogEvent, TEvent, TConfiguration> 
    : JournaledGrain<TState, StateLogEventBase<TStateLogEvent>>, IStateGAgent<TState>
    where TState : StateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
    where TConfiguration : ConfigurationBase
{
    protected IStreamProvider StreamProvider => this.GetStreamProvider(AevatarCoreConstants.StreamProvider);

    public ILogger Logger { get; set; } = NullLogger.Instance;

    private readonly List<EventWrapperBaseAsyncObserver> _observers = [];

    private readonly AevatarOptions _aevatarOptions;

    protected GAgentBase()
    {
        _aevatarOptions = ServiceProvider.GetRequiredService<IOptionsSnapshot<AevatarOptions>>().Value;
    }

    public async Task ActivateAsync()
    {
        await Task.Yield();
    }

    public async Task RegisterAsync(IGAgent gAgent)
    {
        var guid = gAgent.GetPrimaryKey();
        if (gAgent.GetGrainId() == this.GetGrainId())
        {
            Logger.LogError($"Cannot register GAgent with same GrainId.");
            return;
        }

        await AddChildAsync(gAgent.GetGrainId());
        await gAgent.SubscribeToAsync(this);
        await OnRegisterAgentAsync(gAgent.GetGrainId());
    }

    public Task SubscribeToAsync(IGAgent gAgent)
    {
        return SetParentAsync(gAgent.GetGrainId());
    }

    public Task UnsubscribeFromAsync(IGAgent gAgent)
    {
        return ClearParentAsync(gAgent.GetGrainId());
    }

    public async Task UnregisterAsync(IGAgent gAgent)
    {
        await RemoveChildAsync(gAgent.GetGrainId());
        await gAgent.UnsubscribeFromAsync(this);
        await OnUnregisterAgentAsync(gAgent.GetGrainId());
    }

    public virtual Task<List<Type>?> GetAllSubscribedEventsAsync(bool includeBaseHandlers = false)
    {
        var eventHandlerMethods = GetEventHandlerMethods(GetType());
        eventHandlerMethods = eventHandlerMethods.Where(m =>
            m.Name != nameof(ForwardEventAsync) && m.Name != nameof(PerformConfigAsync));
        var handlingTypes = eventHandlerMethods
            .Select(m => m.GetParameters().First().ParameterType);
        if (!includeBaseHandlers)
        {
            handlingTypes = handlingTypes.Where(t => t != typeof(RequestAllSubscriptionsEvent));
        }

        return Task.FromResult(handlingTypes.ToList())!;
    }

    public Task<List<GrainId>> GetChildrenAsync()
    {
        return Task.FromResult(State.Children);
    }

    public Task<GrainId> GetParentAsync()
    {
        return Task.FromResult(State.Parent ?? default);
    }

    public virtual Task<Type?> GetConfigurationTypeAsync()
    {
        return Task.FromResult(typeof(TConfiguration))!;
    }

    public async Task ConfigAsync(ConfigurationBase configuration)
    {
        if (configuration is TConfiguration config)
        {
            await PerformConfigAsync(config);
        }
    }

    protected virtual Task PerformConfigAsync(TConfiguration configuration)
    {
        return Task.CompletedTask;
    }

    [EventHandler]
    public async Task<SubscribedEventListEvent> HandleRequestAllSubscriptionsEventAsync(
        RequestAllSubscriptionsEvent request)
    {
        return await GetGroupSubscribedEventListEvent();
    }

    private async Task<SubscribedEventListEvent> GetGroupSubscribedEventListEvent()
    {
        var gAgentList = State.Children.Select(grainId => GrainFactory.GetGrain<IGAgent>(grainId)).ToList();

        if (gAgentList.IsNullOrEmpty())
        {
            return new SubscribedEventListEvent
            {
                Value = new Dictionary<Type, List<Type>>(),
                GAgentType = GetType()
            };
        }

        if (gAgentList.Any(grain => grain == null))
        {
            // Only happened on test environment.
            throw new InvalidOperationException("One or more grains in gAgentList are null.");
        }

        var dict = new ConcurrentDictionary<Type, List<Type>>();
        foreach (var gAgent in gAgentList.AsParallel())
        {
            var eventList = await gAgent.GetAllSubscribedEventsAsync();
            dict[gAgent.GetType()] = eventList ?? [];
        }

        return new SubscribedEventListEvent
        {
            Value = dict.ToDictionary(),
            GAgentType = GetType()
        };
    }

    [AllEventHandler]
    protected virtual async Task ForwardEventAsync(EventWrapperBase eventWrapper)
    {
        Logger.LogInformation(
            $"{this.GetGrainId().ToString()} is forwarding event downwards: {JsonConvert.SerializeObject((EventWrapper<TEvent>)eventWrapper)}");
        await SendEventDownwardsAsync((EventWrapper<TEvent>)eventWrapper);
    }

    protected virtual Task OnRegisterAgentAsync(GrainId agentGuid)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnUnregisterAgentAsync(GrainId agentGuid)
    {
        return Task.CompletedTask;
    }

    public abstract Task<string> GetDescriptionAsync();

    public Task<TState> GetStateAsync()
    {
        return Task.FromResult(State);
    }

    public sealed override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
        await BaseOnActivateAsync(cancellationToken);
        await OnGAgentActivateAsync(cancellationToken);
    }

    protected virtual Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        // Derived classes can override this method.
        return Task.CompletedTask;
    }

    private async Task BaseOnActivateAsync(CancellationToken cancellationToken)
    {
        // This must be called first to initialize Observers field.
        await UpdateObserverListAsync(GetType());
        await InitializeOrResumeEventWrapperBaseStreamAsync();
        await InitializeOrResumeStateLogEventBaseStreamAsync();
        await InitializeOrResumeStateBaseStreamAsync();
    }

    private async Task InitializeOrResumeEventWrapperBaseStreamAsync()
    {
        var streamOfThisGAgent = GetEventWrapperBaseStream(this.GetGrainId().ToString());
        var handles = await streamOfThisGAgent.GetAllSubscriptionHandles();
        var asyncObserver = new GAgentAsyncObserver(_observers);
        if (handles.Count > 0)
        {
            foreach (var handle in handles)
            {
                await handle.ResumeAsync(asyncObserver);
            }
        }
        else
        {
            await streamOfThisGAgent.SubscribeAsync(asyncObserver);
        }
    }

    private async Task InitializeOrResumeStateLogEventBaseStreamAsync()
    {
        var stream = GetStateLogEventBaseStream(this.GetGrainId().ToString());
        var handles = await stream.GetAllSubscriptionHandles();
        var stateLogEventHandlers = ServiceProvider.GetRequiredService<IEnumerable<IGAgentStateLogEventHandler>>();
        var asyncObserver = new StateLogEventBaseAsyncObserver(this, stateLogEventHandlers);
        if (handles.Count > 0)
        {
            foreach (var handle in handles)
            {
                await handle.ResumeAsync(asyncObserver);
            }
        }
        else
        {
            await stream.SubscribeAsync(asyncObserver);
        }
    }
    
    private async Task InitializeOrResumeStateBaseStreamAsync()
    {
        var stream = GetStateBaseStream(this.GetGrainId().ToString());
        var handles = await stream.GetAllSubscriptionHandles();
        var stateHandlers = ServiceProvider.GetRequiredService<IEnumerable<IGAgentStateHandler>>();
        var asyncObserver = new StateBseAsyncObserver(this, stateHandlers);
        if (handles.Count > 0)
        {
            foreach (var handle in handles)
            {
                await handle.ResumeAsync(asyncObserver);
            }
        }
        else
        {
            await stream.SubscribeAsync(asyncObserver);
        }
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    protected virtual Task HandleStateChangedAsync()
    {
        return Task.CompletedTask;
    }

    protected sealed override void OnStateChanged()
    {
        InternalOnStateChangedAsync().ContinueWith(task =>
        {
            if (task.Exception != null)
            {
                Logger.LogError(task.Exception, "InternalOnStateChangedAsync operation failed");
            }
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    private async Task InternalOnStateChangedAsync()
    {
        await HandleStateChangedAsync();
        var stateStream = GetStateBaseStream(this.GetGrainId().ToString());
        await stateStream.OnNextAsync(State);
    }

    protected sealed override void RaiseEvent<T>(T @event)
    {
        Logger.LogDebug("base raiseEvent info:{info}", JsonConvert.SerializeObject(@event));
        base.RaiseEvent(@event);
        InternalRaiseEventAsync(@event).ContinueWith(task =>
        {
            if (task.Exception != null)
            {
                Logger.LogError(task.Exception, "InternalRaiseEventAsync operation failed");
            }
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    private async Task InternalRaiseEventAsync<T>(T @event)
    {
        await HandleRaiseEventAsync();
        if (@event is StateLogEventBase stateLogEvent)
        {
            var stateLogEventStream = GetStateLogEventBaseStream(this.GetGrainId().ToString());
            await stateLogEventStream.OnNextAsync(stateLogEvent);
        }
    }

    protected virtual Task HandleRaiseEventAsync()
    {
        return Task.CompletedTask;
    }

    private IAsyncStream<EventWrapperBase> GetEventWrapperBaseStream(string grainIdString)
    {
        var streamId = StreamId.Create(_aevatarOptions.StreamNamespace, grainIdString);
        return StreamProvider.GetStream<EventWrapperBase>(streamId);
    }

    private IAsyncStream<StateLogEventBase> GetStateLogEventBaseStream(string grainIdString)
    {
        var streamId = StreamId.Create(_aevatarOptions.StreamNamespace + ".StateLogEvent", grainIdString);
        return StreamProvider.GetStream<StateLogEventBase>(streamId);
    }

    private IAsyncStream<StateBase> GetStateBaseStream(string grainIdString)
    {
        var streamId = StreamId.Create(_aevatarOptions.StreamNamespace + ".State", grainIdString);
        return StreamProvider.GetStream<StateBase>(streamId);
    }
}
using System.Reflection;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Aevatar;

public static class GAgentEventHandlerAnalyzer
{
    public static Dictionary<Type, List<Type>> GetPublishedEvents(Type gAgentType)
    {
        var publishedEvents = new Dictionary<Type, List<Type>>();
        var eventHandlerMethods = GetEventHandlerMethods(gAgentType);
        foreach (var eventHandlerMethod in eventHandlerMethods)
        {
            var parameters = eventHandlerMethod.GetParameters();
            var eventHandlerParameter = parameters[0].ParameterType;
            var typeList = GetPublishAsyncArgumentTypes(eventHandlerMethod);
            if (typeList != null)
            {
                publishedEvents[eventHandlerParameter] = typeList;
            }
        }

        return publishedEvents;
    }

    private static List<Type>? GetPublishAsyncArgumentTypes(MethodInfo methodInfo)
    {
        if (methodInfo == null)
            throw new ArgumentNullException(nameof(methodInfo));

        var module = ModuleDefinition.ReadModule(
            methodInfo.Module.FullyQualifiedName,
            new ReaderParameters { ReadSymbols = true }
        );

        var methodDefinitions = module
            .GetType(methodInfo.DeclaringType!.FullName)?
            .Methods;

        var targetMethod = methodDefinitions?.FirstOrDefault(m => m.Name == methodInfo.Name &&
                                                                  m.Parameters.Count == 1);
        return targetMethod == null ? null : AnalyzeIlForPublishAsync(targetMethod);
    }

    private static List<Type> AnalyzeIlForPublishAsync(MethodDefinition methodDef)
    {
        var argumentTypes = new List<Type>();
    
        if (!methodDef.HasBody) 
            return argumentTypes;

        foreach (var instruction in methodDef.Body.Instructions)
        {
            if (instruction.OpCode != OpCodes.Call && 
                instruction.OpCode != OpCodes.Callvirt) 
                continue;

            var methodReference = instruction.Operand as MethodReference;
            if (methodReference == null) 
                continue;

            var isTargetMethod =
                methodReference is { Name: "PublishAsync", HasGenericParameters: false };

            if (methodReference.DeclaringType.FullName.Contains("AsyncStateMachine"))
                continue;

            if (!isTargetMethod) 
                continue;

            var genericMethodRef = methodReference as GenericInstanceMethod;
            if (genericMethodRef != null)
            {
                foreach (var genericArg in genericMethodRef.GenericArguments)
                {
                    argumentTypes.Add(Type.GetType(genericArg.FullName));
                }
            }
            else
            {
                var paramTypes = ResolveMethodParameters(methodDef, instruction);
                argumentTypes.AddRange(paramTypes);
            }
        }

        return argumentTypes;
    }

    private static List<Type> ResolveMethodParameters(
        MethodDefinition callerMethod,
        Instruction callInstruction)
    {
        var paramTypes = new List<Type>();
        var calledMethod = callInstruction.Operand as MethodReference;

        if (calledMethod == null || calledMethod.Parameters.Count == 0)
            return paramTypes;

        // ==== 新核心逻辑 ====
        // 参数分析准确手段：
        // 1. 模拟栈状态：从call指令向前追踪各参数的加载方式
        // 2. 通过IL模式匹配处理 newobj 场景

        var current = callInstruction.Previous;
        for (int i = calledMethod.Parameters.Count - 1; i >= 0; i--)
        {
            // 回退到参数加载起点
            while (current != null)
            {
                // 场景1：参数是newobj创建的实例
                if (current.OpCode == OpCodes.Newobj)
                {
                    MethodReference newObjCtor = current.Operand as MethodReference;
                    paramTypes.Add(ResolveType(newObjCtor.DeclaringType));
                    break; // 找到类型后跳出循环
                }
                // 场景2：参数来自方法参数
                else if (current.OpCode.FlowControl == FlowControl.Next &&
                         current.OpCode.Code.ToString().StartsWith("Ldarg"))
                {
                    int index = GetArgIndex(current);
                    if (index >= 0 && index < callerMethod.Parameters.Count)
                    {
                        paramTypes.Add(ResolveType(callerMethod.Parameters[index].ParameterType));
                    }

                    break;
                }

                // 其他情况处理...
                current = current.Previous;
            }

            // 移动到前一个可能相关的指令
            current = current?.Previous;
        }

        // 结果顺序调整（因逆序填充）
        paramTypes.Reverse();

        return paramTypes;
    }

// 类型解析简写
    private static Type ResolveType(TypeReference typeRef)
    {
        return Type.GetType(typeRef.FullName) ?? typeof(object);
    }

// 获取Ldarg参数的序号
    private static int GetArgIndex(Instruction instruction)
    {
        return instruction.OpCode.Code switch
        {
            Code.Ldarg_0 => 0,
            Code.Ldarg_1 => 1,
            Code.Ldarg_2 => 2,
            Code.Ldarg_3 => 3,
            Code.Ldarg_S => (int)((ParameterDefinition)instruction.Operand).Index,
            _ => -1
        };
    }


    private static IEnumerable<MethodInfo> GetEventHandlerMethods(Type type)
    {
        return type
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(IsEventHandlerMethod);
    }

    private static bool IsEventHandlerMethod(MethodInfo methodInfo)
    {
        return methodInfo.GetParameters().Length == 1 && (
            // Either the method has the EventHandlerAttribute
            // Or is named HandleEventAsync
            //     and the parameter is not EventWrapperBase 
            //     and the parameter is inherited from EventBase
            ((methodInfo.GetCustomAttribute<EventHandlerAttribute>() != null ||
              methodInfo.Name == AevatarGAgentConstants.EventHandlerDefaultMethodName) &&
             methodInfo.GetParameters()[0].ParameterType != typeof(EventWrapperBase) &&
             typeof(EventBase).IsAssignableFrom(methodInfo.GetParameters()[0].ParameterType))
            // Or the method has the AllEventHandlerAttribute and the parameter is EventWrapperBase
            || (methodInfo.GetCustomAttribute<AllEventHandlerAttribute>() != null &&
                methodInfo.GetParameters()[0].ParameterType == typeof(EventWrapperBase))
            // Or the method is for GAgent initialization
            || (methodInfo.Name == AevatarGAgentConstants.ConfigDefaultMethodName &&
                typeof(ConfigurationBase).IsAssignableFrom(methodInfo.GetParameters()[0].ParameterType))
        );
    }
}
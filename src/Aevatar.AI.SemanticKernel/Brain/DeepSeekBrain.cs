using System;
using System.Net.Http;
using System.Threading.Tasks;
using Aevatar.AI.KernelBuilderFactory;
using Aevatar.AI.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextGeneration;

namespace Aevatar.AI.Brain;

public class DeepSeekBrain : BrainBase
{
    private readonly IOptions<DeepSeekConfig> _deepSeekConfig;

    public DeepSeekBrain(
        IOptions<DeepSeekConfig> deepSeekConfig, IKernelBuilderFactory kernelBuilderFactory, ILogger logger,
        IOptions<RagConfig> ragConfig) : base(kernelBuilderFactory, logger, ragConfig)
    {
        _deepSeekConfig = deepSeekConfig;
    }

    protected override Task ConfigureKernelBuilder(IKernelBuilder kernelBuilder)
    {
        OpenAIChatCompletionService GetOpenAiChatCompletion()
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(_deepSeekConfig.Value.Endpoint);
            return new OpenAIChatCompletionService(_deepSeekConfig.Value.ModelId, _deepSeekConfig.Value.ApiKey, httpClient: httpClient);
        }

        kernelBuilder.Services.AddKeyedSingleton<IChatCompletionService>(DeepSeekConfig.ConfigSectionName,
            (sp, key) => GetOpenAiChatCompletion());
        
        kernelBuilder.Services.AddKeyedSingleton<ITextGenerationService>(DeepSeekConfig.ConfigSectionName,
            (sp, key) => GetOpenAiChatCompletion());

        return Task.CompletedTask;
    }
}
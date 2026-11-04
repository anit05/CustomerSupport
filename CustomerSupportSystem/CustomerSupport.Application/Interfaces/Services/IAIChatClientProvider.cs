using Microsoft.Extensions.AI;
namespace CustomerSupport.Application.Interfaces.Services
{
  // Supplies the configured AI provider without exposing its SDK.
  public interface IAIChatClientProvider
  {
    string ProviderName { get; }
    IChatClient GetClient();
  }
}

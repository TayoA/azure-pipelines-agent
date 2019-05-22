using System;
using System.Linq;
using Microsoft.VisualStudio.Services.Agent.Util;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
#if OS_WINDOWS
    [ServiceLocator(Default = typeof(WindowsServiceControlManager))]
    public interface IWindowsServiceControlManager : IAgentService
    {
        void ConfigureService(AgentSettings settings, CommandSettings command);

        void UnconfigureService();
    }
#endif

#if !OS_WINDOWS

#if OS_LINUX
    [ServiceLocator(Default = typeof(SystemDControlManager))]
#elif OS_OSX
    [ServiceLocator(Default = typeof(OsxServiceControlManager))]
#endif
    public interface ILinuxServiceControlManager : IAgentService
    {
        void GenerateScripts(AgentSettings settings);
    }
#endif

    public class ServiceControlManager : AgentService
    {
        public void CalculateServiceName(AgentSettings settings, string serviceNamePattern, string serviceDisplayNamePattern, out string serviceName, out string serviceDisplayName)
        {
            Trace.Entering();
            serviceName = string.Empty;
            serviceDisplayName = string.Empty;

            Uri accountUri = new Uri(settings.ServerUrl);
            string accountName = string.Empty;

            if (accountUri.Host.Equals("dev.azure.com", StringComparison.OrdinalIgnoreCase))
            {
                accountName = accountUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            }
            else
            {
                accountName = accountUri.Host.Split('.').FirstOrDefault();
            }

            if (string.IsNullOrEmpty(accountName))
            {
                throw new InvalidOperationException(StringUtil.Loc("CannotFindHostName", settings.ServerUrl));
            }

            serviceName = StringUtil.Format(serviceNamePattern, accountName, settings.PoolName, settings.AgentName);

            if (serviceName.Length > 80)
            {
                Trace.Verbose($"Calculated service name is too long (> 80 chars). Trying again by calculating a shorter name.");

                string accountNameSubstring = StringUtil.SubstringPrefix(accountName, 30);
                string poolNameSubstring = StringUtil.SubstringPrefix(settings.PoolName, 30);
                string agentNameSubstring = StringUtil.SubstringPrefix(settings.AgentName, 20);

                serviceName = StringUtil.Format(serviceNamePattern, accountNameSubstring, poolNameSubstring, agentNameSubstring);
            }

            serviceDisplayName = StringUtil.Format(serviceDisplayNamePattern, accountName, settings.PoolName, settings.AgentName);

            Trace.Info($"Service name '{serviceName}' display name '{serviceDisplayName}' will be used for service configuration.");
        }
    }
}
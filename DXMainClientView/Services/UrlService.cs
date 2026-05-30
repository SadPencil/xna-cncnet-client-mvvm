using System;
using System.Linq;

using ClientCore;

using Rampastring.Tools;

using DXMainClientMVVMContract.ViewServices;

namespace DXMainClientView.Services
{
    /// <summary>
    /// Implementation of IUrlService that handles URL trust checking and opening.
    /// Migrated from URLHandler.cs.
    /// </summary>
    public class UrlService : IUrlService
    {
        public bool IsTrustedUrl(string url)
        {
            try
            {
                string domain = new Uri(url).Host;
                var trustedDomains = ClientConfiguration.Instance.TrustedDomains
                    .Concat(ClientConfiguration.Instance.AlwaysTrustedDomains);
                return trustedDomains.Contains(domain, StringComparer.InvariantCultureIgnoreCase)
                    || trustedDomains.Any(trustedDomain =>
                        domain.EndsWith("." + trustedDomain, StringComparison.InvariantCultureIgnoreCase));
            }
            catch (Exception ex)
            {
                Logger.Log($"Error in parsing the URL \"{url}\": {ex}");
                return false;
            }
        }

        public void OpenUrl(string url)
        {
            ProcessLauncher.StartShellProcess(url);
        }
    }
}

namespace AvClientMvvmContract.ViewServices
{
    /// <summary>
    /// Service for handling URL opening with trust verification.
    /// Abstracts URLHandler for ViewModel use.
    /// </summary>
    public interface IUrlService
    {
        /// <summary>
        /// Checks whether a URL's domain is in the trusted domains list.
        /// </summary>
        bool IsTrustedUrl(string url);

        /// <summary>
        /// Opens a URL in the default browser.
        /// </summary>
        void OpenUrl(string url);
    }
}

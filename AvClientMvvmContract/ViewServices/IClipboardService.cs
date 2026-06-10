namespace AvClientMvvmContract.ViewServices
{
    /// <summary>
    /// Service for clipboard operations. Abstracts Avalonia Clipboard for ViewModel use.
    /// </summary>
    public interface IClipboardService
    {
        /// <summary>
        /// Copies text to the system clipboard asynchronously.
        /// </summary>
        void SetTextAsync(string text);
    }
}

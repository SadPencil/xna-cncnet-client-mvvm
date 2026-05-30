using System;

namespace DXMainClientMVVMContract.ViewServices
{
    /// <summary>
    /// Provides UI thread marshaling functionality.
    /// Replaces WindowManager.AddCallback for ViewModel use.
    /// </summary>
    public interface IUIThreadMarshaller
    {
        /// <summary>
        /// Executes a delegate on the UI thread.
        /// </summary>
        /// <param name="d">The delegate to execute.</param>
        /// <param name="args">Arguments for the delegate.</param>
        void AddCallback(Delegate d, params object[] args);
    }
}

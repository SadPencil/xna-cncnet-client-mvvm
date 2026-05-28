using System;

namespace DXMainClientViewModel
{
    /// <summary>
    /// Service interface for game process lifecycle events.
    /// Abstracts GameProcessLogic from ClientGUI.
    /// </summary>
    public interface IGameProcessService
    {
        /// <summary>
        /// Raised when the game process has started.
        /// </summary>
        event Action GameProcessStarted;

        /// <summary>
        /// Raised when the game process is about to start.
        /// </summary>
        event Action GameProcessStarting;

        /// <summary>
        /// Raised when the game process has exited.
        /// </summary>
        event Action GameProcessExited;

        /// <summary>
        /// Whether to use QRes for windowed mode.
        /// </summary>
        bool UseQres { get; set; }

        /// <summary>
        /// Whether to set single core affinity on the game process.
        /// </summary>
        bool SingleCoreAffinity { get; set; }
    }
}

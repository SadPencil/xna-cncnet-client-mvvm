using AvClientMvvmContract.ViewServices;

namespace AvClientView.Services
{
    /// <summary>
    /// Avalonia implementation of IClientSoundService.
    /// Provides client UI sound effects volume control.
    /// </summary>
    public class ClientSoundService : IClientSoundService
    {
        private float currentVolume;

        public void SetVolume(float volume)
        {
            currentVolume = volume;
            // TODO: Implement with Avalonia audio framework when available
        }
    }
}

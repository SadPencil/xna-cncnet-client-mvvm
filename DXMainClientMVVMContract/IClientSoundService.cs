namespace DXMainClientMVVMContract
{
    /// <summary>
    /// Service interface for client UI sound effects volume.
    /// Abstracts WindowManager.SoundPlayer.SetVolume().
    /// </summary>
    public interface IClientSoundService
    {
        /// <summary>
        /// Sets the client sound effects volume.
        /// </summary>
        /// <param name="volume">Volume level from 0.0 to 1.0.</param>
        void SetVolume(float volume);
    }
}

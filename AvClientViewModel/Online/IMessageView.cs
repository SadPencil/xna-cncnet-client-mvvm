using AvClientMvvmContract.Online;
namespace AvClientViewModel.Online
{
    public interface IMessageView
    {
        void AddMessage(IChatMessage message);
    }
}

using AvMainClientMvvmContract.Online;
namespace AvMainClientViewModel.Online
{
    public interface IMessageView
    {
        void AddMessage(IChatMessage message);
    }
}

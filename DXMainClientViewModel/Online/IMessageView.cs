using DXMainClientMvvmContract.Online;
namespace DXMainClientViewModel.Online
{
    public interface IMessageView
    {
        void AddMessage(IChatMessage message);
    }
}

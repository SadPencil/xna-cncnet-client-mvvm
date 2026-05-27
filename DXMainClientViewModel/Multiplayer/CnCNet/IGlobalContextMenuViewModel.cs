#nullable enable
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Multiplayer.CnCNet;

public interface IGlobalContextMenuViewModel : INotifyPropertyChanged
{
    string TargetUserName { get; }
    bool CanInvitePlayer { get; }
    bool CanJoinPlayer { get; }
    bool CanOpenPrivateMessage { get; }
    bool CanAddFriend { get; }
    bool IsBlocked { get; }

    IRelayCommand OpenPrivateMessageCommand { get; }
    IRelayCommand InvitePlayerCommand { get; }
    IRelayCommand JoinPlayerCommand { get; }
    IRelayCommand AddFriendCommand { get; }
    IRelayCommand RemoveFriendCommand { get; }
    IRelayCommand BlockUserCommand { get; }
    IRelayCommand UnblockUserCommand { get; }
    IRelayCommand CopyNameCommand { get; }
}

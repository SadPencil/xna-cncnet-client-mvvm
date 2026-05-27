#nullable enable
using System.Collections.Generic;

namespace DXMainClientViewModel;

public interface IUserCollectionService
{
    IReadOnlyList<string> UserNames { get; }

    bool ContainsUser(string userName);
    void AddUser(string userName);
    void RemoveUser(string userName);
    void RenameUser(string oldUserName, string newUserName);
    void ClearUsers();
}

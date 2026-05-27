#nullable enable

using System.Collections.Generic;

namespace DXMainClientView;

public interface ISkirmishLobbyView : IGameLobbyView
{
    void SetAiDifficultyOptions(int playerIndex, IEnumerable<string> options);
    void SetAiPlayerEnabled(int playerIndex, bool enabled);
    void SetRandomSeedText(string seedText);
    void SetSeedVisible(bool visible);
    void ShowQuickMatchHint(string hintText);
}

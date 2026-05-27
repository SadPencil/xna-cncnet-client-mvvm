#nullable enable
using System.Collections.Generic;

namespace DXMainClientViewModel.Online;

public interface IChatColorService
{
    IReadOnlyList<string> ColorNames { get; }

    string GetColorCode(string colorName);
    string ApplyColor(string text, string colorName);
    string StripColors(string text);
}

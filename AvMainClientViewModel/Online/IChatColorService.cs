using System.Collections.Generic;

namespace AvMainClientViewModel.Online;

public interface IChatColorService
{
    IReadOnlyList<string> ColorNames { get; }

    string GetColorCode(string colorName);
    string ApplyColor(string text, string colorName);
    string StripColors(string text);
}

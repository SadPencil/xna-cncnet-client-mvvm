using System;
using Avalonia.Markup.Xaml;
using ClientCore.I18N;

namespace AvClientView.MarkupExtensions;

/// <summary>
/// Avalonia markup extension that resolves translated strings at runtime via
/// <see cref="Translation.Instance"/>. The default (English) text must be a literal;
/// the key identifies the string in translation files.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Translation.LookUp(string, string, bool)"/> defaults <c>notify=true</c>,
/// which registers every key in <c>MissingKeys</c> and <c>Values</c> the first time
/// it is seen. Avalonia eagerly instantiates all controls in the visual tree (even
/// those with <c>IsVisible="False"</c>), so <c>ProvideValue</c> runs for every
/// <c>{l:Translate}</c> in every view at startup. Together this means every View
/// translation key is automatically registered for stub generation.
/// </para>
/// </remarks>
/// <example>
/// <c>Text="{l:Translate 'Launch', 'Client:UI:ButtonLaunch'}"</c>
/// </example>
public sealed class TranslateExtension : MarkupExtension
{
    private readonly string _defaultValue;
    private readonly string _key;

    public TranslateExtension(string defaultValue, string key)
    {
        _defaultValue = defaultValue;
        _key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return Translation.Instance.LookUp(_key, _defaultValue);
    }
}

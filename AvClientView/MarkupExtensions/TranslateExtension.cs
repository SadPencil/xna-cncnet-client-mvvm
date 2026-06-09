using System;
using Avalonia.Markup.Xaml;
using ClientCore.I18N;

namespace AvClientView.MarkupExtensions;

/// <summary>
/// Avalonia markup extension that resolves translated strings at runtime via
/// <see cref="Translation.Instance"/>. The default (English) text must be literal;
/// the key identifies the string in translation files.
/// </summary>
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
        // Translation.Instance is always non-null (initialized as a default "en" instance).
        // LookUp returns the translated value if found, otherwise the defaultValue.
        return Translation.Instance.LookUp(_key, _defaultValue);
    }
}

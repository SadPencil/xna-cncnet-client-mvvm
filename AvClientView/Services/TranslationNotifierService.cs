using AvClientMvvmContract.ViewServices;

namespace AvClientView.Services;

/// <summary>
/// Registers compile-time L10N strings from the View project with the
/// translation system so they are included in translation stub generation.
/// </summary>
public sealed class TranslationNotifierService : ITranslationNotifierService
{
    public void Register()
    {
        AvClientView.Generated.TranslationNotifier.Register();
    }
}

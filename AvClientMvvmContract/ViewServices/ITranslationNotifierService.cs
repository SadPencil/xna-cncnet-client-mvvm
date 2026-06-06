namespace AvClientMvvmContract.ViewServices;

/// <summary>
/// Service for registering compile-time translation strings from the View project.
/// The Exe composition root creates the implementation and passes it to the ViewModel,
/// because the ViewModel cannot reference the View project directly.
/// </summary>
public interface ITranslationNotifierService
{
    /// <summary>
    /// Registers all compile-time translation strings from the View project
    /// with the translation system for stub generation.
    /// </summary>
    void Register();
}

namespace AvMainClientMvvmContract.Campaign;

/// <summary>
/// Abstracts a campaign checkbox option's business logic.
/// The View creates the actual UI control from INI and registers an implementation here.
/// TODO: this violates MVVM. Re-think the pattern here. The view model must be able to read the ini and create the options itself without the view's help.
/// </summary>
public interface ICampaignCheckBoxOption
{
    string Name { get; }
    bool Checked { get; set; }
}

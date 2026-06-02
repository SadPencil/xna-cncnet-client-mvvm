using System.Collections.Generic;
using System.ComponentModel;

namespace AvMainClientMvvmContract.Domain;

/// <summary>
/// Read-only view of a mission definition.
/// </summary>
public interface IMission : INotifyPropertyChanged
{
    string CodeName { get; }
    string GUIName { get; }
    string UntranslatedGUIName { get; }
    string IconPath { get; }
    string GUIDescription { get; }
    bool Enabled { get; }
    string PreviewImage { get; }
}

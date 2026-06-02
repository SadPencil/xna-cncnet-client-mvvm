using System;

namespace AvClientViewModel.Domain.Multiplayer.CnCNet;

public class PasswordEventArgs : EventArgs
{
    public PasswordEventArgs(string password, HostedCnCNetGame hostedGame)
    {
        Password = password;
        HostedGame = hostedGame;
    }

    public string Password { get; }

    public HostedCnCNetGame HostedGame { get; }
}

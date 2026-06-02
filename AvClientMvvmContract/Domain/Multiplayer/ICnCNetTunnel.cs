namespace AvClientMvvmContract.Domain.Multiplayer;

/// <summary>
/// Read-only view of a CnCNet tunnel server.
/// </summary>
public interface ICnCNetTunnel
{
    string Address { get; }
    int Port { get; }
    string Country { get; }
    string CountryCode { get; }
    string Name { get; }
    bool RequiresPassword { get; }
    int Clients { get; }
    int MaxClients { get; }
    bool Official { get; }
    bool Recommended { get; }
    double Latitude { get; }
    double Longitude { get; }
    int Version { get; }
    double Distance { get; }
    int PingInMs { get; }
}

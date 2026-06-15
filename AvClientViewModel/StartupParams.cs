using System;
using System.Collections.Generic;

namespace AvClientViewModel;

/// <summary>
/// Contains client startup parameters.
/// </summary>
public class StartupParams
{
    public StartupParams(bool noAudio, bool multipleInstanceMode,
        List<string> unknownParams)
    {
        NoAudio = noAudio;
        MultipleInstanceMode = multipleInstanceMode;
        UnknownStartupParams = unknownParams;
    }

    public StartupParams(string[] args)
    {
        var unknownParams = new List<string>();

        for (int arg = 0; arg < args.Length; arg++)
        {
            string argument = args[arg].ToUpperInvariant();

            switch (argument)
            {
                case "-NOAUDIO":
                case "--NOAUDIO":
                    NoAudio = true;
                    break;
                case "-MULTIPLEINSTANCE":
                case "--MULTIPLEINSTANCES":
                case "--MULTIPLE-INSTANCES":
                    MultipleInstanceMode = true;
                    break;
                default:
                    unknownParams.Add(args[arg]);
                    break;
            }
        }

        UnknownStartupParams = unknownParams;
    }

    public bool NoAudio { get; }
    public bool MultipleInstanceMode { get; }
    public List<string> UnknownStartupParams { get; }
}

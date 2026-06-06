using System;
using System.Collections.Generic;
using System.Linq;

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
    }

    public StartupParams(string[] args)
    {
        NoAudio = args.Contains("--noaudio", StringComparer.InvariantCultureIgnoreCase);
        MultipleInstanceMode = args.Contains("--multipleinstances", StringComparer.InvariantCultureIgnoreCase)
            || args.Contains("--multiple-instances", StringComparer.InvariantCultureIgnoreCase);
    }

    public bool NoAudio { get; }
    public bool MultipleInstanceMode { get; }
}

using System;

namespace DTAClient.Online.EventArguments
{
    public class GameOptionPresetEventArgs : EventArgs // checked
    {
        public string PresetName { get; }

        public GameOptionPresetEventArgs(string presetName)
        {
            PresetName = presetName;
        }
    }
}

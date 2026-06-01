using ClientCore;
using ClientCore.Extensions;

using DXMainClientMvvmContract;

using DXMainClientViewModel.Online;

using Rampastring.Tools;

using System;
using System.Collections.Generic;

namespace DXMainClientViewModel.Domain.Multiplayer
{
    /// <summary>
    /// A color for the multiplayer game lobby.
    /// </summary>
    public class MultiplayerColor
    {
        public int GameColorIndex { get; private set; }
        public string Name { get; private set; }
        public IRgb24Color Color { get; private set; }

        private static List<MultiplayerColor> colorList;

        /// <summary>
        /// Creates a new multiplayer color from data in a string array.
        /// </summary>
        /// <param name="name">The name of the color.</param>
        /// <param name="data">The input data. Needs to be in the format R,G,B,(game color index).</param>
        /// <returns>A new multiplayer color created from the given string array.</returns>
        public static MultiplayerColor CreateFromStringArray(string name, string[] data)
        {
            return new MultiplayerColor()
            {
                Name = name,
                Color = new Rgb24Color(byte.Parse(data[0]), byte.Parse(data[1]), byte.Parse(data[2])),
                GameColorIndex = Int32.Parse(data[3])
            };
        }

        /// <summary>
        /// Returns the available multiplayer colors.
        /// </summary>
        public static List<MultiplayerColor> LoadColors()
        {
            if (colorList != null)
                return new List<MultiplayerColor>(colorList);

            IniFile gameOptionsIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), "GameOptions.ini"));

            List<MultiplayerColor> mpColors = new List<MultiplayerColor>();

            List<string> colorKeys = gameOptionsIni.GetSectionKeys("MPColors");

            if (colorKeys == null)
                throw new ClientConfigurationException("[MPColors] not found in GameOptions.ini!");

            foreach (string key in colorKeys)
            {
                string[] values = gameOptionsIni.GetStringListValue("MPColors", key, "255,255,255,0");

                try
                {
                    MultiplayerColor mpColor = MultiplayerColor.CreateFromStringArray(key.L10N($"INI:Colors:{key}"), values);
                    mpColors.Add(mpColor);
                }
                catch
                {
                    throw new ClientConfigurationException("Invalid MPColor specified in GameOptions.ini: " + key);
                }
            }

            colorList = mpColors;
            return new List<MultiplayerColor>(colorList);
        }
    }
}

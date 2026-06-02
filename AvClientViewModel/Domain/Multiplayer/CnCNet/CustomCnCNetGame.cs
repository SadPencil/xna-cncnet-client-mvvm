#nullable enable
using System;
using System.Reflection;

using SixLabors.ImageSharp;

namespace AvClientViewModel.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// A <see cref="CnCNetGame"/> that loads its texture from a custom icon file, or falls back
    /// to the unknown game icon embedded in the assembly.
    /// </summary>
    internal sealed class CustomCnCNetGame : CnCNetGame
    {
        //private static readonly Lazy<Image> lazyFallbackImage = new(() =>
        //    Image.Load(
        //        Assembly.GetAssembly(typeof(CustomCnCNetGame))!
        //        .GetManifestResourceStream("DTAClient.Icons.unknownicon.png")));
        //private static Image FallbackImage => lazyFallbackImage.Value;
        private static Image? FallbackImage => null;

        public CustomCnCNetGame(string iconFilename)
        {
        }

        protected override Image? LoadImage() => FallbackImage;
    }
}

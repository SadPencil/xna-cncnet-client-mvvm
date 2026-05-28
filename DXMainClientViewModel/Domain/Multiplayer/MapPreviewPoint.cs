namespace DXMainClientViewModel.Domain.Multiplayer
{
    /// <summary>
    /// A simple point struct to replace Microsoft.Xna.Framework.Point.
    /// </summary>
    public struct MapPreviewPoint
    {
        public int X { get; set; }
        public int Y { get; set; }

        public MapPreviewPoint(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}

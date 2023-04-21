using System.Drawing;

namespace terrain
{
    public sealed class MapTile
    {
        public ushort Tile = ushort.MaxValue;
        public ushort Object = ushort.MaxValue;
        public TileRegion Region = TileRegion.FM_Empty;

        public Color RenderColor;
    }
}

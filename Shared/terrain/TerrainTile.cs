using System;

namespace common.terrain
{
    public enum Tile : ushort
    {
    }
    
    public enum TerrainType
    {
        None,
        Mountains,
        HighSand,
        HighPlains,
        HighForest,
        MidSand,
        MidPlains,
        MidForest,
        LowSand,
        LowPlains,
        LowForest,
        ShoreSand,
        ShorePlains,
    }

    public enum TileRegion : byte
    {
        None,
        Spawn,
        Realm_Portals,
        Vault,
        Gifting_Chest,
        Store_1,
        Store_2,
        Store_3,
        Store_4,
        Store_5,
        Store_6,
        Store_7,
        Store_8,
        Store_9,
        Store_10,
        Store_11,
        Store_12,
        Store_13,
        Store_14,
        Store_15,
        Store_16,
        Store_17,
        Store_18,
        Store_19,
        Store_20,
        PetRegion,
        Safezone,
        Biome_Valley,
        Biome_Hallowed,
        Biome_Fungal_Forest,
        Biome_Desert,
        Biome_Elvish_Wood,
        Biome_Snowy_Peaks,
        Biome_Scorch,
        Biome_Forest,
        Biome_Rainforest,
        Biome_Coastal_Forest,
        Biome_Swamp,
        Biome_Ocean,
        Biome_Ocean_Unwalk,
        Biome_Deep_Forest
    }

    public struct TerrainTile : IEquatable<TerrainTile>
    {
        public int PolygonId;
        public byte Elevation;
        public float Moisture;
        public string Biome;
        public ushort TileId;
        public string Name;
        public string TileObj;
        public TerrainType Terrain;
        public TileRegion Region;

        public bool Equals(TerrainTile other)
        {
            return
                this.TileId == other.TileId &&
                this.TileObj == other.TileObj &&
                this.Name == other.Name &&
                this.Terrain == other.Terrain &&
                this.Region == other.Region;
        }
    }
}
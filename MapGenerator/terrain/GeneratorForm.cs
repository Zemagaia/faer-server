using Ionic.Zlib;
using Microsoft.Win32;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace terrain
{
    public partial class GeneratorForm : Form
    {
        public int MAP_SIZE => (int)numericUpDown4.Value;

        public GeneratorForm()
        {
            InitializeComponent();

            pictureBox2.Parent = pictureBox1;
            pictureBox3.Parent = pictureBox2;
        }

        public bool ShowGrid => checkBox1.Checked;
        public bool ShowOcean => checkBox2.Checked;
        public bool RandomizeEdges => checkBox3.Checked;


        private void button1_Click(object sender, EventArgs e)
        {
            Generate();
        }

        public volatile bool Generating;

        public volatile PolygonMap CurrentMap;
        public volatile MapTile[,] MapData;

        public void Generate()
        {
            if (Generating)
                return;
            Generating = true;

            _ = Task.Factory.StartNew(() =>
            {
                var seed = textBox1.Text.GetHashCode();
                var biomeSeed = textBox2.Text.GetHashCode();

                var map = new PolygonMap(seed, biomeSeed);
                map.Generate(/*(int)numericUpDown1.Value*/ 32768 * 3, (int)numericUpDown2.Value, (int)numericUpDown3.Value);
                CurrentMap = map;

                ////var dat = CreateTerrain(map);
                //new Biome(map).ComputeBiomes(dat);

                RenderMap();

            }).ContinueWith(_ =>
            {
                Generating = false;
            });
        }

        public void RenderMap()
        {
            if(CurrentMap == null) 
                return;

            var (bitmap, bitmap2, mapData) = RenderPolygons(CurrentMap);
            pictureBox1.Image = bitmap;
            pictureBox2.Image = bitmap2;
            MapData = mapData;
        }

        (Bitmap, Bitmap, MapTile[,]) RenderPolygons(PolygonMap map)
        {
            var rasterizer = new Rasterizer<MapTile>(MAP_SIZE, MAP_SIZE);
            var overlayRasterizer = new Rasterizer<int>(MAP_SIZE, MAP_SIZE);

            rasterizer.Clear(new MapTile());
            overlayRasterizer.Clear(Color.Transparent.ToArgb());

            var pointThickness = MAP_SIZE < 512 ? 1 : MAP_SIZE < 1024 ? 2 : 3;

            var beaches = new HashSet<MapPolygon>(map.MapPolygons.Where(_ => !_.IsWater && _.Neighbours.Any(__ => __.IsCoast)));

            //Render lands poly
            foreach (var poly in map.MapPolygons)
            {
                if (poly.IsWater)
                    continue;

                var points = new List<double>();
                foreach (var polyNode in poly.Nodes)
                {
                    points.Add((polyNode.X + 1) / 2 * MAP_SIZE);
                    points.Add((polyNode.Y + 1) / 2 * MAP_SIZE);
                }
                points.Add((poly.Nodes[0].X + 1) / 2 * MAP_SIZE);
                points.Add((poly.Nodes[0].Y + 1) / 2 * MAP_SIZE);

                var tile = new MapTile();
                var color = Color.Magenta;
                var type = GetTileType("Water Dark");
                var region = TileRegion.FM_Empty;
                var r = map.Random.NextDouble();
                switch (poly.Biome)
                {
                    case "Volcanic":
                        color = Color.FromArgb(69, 78, 99);
                        type = GetTileType("Cobblestone");
                        region = r > 0.5 ? TileRegion.Biome_Volcanic_Encounter_Spawn : TileRegion.Biome_Volcanic_Setpiece_Spawn;
                        break;
                    case "Forest":
                        color = Color.FromArgb(65, 109, 93);
                        type = GetTileType("Grass"); 
                        region = r > 0.5 ? TileRegion.Biome_Forest_Encounter_Spawn : TileRegion.Biome_Forest_Setpiece_Spawn;
                        break;
                    case "Desert":
                        color = Color.FromArgb(226, 178, 126);
                        type = GetTileType("Desert Sand");
                        region = r > 0.5 ? TileRegion.Biome_Desert_Encounter_Spawn : TileRegion.Biome_Desert_Setpiece_Spawn;
                        break;
                }

                // remove some regions
                if(region != TileRegion.FM_Empty)
                    if (map.Random.NextDouble() > 0.5)
                        region = TileRegion.FM_Empty;

                tile.Tile = type;
                //tile.Region = region;
                tile.RenderColor = color;

                rasterizer.FillPolygon(points.ToArray(), tile);

                var cx = (int)((poly.CentroidX + 1) / 2 * MAP_SIZE);
                var cy = (int)((poly.CentroidY + 1) / 2 * MAP_SIZE);

                if (ShowGrid)
                {
                    overlayRasterizer.DrawPolygon(points.ToArray(), Color.Black.ToArgb(), pointThickness);
                    overlayRasterizer.PlotSqr(cx, cy, Color.Black.ToArgb(), pointThickness);
                }

                if (region == TileRegion.FM_Empty)
                    continue;

                var regionColor = Color.Transparent;
                if (region >= TileRegion.Biome_Desert_Encounter_Spawn && region <= TileRegion.Biome_Forest_Encounter_Spawn)
                    regionColor = Color.FromArgb(155, 0, 255);
                else if (region >= TileRegion.Biome_Desert_Setpiece_Spawn && region <= TileRegion.Biome_Forest_Setpiece_Spawn)
                    regionColor = Color.FromArgb(0, 255, 173);
                else if (region == TileRegion.Spawn)
                    regionColor = Color.Green;

                if (regionColor != Color.Transparent)
                {
                    tile = new MapTile();
                    tile.Tile = type;
                    tile.Region = region;
                    rasterizer.Plot(cx, cy, tile);
                    overlayRasterizer.PlotSqr(cx, cy, regionColor.ToArgb(), pointThickness);
                }
            }

            foreach (var poly in map.MapPolygons)
            {
                if (!poly.IsWater || !ShowOcean)
                    continue;

                var points = new List<double>();
                foreach (var polyNode in poly.Nodes)
                {
                    points.Add((polyNode.X + 1) / 2 * MAP_SIZE);
                    points.Add((polyNode.Y + 1) / 2 * MAP_SIZE);
                }
                points.Add((poly.Nodes[0].X + 1) / 2 * MAP_SIZE);
                points.Add((poly.Nodes[0].Y + 1) / 2 * MAP_SIZE);

                var tile = new MapTile();

                var color = Color.FromArgb(20, 160, 184);
                var type = GetTileType("Water");
                if (poly.IsOcean && !poly.IsCoast || poly.Neighbours.All(_ => _.IsWater))
                {
                    color = Color.FromArgb(14, 122, 140);
                    type = GetTileType("Water Dark");
                }
                tile.RenderColor = color;
                tile.Tile = type;

                rasterizer.FillPolygon(points.ToArray(), tile);

                var cx = (int)((poly.CentroidX + 1) / 2 * MAP_SIZE);
                var cy = (int)((poly.CentroidY + 1) / 2 * MAP_SIZE);

                if (ShowGrid)
                {
                    overlayRasterizer.DrawPolygon(points.ToArray(), Color.White.ToArgb(), pointThickness);
                    overlayRasterizer.PlotSqr(cx, cy, Color.Black.ToArgb(), pointThickness);
                }
            }

            foreach (var poly in beaches)
            {
                var points = new List<double>();
                foreach (var polyNode in poly.Nodes)
                {
                    points.Add((polyNode.X + 1) / 2 * MAP_SIZE);
                    points.Add((polyNode.Y + 1) / 2 * MAP_SIZE);
                }
                points.Add((poly.Nodes[0].X + 1) / 2 * MAP_SIZE);
                points.Add((poly.Nodes[0].Y + 1) / 2 * MAP_SIZE);

                var tile = new MapTile();

                var type = GetTileType("Dirt");
                tile.RenderColor = Color.FromArgb(86, 72, 70);
                tile.Tile = type;

                rasterizer.FillPolygon(points.ToArray(), tile);

                if (ShowGrid)
                {
                    overlayRasterizer.DrawPolygon(points.ToArray(), Color.White.ToArgb(), pointThickness);

                    var cx = (int)((poly.CentroidX + 1) / 2 * MAP_SIZE);
                    var cy = (int)((poly.CentroidY + 1) / 2 * MAP_SIZE);
                    overlayRasterizer.PlotSqr(cx, cy, Color.Black.ToArgb(), pointThickness);
                }
            }


            foreach (var poly in beaches)
            {
                var points = new List<double>();
                foreach (var polyNode in poly.Nodes)
                {
                    points.Add((polyNode.X + 1) / 2 * MAP_SIZE);
                    points.Add((polyNode.Y + 1) / 2 * MAP_SIZE);
                }
                points.Add((poly.Nodes[0].X + 1) / 2 * MAP_SIZE);
                points.Add((poly.Nodes[0].Y + 1) / 2 * MAP_SIZE);

                var tile = new MapTile();

                if (!map.MapPolygons.Any(_ => poly.Neighbours.Contains(_)))
                    continue;

                var color = Color.Magenta;
                var type = GetTileType("Water Dark");
                switch (poly.Biome)
                {
                    case "Volcanic":
                        color = Color.FromArgb(190, 155, 115);
                        type = GetTileType("Wood Light");
                        break;
                    case "Forest":
                        color = Color.FromArgb(86, 72, 70);
                        type = GetTileType("Dirt");
                        break;
                    case "Desert":
                        color = Color.FromArgb(160, 128, 90);
                        type = GetTileType("Darket Desert Sand");
                        break;
                }
                tile.Tile = type;
                tile.RenderColor = color;

                rasterizer.FillPolygon(points.ToArray(), tile);

                if (ShowGrid)
                {
                    overlayRasterizer.DrawPolygon(points.ToArray(), Color.White.ToArgb(), pointThickness);

                    var cx = (int)((poly.CentroidX + 1) / 2 * MAP_SIZE);
                    var cy = (int)((poly.CentroidY + 1) / 2 * MAP_SIZE);
                    overlayRasterizer.PlotSqr(cx, cy, Color.Black.ToArgb(), pointThickness);
                }
            }

            if (RandomizeEdges)
            {
                Randomize(map, rasterizer.Buffer);
            }

            var bmp = new Bitmap(MAP_SIZE, MAP_SIZE);
            var buff = new BitmapBuffer(bmp);
            buff.Lock();

            for (int y = 0; y < MAP_SIZE; y++)
                for (int x = 0; x < MAP_SIZE; x++)
                    buff[x, y] = (uint)rasterizer.Buffer[x, y].RenderColor.ToArgb();
            buff.Unlock();

            var bmp2 = new Bitmap(MAP_SIZE, MAP_SIZE);
            var buff2 = new BitmapBuffer(bmp2);
            buff2.Lock();

            for (int y = 0; y < MAP_SIZE; y++)
                for (int x = 0; x < MAP_SIZE; x++)
                    buff2[x, y] = (uint)overlayRasterizer.Buffer[x, y];
            buff2.Unlock();
            return (bmp, bmp2, rasterizer.Buffer);
        }

        public static ushort GetTileType(string tile)
        {
            ushort type = 0x31;
            switch (tile)
            {
                case "Water":
                    type = 0x19;
                    break;
                case "Water Dark":
                    type = 0x20;
                    break;
                case "Cobblestone":
                    type = 0x25;
                    break;
                case "Desert Sand":
                    type = 0x2b;
                    break;
                case "Grass":
                    type = 0x21;
                    break;
                case "Dirt":
                    type = 0x22;
                    break;
            }
            return type;
        }

        void Randomize(PolygonMap map, string[,] buff)
        {
            for (int x = 8; x < MAP_SIZE - 8; x++)
                for (int y = 8; y < MAP_SIZE - 8; y++)
                {
                    if (buff[x, y] == "Water Dark" || buff[x, y] == "Water")
                        continue;
                    var t = buff[x + map.Random.Next(-2, 3), y + map.Random.Next(-2, 3)];
                    if (t == "Water Dark" || t == "Water")
                        continue;
                    buff[x, y] = t;
                }
        }

        void Randomize(PolygonMap map, MapTile[,] buff)
        {
            var ocean = GetTileType("Water");
            var water = GetTileType("Water Dark");
            for (var x = 8; x < MAP_SIZE - 8; x++)
                for (var y = 8; y < MAP_SIZE - 8; y++)
                {
                    var tile = buff[x, y];
                    var tileType = tile.Tile;

                    if (buff[x, y].Region != TileRegion.FM_Empty)
                        continue;

                    if (tileType == ocean || tileType == water)
                        continue;

                    var px = x + map.Random.Next(-2, 3);
                    var py = y + map.Random.Next(-2, 3);

                    var swapTile = buff[px, py];
                    tileType = swapTile.Tile;
                    if (tileType == ocean || tileType == water)
                        continue;

                    buff[x, y] = swapTile;
                    buff[px, py] = tile;
                }
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            Generate();
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            Generate();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Generate();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            RenderMap();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            RenderMap();
        }


        private void pictureBox3_MouseMove(object sender, MouseEventArgs e)
        {
            return;

            if (CurrentMap == null || pictureBox1.Image == null)
                return;

            float panelX = ((float)e.X / pictureBox3.ClientSize.Width) * MAP_SIZE;
            float panelY = ((float)e.Y / pictureBox3.ClientSize.Height) * MAP_SIZE;

            var worldX = panelX;
            var worldY = panelY;
            var nearestVoronis = GetNearestVoroniCell(worldX, worldY);
            if (nearestVoronis == null)
                return;

            // repaint etc

            var bmp = new Bitmap(MAP_SIZE, MAP_SIZE);

            // Get a Graphics object from the Bitmap object
            using (var g = Graphics.FromImage(bmp))
                Update(nearestVoronis, g);

            pictureBox3.Image = bmp;
        }

        private void Update(MapPolygon poly, Graphics g)
        {
            var points = new List<PointF>();
            foreach (var polyNode in poly.Nodes)
            {
                var point = new PointF();
                point.X = (float)((polyNode.X + 1) / 2 * MAP_SIZE);
                point.Y = (float)((polyNode.Y + 1) / 2 * MAP_SIZE);
                points.Add(point);
            }
            var p = new PointF();
            p.X = (float)((poly.Nodes[0].X + 1) / 2 * MAP_SIZE);
            p.Y = (float)((poly.Nodes[0].Y + 1) / 2 * MAP_SIZE);
            points.Add(p);

            g.FillPolygon(new Pen(Color.Red).Brush, points.ToArray());


            foreach (var neighbour in poly.Neighbours)
            {
                if (neighbour.IsWater && !ShowOcean)
                    continue;

                points.Clear();
                foreach (var polyNode in neighbour.Nodes)
                {
                    var point = new PointF();
                    point.X = (float)((polyNode.X + 1) / 2 * MAP_SIZE);
                    point.Y = (float)((polyNode.Y + 1) / 2 * MAP_SIZE);
                    points.Add(point);
                }
                p = new PointF();
                p.X = (float)((neighbour.Nodes[0].X + 1) / 2 * MAP_SIZE);
                p.Y = (float)((neighbour.Nodes[0].Y + 1) / 2 * MAP_SIZE);
                points.Add(p);
                g.FillPolygon(new Pen(Color.Orange).Brush, points.ToArray());
            }
        }

        private MapPolygon GetNearestVoroniCell(double x, double y)
        {
            // Iterate through all the polygons in the map
            foreach (var polygon in CurrentMap.MapPolygons)
            {
                if (polygon.IsWater && !ShowOcean)
                    continue;

                var vertices = polygon.Polygon.ExteriorRing.Coordinates.Select(coord => new PointF(
                                (float)((coord.X + 1) / 2 * MAP_SIZE), 
                                (float)((coord.Y + 1) / 2 * MAP_SIZE))).ToList();

                // Check if the point (x, y) is inside the polygon
                if (PointInPolygon(x, y, vertices))
                {
                    // If the point is inside the polygon, return the polygon
                    return polygon;
                }
            }

            // If the point is not inside any polygon, return null
            return null;
        }

        // Implementation of the PointInPolygon algorithm
        private bool PointInPolygon(double x, double y, List<PointF> polygonVertices)
        { 
            int i, j;
            bool c = false;
            for (i = 0, j = polygonVertices.Count - 1; i < polygonVertices.Count; j = i++)
            {
                if ((((polygonVertices[i].Y <= y) && (y < polygonVertices[j].Y)) || ((polygonVertices[j].Y <= y) && (y < polygonVertices[i].Y))) &&
                    (x < (polygonVertices[j].X - polygonVertices[i].X) * (y - polygonVertices[i].Y) / (polygonVertices[j].Y - polygonVertices[i].Y) + polygonVertices[i].X))
                {
                    c = !c;
                }
            }
            return c;
        }

        private void pictureBox3_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        }

        public static class ParseMap
        {
            public static byte[] Serialize(MapTile[,] tiles)
            {
                var w = (ushort)tiles.GetLength(0);
                var h = (ushort)tiles.GetLength(1);

                using (var ms = new MemoryStream())
                {
                    using (var wtr = new BinaryWriter(ms))
                    {
                        wtr.Write((byte)1);
                        wtr.Write((ushort)0); // this will have to be adjusted if client sizes change from 2048 to anything bigger
                        wtr.Write((ushort)0); // this will have to be adjusted if client sizes change from 2048 to anything bigger
                        wtr.Write(w);
                        wtr.Write(h);
                        for (var y = 0; y < h; y++)
                            for (var x = 0; x < w; x++)
                            {
                                wtr.Write(tiles[x, y].Tile);
                                wtr.Write(tiles[x, y].Object);
                                wtr.Write((byte)tiles[x, y].Region);
                            }
                    }
                    return ZlibStream.CompressBuffer(ms.ToArray());
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            File.WriteAllBytes("test.fm", ParseMap.Serialize(MapData));
        }
    }
}

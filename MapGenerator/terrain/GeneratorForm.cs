using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Tracing;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace terrain
{
    public partial class GeneratorForm : Form
    {
        public const int MAP_SIZE = 2048;

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
                map.Generate((int)numericUpDown1.Value, (int)numericUpDown2.Value, (int)numericUpDown3.Value);
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

            var (bitmap, bitmap2) = RenderPolygons(CurrentMap);
            pictureBox1.Image = bitmap;
            pictureBox2.Image = bitmap2;
        }

        (Bitmap, Bitmap) RenderPolygons(PolygonMap map)
        {
            var rasterizer = new Rasterizer<int>(MAP_SIZE, MAP_SIZE);
            var overlayRasterizer = new Rasterizer<int>(MAP_SIZE, MAP_SIZE);

            rasterizer.Clear(Color.Transparent.ToArgb());
            overlayRasterizer.Clear(Color.Transparent.ToArgb());

            //Render lands poly
            foreach (var poly in map.MapPolygons)
            {
                var points = new List<double>();
                foreach (var polyNode in poly.Nodes)
                {
                    points.Add((polyNode.X + 1) / 2 * MAP_SIZE);
                    points.Add((polyNode.Y + 1) / 2 * MAP_SIZE);
                }
                points.Add((poly.Nodes[0].X + 1) / 2 * MAP_SIZE);
                points.Add((poly.Nodes[0].Y + 1) / 2 * MAP_SIZE);

                var color = Color.FromArgb(map.Random.Next() % 200, map.Random.Next() % 200, map.Random.Next() % 200).ToArgb();

                switch (poly.Biome)
                {
                    case "Volcanic":
                        color = Color.DarkSlateGray.ToArgb();
                        break;
                    case "Forest":
                        color = Color.Green.ToArgb();
                        break;
                    case "Desert":
                        color = Color.SandyBrown.ToArgb();
                        break;
                }

                rasterizer.FillPolygon(points.ToArray(), color);
                if (ShowGrid)
                    overlayRasterizer.DrawPolygon(points.ToArray(), Color.Black.ToArgb());
            }

            foreach (var poly in map.MapPolygons)
            {
                if (!poly.IsWater)
                    continue;

                var points = new List<double>();
                foreach (var polyNode in poly.Nodes)
                {
                    points.Add((polyNode.X + 1) / 2 * MAP_SIZE);
                    points.Add((polyNode.Y + 1) / 2 * MAP_SIZE);
                }
                points.Add((poly.Nodes[0].X + 1) / 2 * MAP_SIZE);
                points.Add((poly.Nodes[0].Y + 1) / 2 * MAP_SIZE);

                var color = Color.Blue.ToArgb();
                if (poly.IsOcean && !poly.IsCoast || poly.Neighbours.All(_ => _.IsWater))
                    color = Color.DarkBlue.ToArgb();

                if (ShowOcean)
                    rasterizer.FillPolygon(points.ToArray(), color);
                if (ShowGrid)
                    overlayRasterizer.DrawPolygon(points.ToArray(), Color.White.ToArgb());
            }


            // render water polys

            foreach (var poly in map.MapPolygons)
            {
                //foreach (var neighbours in poly.Neighbour)
                //    rasterizer.DrawLineBresenham(
                //        (int)((poly.CentroidX + 1) / 2 * MAP_SIZE),
                //        (int)((poly.CentroidY + 1) / 2 * MAP_SIZE),
                //        (int)((neighbours.CentroidX + 1) / 2 * MAP_SIZE),
                //        (int)((neighbours.CentroidY + 1) / 2 * MAP_SIZE),
                //        Color.Green.ToArgb(), 3);

                rasterizer.PlotSqr((int)((poly.CentroidX + 1) / 2 * MAP_SIZE), (int)((poly.CentroidY + 1) / 2 * MAP_SIZE), Color.Black.ToArgb(), 3);
            }

            if (RandomizeEdges)
                Randomize(map, rasterizer.Buffer);

            var bmp = new Bitmap(MAP_SIZE, MAP_SIZE);
            var buff = new BitmapBuffer(bmp);
            buff.Lock();

            for (int y = 0; y < MAP_SIZE; y++)
                for (int x = 0; x < MAP_SIZE; x++)
                    buff[x, y] = (uint)rasterizer.Buffer[x, y];
            buff.Unlock();

            var bmp2 = new Bitmap(MAP_SIZE, MAP_SIZE);
            var buff2 = new BitmapBuffer(bmp2);
            buff2.Lock();

            for (int y = 0; y < MAP_SIZE; y++)
                for (int x = 0; x < MAP_SIZE; x++)
                    buff2[x, y] = (uint)overlayRasterizer.Buffer[x, y];
            buff2.Unlock();
            return (bmp, bmp2);
        }

        void Randomize(PolygonMap map, int[,] buff)
        {
            for (int x = 8; x < MAP_SIZE - 8; x++)
                for (int y = 8; y < MAP_SIZE - 8; y++)
                    buff[x, y] = buff[x + map.Random.Next(-2, 3), y + map.Random.Next(-2, 3)];
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
    }
}

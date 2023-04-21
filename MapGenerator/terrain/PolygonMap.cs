using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using NetTopologySuite.GeometriesGraph;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Triangulate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace terrain
{
    public struct MapEdge
    {
        public MapNode From { get; set; }
        public MapNode To { get; set; }
    }

    public class MapNode
    {
        public double X { get; set; }
        public double Y { get; set; }
        public MapEdge[] Edges { get; set; }
        public bool IsWater { get; set; }
        public bool IsOcean { get; set; }
        public double? DistanceToCoast { get; set; }
    }

    public class MapPolygon
    {
        public int Id { get; set; }
        public List<MapPolygon> Neighbours { get; set; }
        public MapNode[] Nodes { get; set; }
        public double? DistanceToCoast { get; set; }
        public double CentroidX { get; set; }
        public double CentroidY { get; set; }
        public bool IsWater { get; set; }
        public bool IsCoast { get; set; }
        public bool IsOcean { get; set; }
        public string Biome { get; set; }
        public Polygon Polygon { get; set; }
        public TileRegion Region { get; set; }
    }

    public class PolygonMap
    {
        public readonly Random Random;
        public readonly Random BiomeSeed;
        public readonly Noise ElevationNoise;
        public readonly Noise MoistureNoise;
        private Noise Noise;

        public PolygonMap(int seed, int biomeSeed)
        {
            Random = new Random(seed);
            BiomeSeed = new Random(biomeSeed);
            Noise = new Noise(seed);
            ElevationNoise = new Noise(seed);
            MoistureNoise = new Noise(seed);
        }


        private string[] Biomes = new string[3] { "Volcanic", "Forest", "Desert" };

        public IGeometryCollection VoronoiDiagram { get; private set; }
        public List<MapPolygon> MapPolygons { get; private set; }
        public IEnumerable<MapNode> Oceans { get { return oceans; } }

        HashSet<MapNode> waters = new HashSet<MapNode>();
        HashSet<MapNode> oceans = new HashSet<MapNode>();

        static IGeometryCollection ClipGeometryCollection(IGeometryCollection geom, Envelope clipEnv)
        {
            var clipPoly = geom.Factory.ToGeometry(clipEnv);
            var clipped = new List<IGeometry>();
            for (int i = 0; i < geom.NumGeometries; i++)
            {
                var g = geom.GetGeometryN(i);

                IGeometry result = null;
                // don't clip unless necessary
                if (clipEnv.Contains(g.EnvelopeInternal))
                    result = g;
                else if (clipEnv.Intersects(g.EnvelopeInternal))
                {
                    result = clipPoly.Intersection(g);
                    // keep vertex key info
                    result.UserData = g.UserData;
                }

                if (result != null && !result.IsEmpty)
                    clipped.Add(result);
            }
            return geom.Factory.CreateGeometryCollection(GeometryFactory.ToGeometryArray(clipped));
        }

        public void Generate(int pointCount, int optimiseSteps, int blobSize)
        {
            //Generate random points
            var hashSet = new HashSet<Coordinate>(pointCount);

            while (hashSet.Count < pointCount)
            {
                var x = Random.NextDouble() * 2 - 1;
                var y = Random.NextDouble() * 2 - 1;
                if (x < -0.99 || y < -0.99 || x > 0.99 || y > 0.99)
                    continue;

                _ = hashSet.Add(new Coordinate(x, y));
            }

            //Optimize points
            {
                var points = hashSet.ToArray();
                for (int i = 0; i < optimiseSteps; i++)
                {
                    var builder = new VoronoiDiagramBuilder();
                    builder.SetSites(points);
                    VoronoiDiagram = builder.GetDiagram(new GeometryFactory());
                    for (int j = 0; j < points.Length; j++)
                    {
                        Polygon poly = VoronoiDiagram[j] as Polygon;
                        points[j] = new Coordinate(poly.Centroid.X, poly.Centroid.Y);
                    }
                }
            }

            //Build graph
            PlanarGraph graph;
            {
                VoronoiDiagram = ClipGeometryCollection(VoronoiDiagram, new Envelope(-1, 1, -1, 1));
                graph = new PlanarGraph(new OverlayNodeFactory());
                var edges = new List<Edge>();
                for (int i = 0; i < VoronoiDiagram.Count; i++)
                {
                    Polygon poly = VoronoiDiagram[i] as Polygon;
                    var coords = poly.Coordinates;
                    for (int j = 1; j < coords.Length; j++)
                    {
                        edges.Add(new Edge(new Coordinate[] { coords[j - 1], coords[j] }, new Label(Location.Boundary)));
                    }
                }
                graph.AddEdges(edges);
            }

            //Convert graph
            Dictionary<Node, MapNode> nodeDict;
            {
                Dictionary<MapPolygon, HashSet<MapPolygon>> polys = new Dictionary<MapPolygon, HashSet<MapPolygon>>();
                nodeDict = new Dictionary<Node, MapNode>();
                Dictionary<MapNode, Tuple<HashSet<MapPolygon>, HashSet<MapEdge>>> dats = new Dictionary<MapNode, Tuple<HashSet<MapPolygon>, HashSet<MapEdge>>>();
                for (int i = 0; i < VoronoiDiagram.Count; i++)
                {
                    List<MapNode> nodes = new List<MapNode>();
                    var poly = new MapPolygon()
                    {
                        CentroidX = VoronoiDiagram[i].Centroid.X,
                        CentroidY = VoronoiDiagram[i].Centroid.Y,
                        Polygon = VoronoiDiagram[i] as Polygon
                    };
                    foreach (var j in VoronoiDiagram[i].Coordinates.Skip(1))
                    {
                        Node n = graph.Find(j);
                        MapNode mapNode;
                        if (!nodeDict.TryGetValue(n, out mapNode))
                        {
                            mapNode = new MapNode() { X = j.X, Y = j.Y };
                            dats[mapNode] = new Tuple<HashSet<MapPolygon>, HashSet<MapEdge>>(new HashSet<MapPolygon>() { poly }, new HashSet<MapEdge>());
                        }
                        else
                            dats[mapNode].Item1.Add(poly);
                        nodes.Add(nodeDict[n] = mapNode);
                    }
                    poly.Nodes = nodes.ToArray();
                    polys.Add(poly, new HashSet<MapPolygon>());
                }
                foreach (var i in nodeDict)
                {
                    foreach (var j in dats[i.Value].Item1)
                        foreach (var k in dats[i.Value].Item1)
                            if (j != k)
                            {
                                polys[j].Add(k);
                                polys[k].Add(j);
                            }
                    foreach (var j in i.Key.Edges)
                    {
                        var from = nodeDict[graph.Find(j.Coordinate)];
                        var to = nodeDict[graph.Find(j.DirectedCoordinate)];
                        dats[from].Item2.Add(new MapEdge() { From = from, To = to });
                    }
                }
                int ftrh = dats.Count(_ => _.Value.Item2.Count == 0);
                foreach (var i in dats)
                    i.Key.Edges = i.Value.Item2.ToArray();

                var x = polys.ToArray();
                for (int i = 0; i < x.Length; i++)
                {
                    x[i].Key.Neighbours = x[i].Value.ToList();
                    x[i].Key.Id = i;
                }
                MapPolygons = x.Select(_ => _.Key).ToList();
            }

            // generate water

            waters.Clear();
            foreach (var i in MapPolygons)
            {
                foreach (var j in i.Nodes)
                {
                    var n = Noise.GetNoise((j.X + 1) * 2, (j.Y + 1) * 2, 0);

                    var d = j.X * j.X + j.Y * j.Y;
                    if (n < d * 0.7 || (Math.Abs(j.X) > 0.9 || Math.Abs(j.Y) > 0.9))
                    {
                        j.IsWater = true;
                        i.IsWater = true;
                        waters.Add(j);
                    }
                }
            }

            // generate biomes

            // Initialize each polygon with a random biome

            foreach (var polygon in MapPolygons)
            {
                if (polygon.IsWater)
                    continue;
                polygon.Biome = Biomes[BiomeSeed.Next(Biomes.Length)];
            }

            // Cluster biomes together in blobs using a flood-fill approach

            List<MapPolygon> unvisited = new List<MapPolygon>(MapPolygons);
            List<string> availableBiomes = new List<string>(Biomes);

            while (unvisited.Count > 0)
            {
                int size = Random.Next(blobSize / 2, blobSize + 1);
                string currentBiome;

                if (availableBiomes.Count > 0)
                {
                    currentBiome = availableBiomes[BiomeSeed.Next(availableBiomes.Count)];
                    availableBiomes.Remove(currentBiome);
                }
                else
                {
                    currentBiome = Biomes[BiomeSeed.Next(Biomes.Length)];
                }

                MapPolygon startPolygon = unvisited[Random.Next(unvisited.Count)];
                Queue<MapPolygon> queue = new Queue<MapPolygon>();
                queue.Enqueue(startPolygon);

                while (queue.Count > 0 && size > 0)
                {
                    MapPolygon currentPolygon = queue.Dequeue();
                    if (!unvisited.Contains(currentPolygon))
                        continue;

                    unvisited.Remove(currentPolygon);
                    currentPolygon.Biome = currentBiome;
                    size--;

                    foreach (MapPolygon neighbor in currentPolygon.Neighbours)
                    {
                        if (unvisited.Contains(neighbor))
                            queue.Enqueue(neighbor);
                    }
                }
            }

            FindLakesAndCoasts();
        }

        public void FindLakesAndCoasts()
        {
            var lake = new HashSet<MapPolygon>(MapPolygons.Where(_ => _.IsWater));
            var  coast = new HashSet<MapPolygon>();
            var start = MapPolygons.First(_ => _.Nodes.Any(__ => __.X == -1 && __.Y == -1));
            _ = lake.Remove(start);

            var q = new Queue<MapPolygon>();
            q.Enqueue(start);
            do
            {
                var poly = q.Dequeue();
                foreach (var i in poly.Neighbours)
                    if (i.IsWater && lake.Contains(i))
                    {
                        if (i.Neighbours.Any(_ => !_.IsWater))
                            _ = coast.Add(i);
                        _ = lake.Remove(i);
                        q.Enqueue(i);
                    }
            }
            while (q.Count > 0);

            foreach (var i in lake)
            {
                i.IsOcean = false;
                i.IsCoast =true;
            }
            foreach (var i in coast)
                i.IsCoast = true;
        }
    }
}
 
//// Cluster biomes together in blobs using a flood-fill approach
//int minBlobSize = 500; // Adjust this value to control the minimum size of each biome blob
//int maxBlobSize = 1000; // Adjust this value to control the maximum size of each biome blob

//List<MapPolygon> unvisited = new List<MapPolygon>(Polygons);
//List<string> availableBiomes = new List<string>(Biomes);

//while (unvisited.Count > 0)
//{
//    int blobSize = Random.Next(minBlobSize, maxBlobSize + 1);
//    string currentBiome;

//    if (availableBiomes.Count > 0)
//    {
//        currentBiome = availableBiomes[Random.Next(availableBiomes.Count)];
//        availableBiomes.Remove(currentBiome);
//    }
//    else
//    {
//        currentBiome = Biomes[Random.Next(Biomes.Length)];
//    }

//    MapPolygon startPolygon = unvisited[Random.Next(unvisited.Count)];
//    Queue<MapPolygon> queue = new Queue<MapPolygon>();
//    queue.Enqueue(startPolygon);

//    while (queue.Count > 0 && blobSize > 0)
//    {
//        MapPolygon currentPolygon = queue.Dequeue();
//        if (!unvisited.Contains(currentPolygon))
//            continue;

//        unvisited.Remove(currentPolygon);
//        currentPolygon.Biome = currentBiome;
//        blobSize--;

//        foreach (MapPolygon neighbor in currentPolygon.Neighbour)
//        {
//            if (unvisited.Contains(neighbor))
//                queue.Enqueue(neighbor);
//        }
//    }
//}
//    }
//}

//public static double Clamp01(double value)
//        {
//            return value < 0.0 ? 0.0 : (value > 1.0 ? 1.0 : value);
//        }
//    }
//}
//// Cluster biomes together in blobs using a flood-fill approach
//int minBlobSize = 5; // Adjust this value to control the minimum size of each biome blob
//int maxBlobSize = 10; // Adjust this value to control the maximum size of each biome blob

//List<MapPolygon> unvisited = new List<MapPolygon>(Polygons);
//List<string> availableBiomes = new List<string>(Biomes);

//while (unvisited.Count > 0)
//{
//    int blobSize = Random.Next(minBlobSize, maxBlobSize + 1);
//    string currentBiome;

//    if (availableBiomes.Count > 0)
//    {
//        currentBiome = availableBiomes[Random.Next(availableBiomes.Count)];
//        availableBiomes.Remove(currentBiome);
//    }
//    else
//    {
//        currentBiome = Biomes[Random.Next(Biomes.Length)];
//    }

//    MapPolygon startPolygon = unvisited[Random.Next(unvisited.Count)];
//    Queue<MapPolygon> queue = new Queue<MapPolygon>();
//    queue.Enqueue(startPolygon);

//    while (queue.Count > 0 && blobSize > 0)
//    {
//        MapPolygon currentPolygon = queue.Dequeue();
//        if (!unvisited.Contains(currentPolygon))
//            continue;

//        unvisited.Remove(currentPolygon);
//        currentPolygon.Biome = currentBiome;
//        blobSize--;

//        foreach (MapPolygon neighbor in currentPolygon.Neighbour)
//        {
//            if (unvisited.Contains(neighbor))
//                queue.Enqueue(neighbor);
//        }
//    }
//}
//}
//    }
//    }
//}

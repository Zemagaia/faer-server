﻿/*
    Copyright (C) 2015 creepylava

    This file is part of RotMG Dungeon Generator.

    RotMG Dungeon Generator is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.

*/

using System;
using DungeonGenerator.Dungeon;
using RotMG.Common.Rasterizer;

namespace DungeonGenerator.Templates.Abyss; 

internal class Overlay : MapRender
{
    private static readonly DungeonObject floor = new()
    {
        ObjectType = AbyssTemplate.PartialRedFloor
    };

    private static readonly DungeonObject broken = new()
    {
        ObjectType = AbyssTemplate.BrokenRedPillar
    };

    private byte[,] GenerateHeightMap(int w, int h)
    {
        var map = new float[w, h];
        var maxR = Math.Min(w, h);
        var r = Rand.Next(maxR * 1 / 3, maxR * 2 / 3);
        var r2 = r * r;

        for (var i = 0; i < 200; i++)
        {
            int cx = Rand.Next(w), cy = Rand.Next(h);
            var fact = (float)Rand.NextDouble() * 3 + 1;
            if (Rand.Next() % 2 == 0)
                fact = 1 / fact;

            for (var x = 0; x < w; x++)
            for (var y = 0; y < h; y++)
            {
                var z = r2 - ((x - cx) * (x - cx) / fact + (y - cy) * (y - cy) * fact);
                if (z < 0)
                    continue;
                map[x, y] += z / r2;
            }
        }

        float max = 0;
        var min = float.MaxValue;
        for (var x = 0; x < w; x++)
        for (var y = 0; y < h; y++)
        {
            if (map[x, y] > max)
                max = map[x, y];
            else if (map[x, y] < min)
                min = map[x, y];
        }

        var norm = new byte[w, h];
        for (var x = 0; x < w; x++)
        for (var y = 0; y < h; y++)
        {
            var normVal = (map[x, y] - min) / (max - min);
            norm[x, y] = (byte)(normVal * normVal * byte.MaxValue);
        }

        return norm;
    }

    private static int Lerp(int a, int b, float val)
    {
        return a + (int)((b - a) * val);
    }

    private void RenderBackground()
    {
        const int Sample = 4;

        int w = Rasterizer.Width, h = Rasterizer.Height;
        var buf = Rasterizer.Bitmap;
        var hm = GenerateHeightMap(w / Sample + 2, h / Sample + 2);

        for (var x = 0; x < w; x++)
        for (var y = 0; y < h; y++)
        {
            if (buf[x, y].TileType == AbyssTemplate.Lava ||
                buf[x, y].TileType == AbyssTemplate.Space)
                continue;

            if (buf[x, y].Region == "Treasure")
            {
                buf[x, y].Region = null;
                continue;
            }

            int dx = x / Sample, dy = y / Sample;
            var hx1 = Lerp(hm[dx, dy], hm[dx + 1, dy], (x % Sample) / (float)Sample);
            var hx2 = Lerp(hm[dx, dy + 1], hm[dx + 1, dy + 1], (x % Sample) / (float)Sample);
            var hv = Lerp(hx1, hx2, (y % Sample) / (float)Sample);

            if ((hv / 10) % 2 == 0)
            {
                buf[x, y].TileType = AbyssTemplate.Lava;
                if (Rand.NextDouble() > 0.9 && buf[x, y].Object == null)
                    buf[x, y].Object = floor;
            }
        }
    }

    private void RenderSafeGround()
    {
        StartRoom startRm = null;
        foreach (var room in Graph.Rooms)
            if (room is StartRoom)
            {
                startRm = (StartRoom)room;
                break;
            }

        if (startRm == null)
            return;

        var buf = Rasterizer.Bitmap;
        var pos = startRm.portalPos;
        for (var dx = -1; dx <= 1; dx++)
        for (var dy = -1; dy <= 1; dy++)
        {
            var tile = buf[pos.X + dx, pos.Y + dy];
            if (tile.TileType == AbyssTemplate.Lava)
            {
                tile.TileType = AbyssTemplate.RedSmallChecks;
                if (tile.Object != null && tile.Object.ObjectType != AbyssTemplate.CowardicePortal)
                    tile.Object = null;
            }

            buf[pos.X + dx, pos.Y + dy] = tile;
        }
    }

    private void RenderLavaGround(Point a, Point b)
    {
        Rasterizer.DrawLine(a, b, (x, y) =>
        {
            if (Rasterizer.Bitmap[x, y].TileType == AbyssTemplate.Lava)
                return new DungeonTile
                {
                    TileType = AbyssTemplate.Lava,
                    Object = floor
                };
            return Rasterizer.Bitmap[x, y];
        }, 1);
    }

    private void RenderBossEdge(Room src, Room dst, Direction direction, int offset)
    {
        switch (direction)
        {
            case Direction.North:
                RenderLavaGround(
                    new Point(offset, dst.Pos.Y + 35),
                    new Point(offset, src.Pos.Y));
                break;

            case Direction.South:
                RenderLavaGround(
                    new Point(offset, src.Pos.Y + src.Height),
                    new Point(offset, dst.Pos.Y + 10));
                break;

            case Direction.West:
                RenderLavaGround(
                    new Point(dst.Pos.X + 35, offset),
                    new Point(src.Pos.X, offset));
                break;

            case Direction.East:
                RenderLavaGround(
                    new Point(src.Pos.X + src.Width, offset),
                    new Point(dst.Pos.X + 10, offset));
                break;

            default:
                throw new ArgumentException();
        }
    }

    private void RenderConnection()
    {
        foreach (var room in Graph.Rooms)
        {
            var xRange = new Range(room.Width * 1 / 4, room.Width * 3 / 4);
            var yRange = new Range(room.Height * 1 / 4, room.Height * 3 / 4);
            var pt = new Point(room.Pos.X + xRange.Random(Rand), room.Pos.Y + yRange.Random(Rand));

            foreach (var edge in room.Edges)
            {
                var direction = edge.Linkage.Direction;
                var randOffset = edge.Linkage.Offset + edge.Linkage.Offset % 3;

                if (edge.RoomA != room)
                    direction = direction.Reverse();
                else if (edge.RoomB is BossRoom)
                {
                    RenderBossEdge(edge.RoomA, edge.RoomB, direction, randOffset);
                }
                else if (edge.RoomA is BossRoom)
                {
                    RenderBossEdge(edge.RoomB, edge.RoomA, direction.Reverse(), randOffset);
                }

                if (room is BossRoom)
                    continue;

                Point pos;
                switch (direction)
                {
                    case Direction.North:
                        pos = new Point(randOffset, room.Pos.Y);
                        break;

                    case Direction.South:
                        pos = new Point(randOffset, room.Pos.Y + room.Height);
                        break;

                    case Direction.West:
                        pos = new Point(room.Pos.X, randOffset);
                        break;

                    case Direction.East:
                        pos = new Point(room.Pos.X + room.Width, randOffset);
                        break;

                    default:
                        throw new ArgumentException();
                }

                RenderLavaGround(pos, pt);
            }

            if (room is StartRoom)
                RenderLavaGround(((StartRoom)room).portalPos, pt);
        }
    }

    private void RenderPillars()
    {
        var buf = Rasterizer.Bitmap;
        int w = Rasterizer.Width, h = Rasterizer.Height;
        for (var x = 0; x < w; x++)
        for (var y = 0; y < h; y++)
        {
            if (buf[x, y].Object != null && buf[x, y].Object.ObjectType == AbyssTemplate.RedPillar &&
                Rand.NextDouble() > 0.7)
                buf[x, y].Object = broken;
        }
    }

    private void RenderWalls()
    {
        var wallA = new DungeonTile
        {
            TileType = AbyssTemplate.Space,
            Object = new DungeonObject
            {
                ObjectType = AbyssTemplate.RedWall
            }
        };
        var wallB = new DungeonTile
        {
            TileType = AbyssTemplate.Space,
            Object = new DungeonObject
            {
                ObjectType = AbyssTemplate.RedTorchWall
            }
        };

        var buf = Rasterizer.Bitmap;
        var tmp = (DungeonTile[,])buf.Clone();
        int w = Rasterizer.Width, h = Rasterizer.Height;

        for (var x = 0; x < w; x++)
        for (var y = 0; y < h; y++)
        {
            if (buf[x, y].TileType != AbyssTemplate.Space)
                continue;

            var notWall = true;
            if (x == 0 || y == 0 || x + 1 == w || y + 1 == h)
                notWall = true;
            else
            {
                for (var dx = -1; dx <= 1 && notWall; dx++)
                for (var dy = -1; dy <= 1 && notWall; dy++)
                {
                    if (tmp[x + dx, y + dy].TileType != AbyssTemplate.Space)
                    {
                        notWall = false;
                        break;
                    }
                }
            }

            if (!notWall)
                buf[x, y] = Rand.NextDouble() < 0.9 ? wallA : wallB;
        }
    }

    public override void Rasterize()
    {
        RenderBackground();
        RenderSafeGround();
        RenderConnection();
        RenderPillars();
        RenderWalls();
    }
}
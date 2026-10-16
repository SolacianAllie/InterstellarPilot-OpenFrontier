using System;
using System.Collections.Generic;
using System.IO;
using OpenFrontier.Editor;

static class Program
{
    // A UV sphere with the same shape as the planet mesh: 2143 verts is the
    // real count, but a smaller one keeps the fixture readable. Winding is
    // counter-clockwise seen from outside, as Unity produces.
    static void Sphere(string path, int rings, int sectors, float radius)
    {
        var pos = new List<float>();
        var nrm = new List<float>();
        var tan = new List<float>();
        var uv = new List<float>();
        var tri = new List<int>();

        pos.Add(0); pos.Add(radius); pos.Add(0);
        nrm.Add(0); nrm.Add(1); nrm.Add(0);
        tan.Add(1); tan.Add(0); tan.Add(0); tan.Add(-1);
        uv.Add(0.5f); uv.Add(0f);

        for (int r = 1; r < rings; r++)
        {
            float phi = MathF.PI * r / rings;
            for (int s = 0; s < sectors; s++)
            {
                float th = 2 * MathF.PI * s / sectors;
                var n = new[] { MathF.Sin(phi) * MathF.Cos(th), MathF.Cos(phi), MathF.Sin(phi) * MathF.Sin(th) };
                pos.Add(n[0] * radius); pos.Add(n[1] * radius); pos.Add(n[2] * radius);
                nrm.Add(n[0]); nrm.Add(n[1]); nrm.Add(n[2]);
                tan.Add(MathF.Cos(th)); tan.Add(0); tan.Add(-MathF.Sin(th)); tan.Add(-1);
                uv.Add((float)s / sectors); uv.Add((float)r / rings);
            }
        }
        pos.Add(0); pos.Add(-radius); pos.Add(0);
        nrm.Add(0); nrm.Add(-1); nrm.Add(0);
        tan.Add(1); tan.Add(0); tan.Add(0); tan.Add(1);
        uv.Add(0.5f); uv.Add(1f);

        int last = pos.Count / 3 - 1;
        int ringVerts = sectors;
        for (int s = 0; s < sectors; s++)
            tri.AddRange(new[] { 0, 1 + (s + 1) % sectors, 1 + s });

        for (int r = 0; r < rings - 2; r++)
        {
            int a = 1 + r * ringVerts;
            int b = 1 + (r + 1) * ringVerts;
            for (int s = 0; s < sectors; s++)
            {
                int s1 = (s + 1) % sectors;
                tri.AddRange(new[] { a + s, b + s1, a + s1 });
                tri.AddRange(new[] { a + s, b + s, b + s1 });
            }
        }

        int baseLast = 1 + (rings - 2) * ringVerts;
        for (int s = 0; s < sectors; s++)
            tri.AddRange(new[] { baseLast + s, last, baseLast + (s + 1) % sectors });

        File.WriteAllText(path, FbxMeshWriter.Build("Sphere", pos.ToArray(),
            nrm.ToArray(), tan.ToArray(), uv.ToArray(), tri.ToArray()));
        Console.WriteLine($"wrote {path}: {(pos.Count/3)} verts, {(tri.Count/3)} tris");
    }

    static void Cube(string path)
    {
        float[] p = { -1,-1,-1,  1,-1,-1,  1,1,-1,  -1,1,-1,
                       -1,-1, 1,  1,-1, 1,  1,1, 1,  -1,1, 1 };
        int[] t = { 0,2,1, 0,3,2,  4,5,6, 4,6,7,  0,1,5, 0,5,4,
                    2,3,7, 2,7,6,  0,4,7, 0,7,3,  1,2,6, 1,6,5 };
        File.WriteAllText(path, FbxMeshWriter.Build("Cube", p, null, null, null, t));
        Console.WriteLine($"wrote {path}: 8 verts, 12 tris, no layers");
    }

    static void Main()
    {
        Directory.CreateDirectory("out");
        Sphere("out/sphere.fbx", 32, 64, 10f);
        Cube("out/cube.fbx");
    }
}

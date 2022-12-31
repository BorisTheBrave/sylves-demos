using Sylves;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;

public class SylvesSpriteUtils
{
    // Resources
    private const string SolidFillSpriteShapeProfile = "SolidFillSpriteShapeProfile";

    private static Material m_UnlitMaterial = null;
    public static Material UnlitMaterial = m_UnlitMaterial
        ?? (m_UnlitMaterial = Resources.Load<Material>("UnlitMaterial"));

    private static Material m_PolygonMaterial = null;
    public static Material PolygonMaterial = m_PolygonMaterial
        ?? (m_PolygonMaterial = Resources.Load<Material>("PolygonMaterial"));

    public static GameObject CreateMesh(IGrid grid, Cell cell)
    {
        return CreateMesh(grid, new[] { cell }.ToHashSet());
    }

    public static GameObject CreateMesh(IGrid grid, HashSet<Cell> cells, bool collider = true)
    {
        var go = new GameObject();
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();

        var md = grid.Masked(cells).ToMeshData().Triangulate().InvertWinding();
        mf.mesh = md.ToMesh();

        mr.sharedMaterial = UnlitMaterial;

        if (collider)
        {
            AddCollider(go, grid, cells);
        }

        return go;
    }

    public static GameObject CreateSpriteShape(IGrid grid, Cell cell, bool collider = true)
    {
        var go = new GameObject();
        var r = go.AddComponent<SpriteShapeRenderer>();
        var c = go.AddComponent<SpriteShapeController>();
        if (collider)
        {
            c.autoUpdateCollider = true;
            go.AddComponent<PolygonCollider2D>();
        }
        var spline = c.spline;
        spline.Clear();
        var polygon = grid.GetPolygon(cell);
        foreach (var p in polygon.Reverse())
        {
            spline.InsertPointAt(0, p);
            spline.SetTangentMode(0, ShapeTangentMode.Linear);
        }
        c.RefreshSpriteShape();
        c.spriteShape = Resources.Load<SpriteShape>(SolidFillSpriteShapeProfile);
        c.splineDetail = 1;

        return go;
    }

    private static void AddCollider(GameObject go, IGrid grid, IEnumerable<Cell> cells)
    {
            var c = go.AddComponent<PolygonCollider2D>();
            c.pathCount = cells.Count();
            var i = 0;
            foreach (var cell in cells)
            {
                var path = grid.GetPolygon(cell).Select(x => (Vector2)x).ToArray();
                c.SetPath(i, path);
                i++;
            }
    }

    public static Mesh CreateStarMesh(Vector3[] polygon)
    {
        var mesh = new Mesh();
        var center = polygon.Aggregate((x, y) => x + y) / polygon.Length;
        var vertices = new Vector3[polygon.Length + 1];
        var uvs = new Vector2[polygon.Length + 1];
        vertices[0] = center;
        uvs[0] = new Vector2();
        var indices = new int[polygon.Length * 3];
        var p = polygon.Length - 1;
        for (var i = 0; i < polygon.Length; i++)
        {
            vertices[i + 1] = polygon[i];
            uvs[i + 1] = new Vector2(1, 1);
            var o = i * 3;
            indices[o + 0] = 0;
            indices[o + 1] = i + 1;
            indices[o + 2] = p + 1;
            p = i;
        }
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = indices;
        return mesh;
    }
}

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
    public static GameObject CreateMesh(IGrid grid, Cell cell)
    {
        return CreateMesh(grid, new[] { cell }.ToHashSet());
    }

    public static GameObject CreateMesh(IGrid grid, HashSet<Cell> cells, bool collider = true)
    {
        var go = new GameObject();
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();

        mf.mesh = grid.Masked(cells).ToMeshData().ToMesh();

        if(collider)
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
}

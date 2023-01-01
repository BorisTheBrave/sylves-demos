using Sylves;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteAlways]
public class ColorMap : MonoBehaviour
{
    public IGrid Grid;

    public Material material;

    public Color? defaultColor;

    private Dictionary<Cell, Color?> colors = new Dictionary<Cell, Color?>();

    private Dictionary<Vector3[], Mesh> cachedMeshes = new Dictionary<Vector3[], Mesh>();
    private Dictionary<Cell, (Mesh, Matrix4x4)> cachedMeshes2 = new Dictionary<Cell, (Mesh, Matrix4x4)>();


    public void SetColor(Cell cell, Color? color)
    {
        if (color == null)
        {
            colors.Remove(cell);
        }
        else
        {
            colors[cell] = color;
        }
    }

    public Color? GetColor(Cell cell)
    {
        return colors.GetValueOrDefault(cell);
    }

    public void Clear()
    {
        colors = new Dictionary<Cell, Color?>();
    }

    protected virtual void Start()
    {
        // Init shader
        if(material == null)
        {
            material = SylvesSpriteUtils.PolygonMaterial;
        }
    }

    protected virtual void LateUpdate()
    {
        if (Grid == null)
            return;

        var newCachedMeshes = new Dictionary<Vector3[], Mesh>();
        var newCachedMeshes2 = new Dictionary<Cell, (Mesh, Matrix4x4)>();

        (Mesh, Matrix4x4) GetMesh(Cell cell)
        {
            Mesh mesh;
            // Lookup by cell, moving from old to new if necessary
            if (newCachedMeshes2.ContainsKey(cell))
                return newCachedMeshes2[cell];
            if (cachedMeshes2.ContainsKey(cell))
            {
                var t = newCachedMeshes2[cell] = cachedMeshes2[cell];
                cachedMeshes2.Remove(cell);
                return t;
            }

            // Lookup by array, moving from old to new if necessary
            // (this is an optimization as some Sylves grids re-use arrays for multiple cells)
            Grid.GetPolygon(cell, out var polygon, out var polygonTransform);
            if (newCachedMeshes.ContainsKey(polygon))
                return (newCachedMeshes[polygon], polygonTransform);
            if (cachedMeshes.ContainsKey(polygon))
            {
                mesh = newCachedMeshes[polygon] = cachedMeshes[polygon];
                cachedMeshes.Remove(polygon);
                return (mesh, polygonTransform);
            }

            // Nothing found
            mesh = SylvesSpriteUtils.CreateStarMesh(polygon);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            newCachedMeshes[polygon] = mesh;
            newCachedMeshes2[cell] = (mesh, polygonTransform);

            return (mesh, polygonTransform);
        }


        var mpb = new MaterialPropertyBlock();
        foreach (var camera in Camera.allCameras)
        {
            foreach (var cell in GetCells(camera))
            {
                var color = CellColor(cell);
                if (color == null)
                    continue;

                var (mesh, meshTransform) = GetMesh(cell);

                mpb.SetColor("_Color", color.Value);

                Graphics.DrawMesh(mesh, transform.localToWorldMatrix * meshTransform, material, gameObject.layer, camera, 0, mpb);
            }
        }

        // Drop anything cached that hasn't been copied over
        cachedMeshes = newCachedMeshes;
        cachedMeshes2 = newCachedMeshes2;
    }

    protected virtual IEnumerable<Cell> GetCells(Camera camera = null)
    {
        camera = camera == null ? Camera.current : camera;
        if(Grid.IsPlanar)
        {
            // Restrict to just visible cells.
            // Works even for infinite grids
            var viewportPoints = new[] { new Vector3(0, 0), new Vector3(0, 1), new Vector3(1, 1), new Vector3(1, 0) };
            var localPoints = new List<Vector3>();
            foreach(var viewportPoint in viewportPoints)
            {
                var ray = camera.ViewportPointToRay(viewportPoint);
                var worldPoint = ray.origin + ray.direction * (-ray.origin.z / ray.direction.z);
                var localPoint = transform.InverseTransformPoint(worldPoint);
                localPoints.Add(localPoint);
            }
            var min = localPoints.Aggregate(Vector3.Min);
            var max = localPoints.Aggregate(Vector3.Max);
            return Grid.GetCellsIntersectsApprox(min, max)
                // Stop unity from crashing, just give up!
                .Take(100000);
        }
        else if(Grid.IsFinite)
        {
            return Grid.GetCells();
        }
        else
        {
            throw new System.Exception("Cannot get cells of infinite grid.");
        }
    }

    protected virtual Color? CellColor(Cell cell) => colors.GetValueOrDefault(cell) ?? defaultColor;
}

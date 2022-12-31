using Sylves;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteAlways]
public class ColorMap : MonoBehaviour
{
    const bool centralFan = true;

    public IGrid Grid;

    public Material material;

    private Dictionary<Cell, Color?> colors = new Dictionary<Cell, Color?>();


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
            material = SylvesSpriteUtils.UnlitDoubleSidedMaterial;
        }
    }

    protected virtual void LateUpdate()
    {
        if (Grid == null)
            return;

        // Keep this from frame to frame?
        Dictionary<Vector3[], Mesh> cachedMeshes = new Dictionary<Vector3[], Mesh>();

        var mpb = new MaterialPropertyBlock();
        foreach (var camera in Camera.allCameras)
        {
            foreach (var cell in GetCells(camera))
            {
                var color = CellColor(cell);
                if (color == null)
                    continue;

                Grid.GetPolygon(cell, out var polygon, out var polygonTransform);
                if (!cachedMeshes.TryGetValue(polygon, out var mesh))
                {
                    cachedMeshes[polygon] = mesh = new Mesh();
                    if (centralFan)
                    {
                        var center = polygon.Aggregate((x, y) => x + y) / polygon.Length;
                        var vertices = new Vector3[polygon.Length + 1];
                        var uvs = new Vector2[polygon.Length + 1];
                        vertices[0] = center;
                        uvs[0] = new Vector2();
                        var indices = new int[polygon.Length * 3];
                        var p = polygon.Length - 1;
                        for (var i=0;i<polygon.Length;i++)
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
                    }
                    else
                    {
                        mesh.vertices = polygon;
                        var indices = new int[(polygon.Length - 2) * 3];
                        for (var i = 2; i < polygon.Length; i++)
                        {
                            var o = (i - 2) * 3;
                            indices[o + 0] = 0;
                            indices[o + 1] = i;
                            indices[o + 2] = i - 1;
                        }
                        mesh.triangles = indices;
                    }
                    mesh.RecalculateBounds();
                    mesh.RecalculateNormals();
                }

                mpb.SetColor("_Color", color.Value);

                Graphics.DrawMesh(mesh, transform.localToWorldMatrix * polygonTransform, material, gameObject.layer, camera, 0, mpb);
            }
        }
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

    protected virtual Color? CellColor(Cell cell) => colors.GetValueOrDefault(cell);
}

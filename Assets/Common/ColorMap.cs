using Sylves;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteAlways]
public class ColorMap : MonoBehaviour
{
    public IGrid Grid;

    private Material solidMaterial;
    private Material outlineMaterial;

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
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            solidMaterial = new Material(shader);
            solidMaterial.enableInstancing = true;
            solidMaterial.hideFlags = HideFlags.HideAndDontSave;
            //Turn on alpha blending
            solidMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            solidMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            // Turn backface culling off
            solidMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            // Turn off depth writes
            //solidMaterial.SetInt("_ZWrite", 0);
        }
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            outlineMaterial = new Material(shader);
            outlineMaterial.enableInstancing = true;
            outlineMaterial.hideFlags = HideFlags.HideAndDontSave;
            //Turn on alpha blending
            outlineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            outlineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            // Turn backface culling off
            outlineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            // Turn off depth writes
            outlineMaterial.SetInt("_ZWrite", 0);
            outlineMaterial.SetFloat("_ZBias", -1.0f);
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
                    mesh.RecalculateBounds();
                    mesh.RecalculateNormals();
                }

                mpb.SetColor("_Color", color.Value);

                Graphics.DrawMesh(mesh, transform.localToWorldMatrix * polygonTransform, solidMaterial, gameObject.layer, camera, 0, mpb);
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

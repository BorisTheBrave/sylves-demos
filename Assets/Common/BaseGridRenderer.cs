using Sylves;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//[ExecuteAlways]
public class BaseGridRenderer : MonoBehaviour
{
    public IGrid Grid;

    private Material solidMaterial;
    private Material outlineMaterial;

    public virtual void Start()
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

    private void DrawCellSolid(Cell cell, Color color)
    {
        if (Grid.Is3D)
        {
            var md = Grid.GetMeshData(cell);
            GL.Begin(GL.TRIANGLES);
            GL.Color(color);
            var vertices = md.vertices;
            foreach (var face in MeshUtils.GetFaces(md, 0))
            {
                for (var i = 2; i < face.Length; i++)
                {
                    GL.Vertex(vertices[face[0]]);
                    GL.Vertex(vertices[face[i]]);
                    GL.Vertex(vertices[face[i - 1]]);
                }
            }
            GL.End();
        }
        else 
        {
            var vertices = Grid.GetPolygon(cell);
            GL.Begin(GL.TRIANGLES);
            GL.Color(color);
            for (var i = 2; i < vertices.Length; i++)
            {
                GL.Vertex(vertices[0]);
                GL.Vertex(vertices[i]);
                GL.Vertex(vertices[i - 1]);
            }
            GL.End();
        }
    }

    private void DrawCellOutline(Cell cell, Color color)
    {
        if (Grid.Is3D)
        {
            var md = Grid.GetMeshData(cell);
            GL.Begin(GL.LINES);
            GL.Color(color);
            var vertices = md.vertices;
            foreach (var face in MeshUtils.GetFaces(md, 0))
            {
                for (var i = 0; i < face.Length; i++)
                {
                    GL.Vertex(vertices[face[(i == 0 ? face.Length : i) - 1]]);
                    GL.Vertex(vertices[face[i]]);
                }
                GL.Vertex(vertices[face[face.Length - 1]]);
                GL.Vertex(vertices[face[0]]);
            }
            GL.End();
        }
        else
        {
            var vertices = Grid.GetPolygon(cell);
            GL.Begin(GL.LINES);
            GL.Color(color);
            for (var i = 0; i < vertices.Length; i++)
            {
                GL.Vertex(vertices[(i == 0 ? vertices.Length : i) - 1]);
                GL.Vertex(vertices[i]);
            }
            GL.Vertex(vertices[vertices.Length - 1]);
            GL.Vertex(vertices[0]);
            GL.End();
        }
    }

    protected virtual void OnRenderObject()
    {
        if (Grid == null)
            return;

        GL.PushMatrix();
        GL.MultMatrix(transform.localToWorldMatrix);
        solidMaterial.SetPass(0);
        foreach (var cell in GetCells())
        {
            if(CellColor(cell) is Color color)
                DrawCellSolid(cell, color);
        }

        outlineMaterial.SetPass(0);
        foreach (var cell in GetCells())
        {
            if (CellOutline(cell) is Color color)
                DrawCellOutline(cell, color);
        }

        GL.PopMatrix();
    }

    protected virtual IEnumerable<Cell> GetCells()
    {
        if(Grid.IsPlanar)
        {
            // Restrict to just visible cells.
            // Works even for infinite grids
            var viewportPoints = new[] { new Vector3(0, 0), new Vector3(0, 1), new Vector3(1, 1), new Vector3(1, 0) };
            var localPoints = new List<Vector3>();
            foreach(var viewportPoint in viewportPoints)
            {
                var ray = Camera.current.ViewportPointToRay(viewportPoint);
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

    protected virtual Color? CellColor(Cell cell) => Color.white;

    protected virtual Color? CellOutline(Cell cell) => Color.black;
}

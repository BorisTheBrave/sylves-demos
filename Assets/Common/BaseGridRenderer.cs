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
        Grid.GetPolygon(cell, out var vertices, out var transform);
        GL.Begin(GL.TRIANGLES);
        GL.Color(color);
        for (var i = 2; i < vertices.Length; i++)
        {
            GL.Vertex(transform.MultiplyPoint3x4(vertices[0]));
            GL.Vertex(transform.MultiplyPoint3x4(vertices[i]));
            GL.Vertex(transform.MultiplyPoint3x4(vertices[i - 1]));
        }
        GL.End();
    }

    private void DrawCellOutline(Cell cell, Color color)
    {
        Grid.GetPolygon(cell, out var vertices, out var transform);
        GL.Begin(GL.LINES);
        GL.Color(color);
        for (var i = 0; i < vertices.Length; i++)
        {
            GL.Vertex(transform.MultiplyPoint3x4(vertices[(i == 0 ? vertices.Length : i) - 1]));
            GL.Vertex(transform.MultiplyPoint3x4(vertices[i]));
        }
        GL.Vertex(transform.MultiplyPoint3x4(vertices[vertices.Length - 1]));
        GL.Vertex(transform.MultiplyPoint3x4(vertices[0]));
        GL.End();
    }

    protected virtual void OnRenderObject()
    {
        if (Grid == null)
            return;

        GL.PushMatrix();
        GL.MultMatrix(transform.localToWorldMatrix);
        solidMaterial.SetPass(0);
        foreach (var cell in Grid.GetCells())
        {
            if(CellColor(cell) is Color color)
                DrawCellSolid(cell, color);
        }

        outlineMaterial.SetPass(0);
        foreach (var cell in Grid.GetCells())
        {
            if (CellOutline(cell) is Color color)
                DrawCellOutline(cell, color);
        }

        GL.PopMatrix();
    }
    protected virtual Color? CellColor(Cell cell) => Color.white;

    protected virtual Color? CellOutline(Cell cell) => Color.black;
}

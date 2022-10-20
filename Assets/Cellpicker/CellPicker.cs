using Sylves;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

//[ExecuteAlways]
public class CellPicker : BaseGridRenderer
{
    public Mesh mesh;
    public float layerHeight = 1;

    private Cell? selectedCell;

    // Indexed by dual cell.x
    private Dictionary<int, int> heights;

    public override void Start()
    {
        base.Start();

        var primalMeshData = new MeshData(mesh);
        var primalMeshGrid = new MeshGrid(primalMeshData);

        var dualMeshData = MakeDual(primalMeshData, primalMeshGrid);
        dualMeshData.RecalculateNormals();
        var dualMeshGrid = new MeshGrid(dualMeshData);

        heights = dualMeshGrid.GetCells().ToDictionary(cell => cell.x, _ => 0);

        Grid = new MeshPrismGrid(dualMeshData, new MeshPrismGridOptions
        {
            MinLayer = 0,
            MaxLayer = 1,
            LayerHeight = 0.3f,
            LayerOffset = 3f,
        });

    }

    private void Update()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var origin = transform.worldToLocalMatrix.MultiplyPoint3x4(ray.origin);
        var direction= transform.worldToLocalMatrix.MultiplyVector(ray.direction);

        //origin = new Vector3(-0.6740497f, 9.531415f, - 1.921084f);
        //direction = new Vector3(-0.04277391f, - 0.937008f, 0.3466791f);


        Debug.Log($"ray {origin.x} {origin.y} {origin.z} {direction.x} {direction.y} {direction.z}");

        var h = Grid.Raycast(origin, direction)
            //.Where(r => r.cell.y <= heights[r.cell.x])
            .Cast<RaycastInfo?>()
            .FirstOrDefault();

        selectedCell = h?.cell;

        if (h != null)
        {
            Debug.Log(selectedCell);

            var x = h.Value.cell.x;
            if(Input.GetMouseButtonDown(0))
            {
                heights[x] = Mathf.Max(5, heights[x] + 1);
            }
            if (Input.GetMouseButtonDown(1))
            {
                heights[x] = Mathf.Min(0, heights[x] - 1);
            }
        }
    }

    protected override Color? CellColor(Cell cell)
    {
        if(selectedCell != null)
        {
            return Grid.GetNeighbours(selectedCell.Value).Contains(cell)? Color.red : null;
        }

        return cell == selectedCell ? Color.red : null;
    }

    // This is a fairly simple implementation, I'm still working on a robust version to put in
    // Sylves
    private static MeshData MakeDual(MeshData primalMeshData, MeshGrid primalMeshGrid = null)
    {
        primalMeshGrid = primalMeshGrid ?? new MeshGrid(primalMeshData);

        var baseGrid = primalMeshGrid;
        var indices = primalMeshData.indices[0];
        var vertices = primalMeshData.vertices;
        var dualVertices = new Vector3[indices.Length / 3];
        for (var i = 0; i < indices.Length; i += 3)
        {
            var v0 = vertices[indices[i + 0]];
            var v1 = vertices[indices[i + 1]];
            var v2 = vertices[indices[i + 2]];
            dualVertices[i / 3] = (v0 + v1 + v2) / 3;
        }
        var visited = new bool[indices.Length];
        var dualIndices = new List<int>();
        for (var i = 0; i < indices.Length; i++)
        {
            if (visited[i])
                continue;
            var tri = i / 3;
            var dir = i % 3;
            var origTri = tri;
            do
            {
                visited[tri * 3 + dir] = true;
                dualIndices.Add(tri);
                if (!baseGrid.TryMove(new Cell(tri, 0), (CellDir)dir, out var dest, out var inverseDir, out var _))
                    throw new System.Exception();
                tri = dest.x;
                dir = ((int)inverseDir + 2) % 3;
            }
            while (tri != origTri);
            dualIndices[dualIndices.Count - 1] = ~dualIndices[dualIndices.Count - 1];
        }
        var meshData = new MeshData
        {
            indices = new[] { dualIndices.ToArray() },
            vertices = dualVertices,
            subMeshCount = 1,
            topologies = new[] { Sylves.MeshTopology.NGon },
        };
        return meshData;
    }
}

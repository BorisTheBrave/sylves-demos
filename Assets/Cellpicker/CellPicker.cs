using Sylves;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

//[ExecuteAlways]
public class CellPicker : BaseGridRenderer
{
    // Configuration
    public Mesh mesh;
    public float layerHeight = 1;

    public GameObject triangle;

    MeshGrid primalMeshGrid;
    MeshPrismGrid primalMeshPrismGrid;
    MeshGrid dualMeshGrid;
    MeshPrismGrid dualMeshPrismGrid;

    // Indexed by mesh vertices (i.e. dual cell.x)
    private Dictionary<int, int> terrain;

    // Editor state
    private Cell? selectedCell;

    public override void Start()
    {
        base.Start();

        var primalMeshData = new MeshData(mesh);
        primalMeshGrid = new MeshGrid(primalMeshData, new MeshGridOptions
        {
            // Make Sylves work closer to Unity's Y-up convention.
            UseXZPlane = true,
        });
        primalMeshPrismGrid = new MeshPrismGrid(primalMeshData, new MeshPrismGridOptions
        {
            LayerHeight = 0.1f,
            LayerOffset = -0.15f,
            UseXZPlane = true,
        });

        var dualMeshData = MakeDual(primalMeshData, primalMeshGrid);
        dualMeshData.RecalculateNormals();
        dualMeshGrid = new MeshGrid(dualMeshData, new MeshGridOptions
        {
            UseXZPlane = true,
        });
        dualMeshPrismGrid = new MeshPrismGrid(dualMeshData, new MeshPrismGridOptions
        {
            LayerHeight = 0.1f,
            LayerOffset = -0.15f,
            UseXZPlane = true,
        });

        terrain = dualMeshGrid.GetCells().ToDictionary(cell => cell.x, _ => 0);

        Grid = dualMeshGrid;
    }

    private void Update()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var origin = transform.worldToLocalMatrix.MultiplyPoint3x4(ray.origin);
        var direction= transform.worldToLocalMatrix.MultiplyVector(ray.direction);

        var h = Grid.Raycast(origin, direction)
            .Cast<RaycastInfo?>()
            .FirstOrDefault();

        selectedCell = h?.cell;

        if (h != null)
        {
            var x = h.Value.cell.x;
            var i = 0;
            if(Input.GetMouseButtonDown(0))
            {
                i = 1;
            }
            if (Input.GetMouseButtonDown(1))
            {
                i = -1;
            }
            if (i != 0)
            {
                terrain[x] = (terrain[x] + i) % 3;
                Regen();
            }
        }
    }


    private void Regen()
    {
        foreach(Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        foreach(var cell in primalMeshPrismGrid.GetCells())
        {
            var go = Instantiate(triangle, transform);
            go.name = $"{triangle.name} {cell}";
            var deformation = primalMeshPrismGrid.GetDeformation(cell);
            var meshFilter = go.GetComponent<MeshFilter>();
            meshFilter.mesh = deformation.Deform(meshFilter.mesh);
        }
    }

    protected override Color? CellColor(Cell cell)
    {
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

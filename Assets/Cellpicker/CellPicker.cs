using Sylves;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class TerrainTile
{
    public int terrain1;
    public int terrain2;
    public int terrain3;
    public GameObject gameObject;
}

//[ExecuteAlways]
public class CellPicker : BaseGridRenderer
{
    // Configuration
    public Mesh mesh;
    public Color highlightColor;

    public int terrainCount = 2;

    public TerrainTile[] terrainTiles;

    MeshGrid primalMeshGrid;
    MeshPrismGrid primalMeshPrismGrid;
    MeshGrid dualMeshGrid;
    MeshPrismGrid dualMeshPrismGrid;
    IDictionary<Cell, List<Cell>> primalCellToDualCells;

    // Indexed by mesh vertices (i.e. dual cell.x)
    private Dictionary<int, int> terrain;

    // Editor state
    private Cell? selectedCell;
    private int? currentlyPainting;

    public void SetSize(int size)
    {
        if (mesh == null)
            throw new System.Exception("Mesh is null");

        var primalMeshData = new MeshData(mesh);
        //primalMeshData = ConwayOperators.Kis(primalMeshData);

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

        var (dualMeshData, primalFaceToDualFace) = MakeDual(primalMeshData);
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

        primalCellToDualCells = primalFaceToDualFace.ToDictionary(x => new Cell(x.Key, 0), x => x.Value.Select(y => new Cell(y, 0)).ToList());

        terrain = dualMeshGrid.GetCells().ToDictionary(cell => cell.x, _ => 0);

        Grid = dualMeshGrid;

        Regen();
    }

    public override void Start()
    {
        base.Start();
        SetSize(0);
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
            if(Input.GetMouseButtonDown(0))
            {
                currentlyPainting = (terrain[x] + 1) % terrainCount;
            }
            else if (Input.GetMouseButtonDown(1))
            {
                currentlyPainting = (terrain[x] - 1 + terrainCount) % terrainCount;
            }
            else if (!Input.GetMouseButton(0) && !Input.GetMouseButton(1))
            {
                currentlyPainting = null;
            }
            if (currentlyPainting != null && terrain[x] != currentlyPainting)
            {
                terrain[x] = currentlyPainting.Value;
                Regen();
            }
        }
    }

    private (GameObject, Matrix4x4) FindTileAndRotation(int terrain1, int terrain2, int terrain3)
    {
        foreach(var tt in terrainTiles)
        {
            if (tt.terrain1 == terrain1 && tt.terrain2 == terrain2 && tt.terrain3 == terrain3)
            {
                return (tt.gameObject, Matrix4x4.identity);
            }
            if (tt.terrain1 == terrain2 && tt.terrain2 == terrain3 && tt.terrain3 == terrain1)
            {
                return (tt.gameObject, Matrix4x4.Rotate(Quaternion.Euler(0, 120, 0)));
            }
            if (tt.terrain1 == terrain3 && tt.terrain2 == terrain1 && tt.terrain3 == terrain2)
            {
                return (tt.gameObject, Matrix4x4.Rotate(Quaternion.Euler(0, -120, 0)));
            }
        }
        Debug.Log($"No tile found for terrains {terrain1}, {terrain2}, {terrain3}");
        return (null, Matrix4x4.identity);
    }

    private void Regen()
    {
        foreach(Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        foreach(var cell in primalMeshPrismGrid.GetCells())
        {
            var terrains = primalCellToDualCells[cell].Select(x => terrain[x.x]).ToList();
            var (triangle, rotation) = FindTileAndRotation(terrains[2], terrains[0], terrains[1]);
            var go = Instantiate(triangle, transform);
            var deformation = primalMeshPrismGrid.GetDeformation(cell) * rotation;
            var meshFilter = go.GetComponent<MeshFilter>();
            meshFilter.mesh = deformation.Deform(meshFilter.mesh);
        }
    }

    protected override Color? CellColor(Cell cell)
    {
        return cell == selectedCell ? highlightColor : null;
    }

    protected override Color? CellOutline(Cell cell)
    {
        return cell == selectedCell ? Color.black : null;
    }


    private static (MeshData meshData, Dictionary<int, int[]> primalFaceToDualFace) MakeDual(MeshData primalMeshData)
    {
        var dmb = new Sylves.DualMeshBuilder(primalMeshData);

        var primalFaceToDualFace = dmb.Mapping.GroupBy(x => x.primalFace)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.primalVert).Select(x => x.dualFace).ToArray());

        return (dmb.DualMeshData, primalFaceToDualFace);
    }
}

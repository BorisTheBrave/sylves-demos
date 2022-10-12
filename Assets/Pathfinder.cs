using Sylves;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Pathfinder : BaseGridRenderer
{
    [Header("Tilemap")]
    public Tilemap tilemap;
    public Tile floorTile;

    [Header("Path drawing")]
    public GameObject cylinder;
    public GameObject sphere;
    public float lineWidth = 0.1f;

    private Tile paintTile;

    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();
        Grid = new TransformModifier(
            new SquareGrid(1),
            // Resize the grid isometrically.
            Matrix4x4.Translate(new Vector3(0, 0.3333f - 0.25f, 0)) * 
            Matrix4x4.Scale(new Vector3(1, 0.5f, 1) / Mathf.Sqrt(2)) * 
            Matrix4x4.Rotate(Quaternion.Euler(0, 0, 45)));
    }

    // Update is called once per frame
    void Update()
    {
        // Clear children
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Find point at cursor
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var origin = transform.worldToLocalMatrix.MultiplyPoint3x4(ray.origin);
        var direction = transform.worldToLocalMatrix.MultiplyVector(ray.direction);
        var h = Grid.Raycast(origin, direction).Cast<RaycastInfo?>().FirstOrDefault();
        var currentCell = h?.cell;

        // If hit
        if (currentCell != null)
        {
            // Find path to selected cell.
            bool isAccessible(Cell cell)
            {
                return tilemap.GetTile((Vector3Int)cell) != null;
            }
            var path = Pathfinding.FindPath(Grid, new Cell(0, 0), currentCell.Value, isAccessible);

            // Draw the path
            if (path != null)
            {
                // Setup a line renderer. Unfortunately, these don't look great in isometric views
                /*
                var positions = new List<Vector3>();
                foreach (var cell in path.Cells)
                {
                    var v = Grid.GetCellCenter(cell);
                    positions.Add(v);
                }
                lineRender.SetPositions(positions.ToArray());
                */


                // Draw a path as a series of cylinders
                foreach (var step in path.Steps)
                {
                    var start = Grid.GetCellCenter(step.Src);
                    var end = Grid.GetCellCenter(step.Dest);
                    var c = Instantiate(cylinder, transform);
                    c.transform.position = (start + end) / 2;
                    c.transform.rotation = Quaternion.FromToRotation(Vector3.up, end - start);
                    c.transform.localScale = new Vector3(lineWidth, (end - start).magnitude / 2, lineWidth);
                    var s = Instantiate(sphere, transform);
                    s.transform.position = end;
                    s.transform.localScale = Vector3.one * lineWidth;
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                // On mouse down, determine whether to set or clear the current tile
                var tile = tilemap.GetTile((Vector3Int)currentCell);
                paintTile = tile == floorTile ? null : floorTile;
            }
            if (Input.GetMouseButton(0))
            {
                // While mouse is held, continue setting/clearing tiles.
                tilemap.SetTile((Vector3Int)currentCell, paintTile);
            }
        }
    }

    protected override void OnRenderObject()
    {
        //base.OnRenderObject();
    }

    protected override Color? CellColor(Cell cell) => null;
    protected override Color? CellOutline(Cell cell) => null;
}

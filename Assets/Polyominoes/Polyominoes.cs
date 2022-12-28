using Sylves;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Polyominoes : BaseGridRenderer
{
    // The grid to base the polyominoes off
    IGrid grid;
    // All current polyominoes
    List<HashSet<Cell>> ps;

    // List of buttons used
    Dictionary<GameObject, HashSet<Cell>> buttons = new Dictionary<GameObject, HashSet<Cell>>();

    // These three indicate which polyomino is currently being painted
    // pivot/rotation indicate how to position it relative to the mouse.
    HashSet<Cell> currentPolyomino = null;
    Cell currentPivot;
    CellRotation currentRotation;

    // What has already been painted
    HashSet<Cell> filled = new HashSet<Cell>();

    // Track what we could paint next, based on the mouse position and currentPolyomino
    HashSet<Cell> hover = new HashSet<Cell>();
    // Are all the current hover cells empty?
    bool hoverOk;
    public override void Start()
    {
        base.Start();
        //grid = new SquareGrid(1);
        base.Grid = grid = new HexGrid(1);
        ps = GetPolyominoes(grid, 5);
        var x = 0f;
        foreach(var p in ps)
        {
            var go = new GameObject();
            go.name = string.Join("  ", p);
            var vertices = new List<Vector3>();
            foreach (var cell in p)
            {
                var cellSprite = SylvesSpriteUtils.CreateMesh(grid, cell);
                cellSprite.transform.parent = go.transform;
                cellSprite.name = cell.ToString();
                vertices.AddRange(grid.GetPolygon(cell));
            }
            var min = vertices.Aggregate(Vector3.Min);
            var max = vertices.Aggregate(Vector3.Max);
            go.transform.position += Vector3.right * (x - min.x);
            x = go.transform.position.x + max.x + 0.5f;

            buttons[go] = p;
        }
    }

    // Update is called once per frame
    void Update()
    {
        var mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // Work out the positioning of currentPolyomino
        // and place it into hover.
        hover.Clear();
        hoverOk = true;
        if (currentPolyomino != null)
        {
            if (grid.FindCell(mousePosition, out var mouseCell))
            {
                // Translate currentPolyomino to start out mosueCell
                var s = grid.FindGridSymmetry(
                    new[] { currentPivot }.ToHashSet(),
                    new[] { mouseCell }.ToHashSet(),
                    currentPivot,
                    currentRotation);
                if (s != null)
                {
                    foreach (var cell in currentPolyomino)
                    {
                        grid.TryApplySymmetry(s, cell, out var dest, out var _);
                        hover.Add(dest);
                        if(filled.Contains(dest))
                        {
                            hoverOk = false;
                        }
                    }
                }
            }
        }

        // Has the user clicked a button to switch currentPolyomino?
        if (Input.GetMouseButtonDown(0))
        {
            var c = Physics2D.OverlapPoint(mousePosition);
            if (c != null)
            {
                var parent = c.transform.parent.gameObject;
                currentPolyomino = buttons[parent];
                var clicked = c.name.Replace("(", "").Replace(")", "").Split(",").Select(int.Parse).ToArray();
                currentPivot = new Cell(clicked[0], clicked[1], clicked[2]);
                currentRotation = grid.GetCellType(currentPivot).GetIdentity();
            }
            else
            {
                if(hoverOk)
                {
                    foreach (var p in hover)
                        filled.Add(p);
                }
            }
        }

        // Right click rotates the current polyomino.
        if (Input.GetMouseButtonDown(1))
        {
            var cellType = grid.GetCellType(currentPivot);
            currentRotation = cellType.Multiply(currentRotation, cellType.RotateCW);
        }
    }

    protected override Color? CellColor(Cell cell)
    {
        if (hover.Contains(cell)) {
            return hoverOk ? Color.green : Color.red;
        }
        if(filled.Contains(cell))
        {
            return Color.black;
        }
        return null;
    }

    protected override Color? CellOutline(Cell cell)
    {
        return null;
    }
    private static List<HashSet<Cell>> GetPolyominoes(IGrid grid, int size)
    {
        if(size == 1)
        {
            return new List<HashSet<Cell>>
            {
                new HashSet<Cell> {new Cell() }
            };
        }

        var smallerPolyominoes = GetPolyominoes(grid, size - 1);
        var polyominoes = new List<HashSet<Cell>>();
        foreach(var p in smallerPolyominoes)
        {
            // Find all the spaces we could add a polyomino to
            var openCells = p
                .SelectMany(c => grid.GetNeighbours(c))
                .Where(c => !p.Contains(c))
                .ToHashSet();
            foreach(var c in openCells)
            {
                // Create a new poly
                var next = p.Concat(new[] { c }).ToHashSet();
                // Check if we've already created
                if (polyominoes.Any(current => AreEquivalent(grid, current, next)))
                    continue;
                // It's unique, add it to the list
                polyominoes.Add(next);
            }
        }
        return polyominoes;
    }

    private static bool AreEquivalent(IGrid grid, HashSet<Cell> polyomino1, HashSet<Cell> polyomino2)
    {
        var srcCell = polyomino1.First();
        foreach(var rotation in grid.GetCellType(srcCell).GetRotations())
        {
            var sym = grid.FindGridSymmetry(polyomino1, polyomino2, srcCell, rotation);
            if (sym != null)
            {
                return true;
            }
        }
        return false;
    }

}

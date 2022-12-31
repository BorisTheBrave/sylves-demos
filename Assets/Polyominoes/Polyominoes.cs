using Sylves;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Polyominoes : MonoBehaviour, IPointerClickHandler
{
    // Components
    public ColorMap map;

    // UI Elements
    public TMP_Text gridText;
    public TMP_Text polyominoSizeText;
    public GameObject currentPolyominoHolder;

    (IGrid, string)[] allGrids = new (IGrid, string)[]
    {
        (new HexGrid(1), "Hex"),
        (new SquareGrid(1), "Square"),
        (new TriangleGrid(1), "Triangle"),
    };

    // The grid to base the polyominoes off
    IGrid grid;
    // All current polyominoes
    public int polyominoSize = 4;
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
    public void Start()
    {
        NextGrid(0);
    }

    public void NextGrid(int offset = 1)
    {
        int i;
        for (i = 0; i < allGrids.Length; i++)
        {
            if (grid == allGrids[i].Item1)
            {
                break;
            }
        }
        i = (i + offset + allGrids.Length) % allGrids.Length;
        ResetGrid(allGrids[i].Item1, allGrids[i].Item2);
    }
    public void NextCurrentPolyomino(int offset = 1)
    {
        int i;
        for (i = 0; i < ps.Count; i++)
        {
            if (currentPolyomino == ps[i])
            {
                break;
            }
        }
        i = (i + offset + ps.Count) % ps.Count;
        ResetCurrentPolyomino(ps[i], ps[i].First());
    }

    public void ChangePolyominoSize(int delta)
    {
        polyominoSize = Mathf.Clamp(polyominoSize + delta, 1, 6);
        ResetPolyominoes();
    }

    public void ResetGrid(IGrid grid, string name)
    {
        gridText.text = name;
        map.Clear();
        map.Grid = this.grid = grid;
        ResetPolyominoes();
    }

    public void ResetPolyominoes()
    {
        polyominoSizeText.text = polyominoSize.ToString();
        ps = GetPolyominoes(grid, polyominoSize);
        ResetCurrentPolyomino(ps.First(), ps.First().First());
        // Draw all the polyominoes
        /*
        var margin = 0.5f;
        var x = 0f;
        var y = 0f;
        var nextY = 0f;
        foreach (var p in ps)
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
            go.transform.position += Vector3.up * (y - min.y);
            x = go.transform.position.x + max.x + margin;
            nextY = Mathf.Max(nextY, go.transform.position.y + max.y + margin);
            if(x > 30)
            {
                y = nextY;
                x = 0;
            }

            buttons[go] = p;
        }
        */
    }

    public void ResetCurrentPolyomino(HashSet<Cell> currentPolyomino, Cell currentPivot)
    {
        this.currentPolyomino = currentPolyomino;
        this.currentPivot = currentPivot;

        foreach (var button in buttons.Keys.ToList())
        {
            buttons.Remove(button);
            Destroy(button.gameObject);
        }


        var go = new GameObject();
        go.name = string.Join("  ", currentPolyomino);
        var vertices = new List<Vector3>();
        foreach (var cell in currentPolyomino)
        {
            var cellSprite = SylvesSpriteUtils.CreateSpriteShape(grid, cell);
            cellSprite.transform.parent = go.transform;
            cellSprite.name = cell.ToString();
            vertices.AddRange(grid.GetPolygon(cell));
        }
        var min = vertices.Aggregate(Vector3.Min);
        var max = vertices.Aggregate(Vector3.Max);
        go.transform.parent = currentPolyominoHolder.transform;
        go.transform.position = currentPolyominoHolder.transform.position + new Vector3(-(max.x + min.x) / 2, -(max.y + min.y) / 2,0);

        buttons[go] = currentPolyomino;
    }

    // Update is called once per frame
    void Update()
    {
        var mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0;

        // Select pivot for polyomino
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
        }
        // Clear last hover
        foreach (var cell in hover)
        {
            UpdateCellColor(cell, false);
        }
        hover.Clear();


        if (EventSystem.current.IsPointerOverGameObject())
            return;

        // Work out the positioning of currentPolyomino
        // and place it into hover.
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
                // Try again with a different pivot.
                // Needed for nice behaviour on triangle grids
                if(s == null)
                {
                    var otherPivot = grid.GetNeighbours(currentPivot).Intersect(currentPolyomino).Take(1).ToHashSet();
                    if (otherPivot.Count > 0)
                    {
                        s = grid.FindGridSymmetry(
                        otherPivot,
                        new[] { mouseCell }.ToHashSet(),
                        currentPivot,
                        currentRotation);
                    }
                }
                if (s != null)
                {
                    foreach (var cell in currentPolyomino)
                    {
                        grid.TryApplySymmetry(s, cell, out var dest, out var _);
                        hover.Add(dest);
                        if (filled.Contains(dest))
                        {
                            hoverOk = false;
                        }
                    }
                }
            }
        }

        foreach (var cell in hover)
        {
            UpdateCellColor(cell);
        }


        // Do paint
        if (Input.GetMouseButtonDown(0))
        {
            if(hoverOk)
            {
                foreach (var p in hover)
                {
                    filled.Add(p);
                    UpdateCellColor(p);
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

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("asdf");
    }

    protected void UpdateCellColor(Cell cell, bool useHover = true)
    {
        if (useHover && hover.Contains(cell)) {
            map.SetColor(cell, hoverOk ? Color.green : Color.red);
            return;
        }
        if(filled.Contains(cell))
        {
            map.SetColor(cell, Color.white);
            return;
        }
        map.SetColor(cell, null);
    }

    private static List<HashSet<Cell>> GetPolyominoes(IGrid grid, int size)
    {
        if(size == 1)
        {
            grid.FindCell(Vector3.zero, out var cell);
            return new List<HashSet<Cell>>
            {
                new HashSet<Cell> { cell }
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

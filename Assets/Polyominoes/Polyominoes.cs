using Assets.Common;
using Sylves;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Polyominoes : MonoBehaviour
{
    // Start is called before the first frame update

    IGrid grid;
    List<HashSet<Cell>> ps;
    void Start()
    {
        //grid = new SquareGrid(1);
        grid = new HexGrid(1);
        ps = GetPolyominoes(grid, 5);
        var x = 0f;
        foreach(var p in ps)
        {
            Debug.Log("Found polyomino: " +string.Join("  ", p));
            var go = new GameObject();
            var vertices = new List<Vector3>();
            foreach (var cell in p)
            {
                var cellSprite = SylvesSpriteUtils.CreateSpriteShape(grid, cell);
                cellSprite.transform.parent = go.transform;
                cellSprite.name = cell.ToString();
                vertices.AddRange(grid.GetPolygon(cell));
            }
            var min = vertices.Aggregate(Vector3.Min);
            var max = vertices.Aggregate(Vector3.Max);
            go.transform.position += Vector3.right * (x - min.x);
            x = go.transform.position.x + max.x + 0.5f;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
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

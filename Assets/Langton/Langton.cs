using Sylves;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class Langton : MonoBehaviour
{
    ColorMap colorMap;
    IGrid Grid => colorMap.Grid;

    float stepsPerSecond = 60;
    
    // Inspector options
    public float zoomSpeed = 0.0001f;
    public float zoomScale= 0.5f;
    public float panSpeed = 0.0001f;
    public GameObject arrow;

    // UI elements
    public TMP_Text gridText;
    public TMP_Text speedText;

    (IGrid, string)[] allGrids = new (IGrid, string)[]
    {
        (new HexGrid(1), "Hex"),
        (new SquareGrid(1), "Square"),
        (new TriangleGrid(1), "Triangle"),
        (new RhombilleGrid(), "Rhombille"),
        (new CairoGrid(), "Cairo"),
        (new TriHexGrid(), "TriHex"),
    };

    (float, string)[] allSpeeds = new (float, string)[]
    {
        (1, "Slowest"),
        (3, "Slow"),
        (10, "Normal"),
        (60, "Fast"),
        (600, "Faster"),
        (6000, "Too fast"),
    };

    HashSet<Cell> blackCells = new HashSet<Cell>();
    Dictionary<Cell, int> timesVisited = new Dictionary<Cell, int>();
    Vector3 minVisited;
    Vector3 maxVisited;
    int maxTimesVisited = 0;
    float spareTime = 0;
    Walker ant;

    public void Start()
    {
        colorMap = GetComponent<ColorMap>();
        NextGrid(4);
        ResetSpeed(allSpeeds[2].Item1, allSpeeds[2].Item2);
    }

    public void NextSpeed(int offset = 1)
    {
        int i;
        for (i = 0; i < allGrids.Length; i++)
        {
            if (stepsPerSecond == allSpeeds[i].Item1)
            {
                break;
            }
        }
        i = Mathf.Clamp(i + offset, 0, allSpeeds.Length - 1);
        ResetSpeed(allSpeeds[i].Item1, allSpeeds[i].Item2);
    }

    public void ResetSpeed(float speed, string name)
    {
        stepsPerSecond = speed;
        speedText.text = name;
    }

    public void NextGrid(int offset = 1)
    {
        int i;
        for (i = 0; i < allGrids.Length; i++)
        {
            if (Grid == allGrids[i].Item1)
            {
                break;
            }
        }
        i = (i + offset + allGrids.Length) % allGrids.Length;
        ResetGrid(allGrids[i].Item1, allGrids[i].Item2);
    }

    public void ResetGrid(IGrid grid, string name)
    {
        colorMap.Grid = grid;
        colorMap.Clear();
        blackCells.Clear();
        timesVisited.Clear();
        maxTimesVisited = 0;
        minVisited = maxVisited = Vector3.zero;
        gridText.text = name;
        colorMap.defaultColor = Color.white;
        Camera.main.transform.position = new Vector3(0, 0, -10);
        Camera.main.orthographicSize = 10.07f;
        // Pick the cell located at the origin
        grid.FindCell(new Vector3(0, 0, 0), out var startingCell);
        var startingDir = grid.GetCellDirs(startingCell).First();
        ant = new Walker(Grid, startingCell, startingDir);
    }

    private void Update()
    {
        var lastMaxTimeVisited = maxTimesVisited;
        spareTime += Time.deltaTime;
        var timePerStep = 1.0f / stepsPerSecond;
        while (spareTime > timePerStep)
        {
            spareTime -= timePerStep;

            // Do actual ant movement
            var cell = Grid.Move(ant.Cell, ant.Dir).Value;
            var isCurrentCellBlack = blackCells.Contains(cell);
            ant.MoveForward(isCurrentCellBlack);
            if (isCurrentCellBlack)
            {
                do
                {
                    ant.TurnRight();
                    // Turn again if there's no way forward
                } while (Grid.Move(ant.Cell, ant.Dir) == null);
                blackCells.Remove(cell);
            }
            else
            {
                do
                {
                    ant.TurnLeft();
                    // Turn again if there's no way forward
                } while (Grid.Move(ant.Cell, ant.Dir) == null);
                blackCells.Add(cell);
            }
            // Record some statistics about the movement
            timesVisited[ant.Cell] = timesVisited.GetValueOrDefault(ant.Cell) + 1;
            maxTimesVisited = Mathf.Max(timesVisited[ant.Cell], maxTimesVisited);
            var antPos = Grid.GetCellCenter(ant.Cell);
            minVisited = Vector3.Min(minVisited, antPos);
            maxVisited = Vector3.Max(maxVisited, antPos);
            UpdateCell(ant.Cell);
        }


        // Adjust arrow
        {
            var antPos = Grid.GetCellCenter(ant.Cell);
            var nextCell = Grid.Move(ant.Cell, ant.Dir).Value;
            var nextAntPosCell = Grid.GetCellCenter(nextCell);
            arrow.transform.position = Vector3.Lerp(antPos, nextAntPosCell, spareTime / timePerStep);
            arrow.transform.rotation = Quaternion.FromToRotation(Vector3.up, Grid.GetCellCenter(nextCell) - Grid.GetCellCenter(ant.Cell));
        }

        // Adjust camera
        var width = maxVisited - minVisited;
        var w = Mathf.Max(width.x, width.y);
        Camera.main.orthographicSize += (Camera.main.orthographicSize - w * zoomScale) * (1 - Mathf.Exp(zoomSpeed));
        var desiredPos = Grid.GetCellCenter(ant.Cell);
        var cameraPos = Camera.main.transform.position;
        Camera.main.transform.Translate(
            -(desiredPos.x - cameraPos.x) * (1 - Mathf.Exp(panSpeed)),
            -(desiredPos.y - cameraPos.y) * (1 - Mathf.Exp(panSpeed)),
            0);

        if (lastMaxTimeVisited != maxTimesVisited)
        {
            foreach (var cell in timesVisited.Keys)
            {
                UpdateCell(cell);
            }
        }
    }

    public void UpdateCell(Cell cell)
    {
        var visitedRatio = timesVisited.GetValueOrDefault(cell) / (float)maxTimesVisited;
        var color =  blackCells.Contains(cell)
            ? Color.black
            : Color.Lerp(Color.white, Color.red, visitedRatio);
        colorMap.SetColor(cell, color);
    }
}

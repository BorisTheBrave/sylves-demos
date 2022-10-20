using Sylves;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Langton : BaseGridRenderer
{
    public float stepsPerSecond = 60;
    public float zoomSpeed = 0.0001f;
    public float panSpeed = 0.0001f;

    HashSet<Cell> blackCells = new HashSet<Cell>();
    Dictionary<Cell, int> timesVisited = new Dictionary<Cell, int>();
    Cell minCellVisited;
    Cell maxCellVisited;
    int maxTimesVisited = 0;
    float spareTime = 0;
    Walker ant;

    public override void Start()
    {
        base.Start();
        //Grid = new SquareGrid(1);
        Grid = new HexGrid(1);
        ant = new Walker(Grid, new Cell(0, 0), (CellDir)SquareDir.Right);
    }

    private void Update()
    {
        spareTime += Time.deltaTime;
        var timePerStep = 1.0f / stepsPerSecond;
        while (spareTime > timePerStep)
        {
            spareTime -= timePerStep;

            // Do actual ant movement
            ant.MoveForward();
            var cell = ant.Cell;
            var isCurrentCellBlack = blackCells.Contains(cell);
            if (isCurrentCellBlack)
            {
                ant.TurnRight();
                blackCells.Remove(cell);
            }
            else
            {
                ant.TurnLeft();
                blackCells.Add(cell);
            }
            // Record some statistics about the movement
            timesVisited[ant.Cell] = timesVisited.GetValueOrDefault(ant.Cell) + 1;
            maxTimesVisited = Mathf.Max(timesVisited[ant.Cell], maxTimesVisited);
            minCellVisited = new Cell(Mathf.Min(minCellVisited.x, cell.x), Mathf.Min(minCellVisited.y, cell.y));
            maxCellVisited = new Cell(Mathf.Max(maxCellVisited.x, cell.x), Mathf.Max(maxCellVisited.y, cell.y));
            // Adjust camera
            Camera.main.orthographicSize *= Mathf.Exp(zoomSpeed);
            var antPos = Grid.GetCellCenter(ant.Cell);
            var cameraPos = Camera.main.transform.position;
            Camera.main.transform.Translate(
                -(antPos.x - cameraPos.x) * (1 - Mathf.Exp(panSpeed)),
                -(antPos.y - cameraPos.y) * (1 - Mathf.Exp(panSpeed)),
                0);
        }
    }

    protected override Color? CellColor(Cell cell)
    {
        var visitedRatio = timesVisited.GetValueOrDefault(cell) / (float)maxTimesVisited;
        return blackCells.Contains(cell) 
            ? Color.black
            : Color.Lerp(Color.white, Color.red, visitedRatio);
    }

}

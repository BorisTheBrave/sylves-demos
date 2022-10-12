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

    public override void Start()
    {
        base.Start();
        Grid = new MeshGrid(new MeshData(mesh));
    }

    private void Update()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var origin = transform.worldToLocalMatrix.MultiplyPoint3x4(ray.origin);
        var direction= transform.worldToLocalMatrix.MultiplyVector(ray.direction);
        var h = Grid.Raycast(origin, direction).Cast<RaycastInfo?>().FirstOrDefault();

        selectedCell = h?.cell;
    }

    protected override Color? CellColor(Cell cell)
    {
        return cell == selectedCell ? Color.red : null;
    }
}

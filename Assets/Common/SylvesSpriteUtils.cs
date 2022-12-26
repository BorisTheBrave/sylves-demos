using Sylves;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;

namespace Assets.Common
{
    public class SylvesSpriteUtils
    {
        public static GameObject CreateSpriteShape(IGrid grid, Cell cell, bool collider = true)
        {
            var go = new GameObject();
            var r = go.AddComponent<SpriteShapeRenderer>();
            var c = go.AddComponent<SpriteShapeController>();
            if (collider)
            {
                c.autoUpdateCollider = true;
                go.AddComponent<PolygonCollider2D>();
            }
            var spline = c.spline;
            spline.Clear();
            var polygon = grid.GetPolygon(cell);
            foreach(var p in polygon)
            {
                spline.InsertPointAt(0, p);
                spline.SetTangentMode(0, ShapeTangentMode.Linear);
            }
            c.RefreshSpriteShape();
            c.spriteShape = Resources.Load<SpriteShape>("SolidFillSpriteShapeProfile");
            c.splineDetail = 1;
            
            return go;
        }
    }
}

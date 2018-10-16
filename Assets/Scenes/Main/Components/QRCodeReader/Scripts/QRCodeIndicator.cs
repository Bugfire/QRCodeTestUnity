using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MainComponents
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class QRCodeIndicator : MonoBehaviour
    {
        [SerializeField]
        Color Color;

        [SerializeField]
        CanvasRenderer CanvasRenderer;

        Mesh Mesh;
        Vector3[] Vertices = new Vector3[4];
        int[] Indices = new int[6] { 0, 1, 2, 3, 2, 0 };
        ZXing.ResultPoint[] Points = null;

        bool IsDirty = true;

        int RemoveTimerMilliSec = 0; // -1 : ActiveMesh, 0 : NoMesh, 1~ : Count down to NoMesh

        void Start()
        {
            Canvas.willRenderCanvases += Canvas_willRenderCanvases;
            IsDirty = true;
            Mesh = new Mesh();
            Vertices[0] = Vertices[1] = Vertices[2] = Vertices[3] = Vector3.zero;
        }

        void Update()
        {
            if (RemoveTimerMilliSec > 0)
            {
                RemoveTimerMilliSec -= (int)(1000 * Time.deltaTime);
                if (RemoveTimerMilliSec <= 0)
                {
                    Vertices[0] = Vertices[1] = Vertices[2] = Vertices[3] = Vector3.zero;
                    IsDirty = true;
                    RemoveTimerMilliSec = 0;
                }
            }
        }

        void OnDestroy()
        {
            Canvas.willRenderCanvases -= Canvas_willRenderCanvases;
        }

        public void SetPoints(int width, int height, ZXing.ResultPoint[] points, bool verticallyMirrored)
        {
            if (Points == null && points == null)
            {
                return;
            }
            Points = points;
            if (Points != null && Points.Length >= 3)
            {
                var wh = width > height ? height : width;
                var xm = width / 2;
                var ym = height / 2;
                var m = (1.0f / wh) * 650f;
                var vm = verticallyMirrored ? -1 : 1;
#if false
                Vertices[0] = new Vector3((Points[0].X - xm) * m, -((Points[0].Y - ym) * m * vm));
                Vertices[1] = new Vector3((Points[1].X - xm) * m, -((Points[1].Y - ym) * m * vm));
                Vertices[2] = new Vector3((Points[2].X - xm) * m, -((Points[2].Y - ym) * m * vm));
                Vertices[3] = new Vector3((Points[3].X - xm) * m, -((Points[3].Y - ym) * m * vm));
#else
                var minx = float.MaxValue;
                var miny = float.MaxValue;
                var maxx = float.MinValue;
                var maxy = float.MinValue;
                for (var i = 0; i < Points.Length; i++) {
                    if (minx > Points[i].X)
                    {
                        minx = Points[i].X;
                    }
                    if (miny > Points[i].Y)
                    {
                        miny = Points[i].Y;
                    }
                    if (maxx < Points[i].X)
                    {
                        maxx = Points[i].X;
                    }
                    if (maxy < Points[i].Y)
                    {
                        maxy = Points[i].Y;
                    }
                }
                minx = (minx - xm) * m;
                maxx = (maxx - xm) * m;
                miny = -(miny - ym) * m * vm;
                maxy = -(maxy - ym) * m * vm;
                Vertices[0] = new Vector3(minx, maxy);
                Vertices[1] = new Vector3(minx, miny);
                Vertices[2] = new Vector3(maxx, miny);
                Vertices[3] = new Vector3(maxx, maxy);
#endif
                RemoveTimerMilliSec = -1;
                IsDirty = true;
            }
            else if (RemoveTimerMilliSec == -1)
            {
                RemoveTimerMilliSec = 500;
            }
        }

        void Canvas_willRenderCanvases()
        {
            if (!IsDirty || Mesh == null)
            {
                return;
            }
            IsDirty = false;
            Mesh.vertices = Vertices;
            Mesh.triangles = Indices;
            CanvasRenderer.SetMesh(Mesh);
            CanvasRenderer.SetColor(Color);
            CanvasRenderer.materialCount = 1;
            CanvasRenderer.SetMaterial(Canvas.GetDefaultCanvasMaterial(), 0);
            CanvasRenderer.SetTexture(Texture2D.whiteTexture);
            CanvasRenderer.SetAlpha(0.5f);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            CanvasRenderer = GetComponent<CanvasRenderer>();
            IsDirty = true;
            if (!Application.isPlaying)
            {
                Canvas_willRenderCanvases();
            }
        }
#endif
    }
}
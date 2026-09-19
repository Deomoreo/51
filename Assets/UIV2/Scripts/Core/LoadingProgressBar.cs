using UnityEngine;
using UnityEngine.UI;

namespace Project51.UIV2.Core
{
    // Draw rounded geometry at the actual bar size: no scaled artwork or distorted caps.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LoadingProgressBar : MaskableGraphic
    {
        public Image Source;
        private float shown = -1;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            Capsule(vh, r, new Color32(78,109,145,255), new Color32(78,109,145,255));
            r = new Rect(r.x+3,r.y+3,r.width-6,r.height-6);
            Capsule(vh, r, new Color32(13,28,46,255), new Color32(13,28,46,255));
            float width = r.width * Mathf.Clamp01(shown);
            if (width > 0) Capsule(vh, new Rect(r.x,r.y,width,r.height), new Color32(213,139,28,255), new Color32(255,218,112,255));
        }
        private static void Capsule(VertexHelper vh, Rect r, Color bottom, Color top)
        {
            float radius = Mathf.Min(r.height,r.width)*.5f;
            int first = vh.currentVertCount;
            vh.AddVert(r.center,Color.Lerp(bottom,top,.5f),Vector2.zero);
            for(int corner=0;corner<4;corner++)
                for(int step=0;step<=8;step++)
                {
                    float angle=(corner*90+step*90f/8)*Mathf.Deg2Rad;
                    var center=new Vector2(corner==0||corner==3?r.xMax-radius:r.xMin+radius,corner<2?r.yMax-radius:r.yMin+radius);
                    var point=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
                    vh.AddVert(point,Color.Lerp(bottom,top,Mathf.InverseLerp(r.yMin,r.yMax,point.y)),Vector2.zero);
                }
            for(int i=0;i<36;i++)vh.AddTriangle(first,first+1+i,first+1+(i+1)%36);
        }
        private void LateUpdate()
        {
            if(Source==null)return;
            Source.enabled=false;
            if(Mathf.Approximately(shown,Source.fillAmount))return;
            shown=Source.fillAmount;SetVerticesDirty();
        }
    }
}

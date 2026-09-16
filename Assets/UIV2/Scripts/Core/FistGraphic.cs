using UnityEngine;
using UnityEngine.UI;

namespace Project51.UIV2.Core
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FistGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            Shape(vh,r,new Rect(.08f,.05f,.84f,.90f),new Color32(163,92,39,255));
            Shape(vh,r,new Rect(.12f,.09f,.76f,.82f),new Color32(243,197,137,255));
            Shape(vh,r,new Rect(.08f,.34f,.24f,.35f),new Color32(163,92,39,255));
            Shape(vh,r,new Rect(.11f,.37f,.18f,.29f),new Color32(255,213,157,255));
            for(int i=0;i<4;i++)
            {
                Shape(vh,r,new Rect(.23f+i*.155f,.64f,.15f,.32f),new Color32(163,92,39,255));
                Shape(vh,r,new Rect(.247f+i*.155f,.665f,.115f,.29f),new Color32(255,219,166,255));
            }
        }
        private static void Shape(VertexHelper vh,Rect parent,Rect shape,Color c)
        {
            var r=new Rect(parent.x+shape.x*parent.width,parent.y+shape.y*parent.height,shape.width*parent.width,shape.height*parent.height);
            float radius=Mathf.Min(r.width,r.height)*.28f;int first=vh.currentVertCount;
            vh.AddVert(r.center,c,Vector2.zero);
            for(int corner=0;corner<4;corner++)for(int s=0;s<=6;s++)
            {
                float a=(90*corner+90*s/6f)*Mathf.Deg2Rad;
                var center=new Vector2(corner==0||corner==3?r.xMax-radius:r.xMin+radius,corner<2?r.yMax-radius:r.yMin+radius);
                vh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,c,Vector2.zero);
            }
            for(int i=0;i<28;i++)vh.AddTriangle(first,first+1+i,first+1+(i+1)%28);
        }
    }
}

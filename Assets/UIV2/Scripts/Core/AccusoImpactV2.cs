using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Project51.UIV2.Core
{
    public sealed class AccusoImpactV2 : MonoBehaviour
    {
        public CanvasGroup Group;
        public RectTransform Fist;
        public TMP_Text Caption;
        private Sequence animation;
        public void Play(string title, Transform[] cards)
        {
            animation?.Kill();Group.gameObject.SetActive(true);Group.alpha=1;Caption.text=title;
            Fist.localScale=Vector3.one*1.6f;
            animation=DOTween.Sequence().SetUpdate(true).Append(Fist.DOScale(1,.18f).SetEase(Ease.InCubic))
                .AppendCallback(()=>
                {
                    if(cards==null)return;
                    foreach(var card in cards)
                    {
                        if(card==null)continue;
                        // Add only a reversible scale pulse: gameplay owns card positions.
                        card.DOPunchScale(Vector3.one*.08f,.4f,3,.5f).SetLink(card.gameObject);
                    }
                }).Append(Fist.DOPunchRotation(new Vector3(0,0,8),.3f,5,.5f))
                .AppendInterval(1).Append(Group.DOFade(0,.25f)).OnComplete(()=>Group.gameObject.SetActive(false));
        }
        private void OnDestroy(){animation?.Kill();}
    }
}

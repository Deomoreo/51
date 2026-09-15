using System;

namespace Project51.UIV2.Animations
{
    public interface IUIV2Showable
    {
        void PlayShow(Action onComplete = null);
        void PlayHide(Action onComplete = null);
    }

    public interface IUIV2Selectable
    {
        void PlaySelected(bool selected);
    }

    public interface IUIV2Pressable
    {
        void PlayPress();
    }

    public interface IUIV2Unlockable
    {
        void PlayUnlock(Action onComplete = null);
    }

    public interface IUIV2Rewardable
    {
        void PlayReward(Action onComplete = null);
    }

    public interface IUIV2ProgressAnimatable
    {
        void PlayProgressChanged(float fromNormalized, float toNormalized);
    }
}

using NUnit.Framework;
using Project51.UI51;

/// <summary>Fase 1 UI51: logica pura delle fondamenta (fotogrammi emoticon, letter-spacing dei token).</summary>
public class UI51FoundationTests
{
    [Test]
    public void EmoticonFrame_StartsAtZero()
    {
        Assert.AreEqual(0, EmoticonPlayer.FrameAt(0f, 1f));
        Assert.AreEqual(0, EmoticonPlayer.FrameAt(-1f, 1f));
    }

    [Test]
    public void EmoticonFrame_ForwardLeg()
    {
        Assert.AreEqual(4, EmoticonPlayer.FrameAt(0.5f, 1f));
        Assert.AreEqual(7, EmoticonPlayer.FrameAt(0.9375f, 1f));
    }

    [Test]
    public void EmoticonFrame_BackwardLegPingPongs()
    {
        Assert.AreEqual(7, EmoticonPlayer.FrameAt(1.0625f, 1f));
        Assert.AreEqual(3, EmoticonPlayer.FrameAt(1.5f, 1f));
        Assert.AreEqual(4, EmoticonPlayer.FrameAt(2.5f, 1f));
    }

    [Test]
    public void EmoticonFrame_DegenerateInputsReturnZero()
    {
        Assert.AreEqual(0, EmoticonPlayer.FrameAt(0.5f, 1f, 1));
        Assert.AreEqual(0, EmoticonPlayer.FrameAt(0.5f, 0f));
    }

    [Test]
    public void LetterSpacing_PxToTmpEm()
    {
        Assert.AreEqual(13.333f, UI51Tokens.LetterSpacing(2f, 15f), 0.001f);
        Assert.AreEqual(0f, UI51Tokens.LetterSpacing(2f, 0f));
    }
}

using CowSay.Core.Models;
using Shouldly;
using static CowSay.Core.Models.SpeechBubble;

namespace Cowsay.Core.Tests;

public class SpeechBubbleModelTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var bubble = new SpeechBubble("Hello", BubbleType.Speech);

        bubble.Content.ShouldBe("Hello");
        bubble.Type.ShouldBe(BubbleType.Speech);
    }

    [Fact]
    public void BubbleType_Speech_HasExpectedValue()
    {
        BubbleType.Speech.ShouldBe((BubbleType)0);
    }

    [Fact]
    public void BubbleType_Thought_HasExpectedValue()
    {
        BubbleType.Thought.ShouldBe((BubbleType)1);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var b1 = new SpeechBubble("Hello", BubbleType.Speech);
        var b2 = new SpeechBubble("Hello", BubbleType.Speech);

        b1.ShouldBe(b2);
    }

    [Fact]
    public void Equality_DifferentType_AreNotEqual()
    {
        var speech = new SpeechBubble("Hello", BubbleType.Speech);
        var thought = new SpeechBubble("Hello", BubbleType.Thought);

        speech.ShouldNotBe(thought);
    }
}

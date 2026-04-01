using Cowsay.Application.DTOs;
using Cowsay.Application.Handlers;
using CowSay.Core.Interfaces;
using CowSay.Core.Models;
using Moq;
using Shouldly;

namespace Cowsay.Application.Tests;

public class GenerateCowSayTests
{
    private readonly Mock<ICowRepository> _repoMock = new();
    private readonly Mock<IBubbleService> _bubbleMock = new();
    private readonly Mock<ITemplateEngine> _templateMock = new();

    private GenerateCowSay CreateSut() => new(_repoMock.Object, _bubbleMock.Object, _templateMock.Object);

    [Fact]
    public void Execute_DefaultRequest_CallsRepoWithDefaultCowName()
    {
        var request = new CowRequest("Hello");
        _repoMock.Setup(r => r.GetCowByName("default")).Returns(new Cow("default", "template"));
        _bubbleMock.Setup(b => b.CreateBubble("Hello", false, 40, false)).Returns("bubble");
        _templateMock.Setup(t => t.Process("template", It.IsAny<Face>(), '\\')).Returns("cow");

        CreateSut().Execute(request);

        _repoMock.Verify(r => r.GetCowByName("default"), Times.Once);
    }

    [Fact]
    public void Execute_CustomCowName_PassedToRepo()
    {
        var request = new CowRequest("Hi", CowName: "tux");
        _repoMock.Setup(r => r.GetCowByName("tux")).Returns(new Cow("tux", "tpl"));
        _bubbleMock.Setup(b => b.CreateBubble(It.IsAny<string>(), false, 40, false)).Returns("b");
        _templateMock.Setup(t => t.Process(It.IsAny<string>(), It.IsAny<Face>(), It.IsAny<char>())).Returns("c");

        CreateSut().Execute(request);

        _repoMock.Verify(r => r.GetCowByName("tux"), Times.Once);
    }

    [Fact]
    public void Execute_SpeechMode_UsesBackslashThoughtChar()
    {
        var request = new CowRequest("Hi", IsThought: false);
        _repoMock.Setup(r => r.GetCowByName(It.IsAny<string>())).Returns(new Cow("default", "tpl"));
        _bubbleMock.Setup(b => b.CreateBubble(It.IsAny<string>(), false, 40, false)).Returns("b");
        _templateMock.Setup(t => t.Process(It.IsAny<string>(), It.IsAny<Face>(), '\\')).Returns("c");

        CreateSut().Execute(request);

        _templateMock.Verify(t => t.Process("tpl", It.IsAny<Face>(), '\\'), Times.Once);
    }

    [Fact]
    public void Execute_ThoughtMode_UsesOThoughtChar()
    {
        var request = new CowRequest("Hi", IsThought: true);
        _repoMock.Setup(r => r.GetCowByName(It.IsAny<string>())).Returns(new Cow("default", "tpl"));
        _bubbleMock.Setup(b => b.CreateBubble(It.IsAny<string>(), true, 40, false)).Returns("b");
        _templateMock.Setup(t => t.Process(It.IsAny<string>(), It.IsAny<Face>(), 'o')).Returns("c");

        CreateSut().Execute(request);

        _templateMock.Verify(t => t.Process("tpl", It.IsAny<Face>(), 'o'), Times.Once);
    }

    [Fact]
    public void Execute_CombinesBubbleAndCowArt()
    {
        var request = new CowRequest("Hi");
        _repoMock.Setup(r => r.GetCowByName(It.IsAny<string>())).Returns(new Cow("default", "tpl"));
        _bubbleMock.Setup(b => b.CreateBubble(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<bool>())).Returns("BUBBLE");
        _templateMock.Setup(t => t.Process(It.IsAny<string>(), It.IsAny<Face>(), It.IsAny<char>())).Returns("COWART");

        var result = CreateSut().Execute(request);

        result.ShouldBe("BUBBLE\nCOWART");
    }

    [Fact]
    public void Execute_PassesCorrectIsThoughtToBubbleService()
    {
        var request = new CowRequest("Hmm", IsThought: true);
        _repoMock.Setup(r => r.GetCowByName(It.IsAny<string>())).Returns(new Cow("default", "tpl"));
        _bubbleMock.Setup(b => b.CreateBubble("Hmm", true, 40, false)).Returns("b");
        _templateMock.Setup(t => t.Process(It.IsAny<string>(), It.IsAny<Face>(), It.IsAny<char>())).Returns("c");

        CreateSut().Execute(request);

        _bubbleMock.Verify(b => b.CreateBubble("Hmm", true, 40, false), Times.Once);
    }

    [Fact]
    public void Execute_CustomWrapWidth_PassedToBubbleService()
    {
        var request = new CowRequest("Hi", WrapWidth: 72);
        _repoMock.Setup(r => r.GetCowByName(It.IsAny<string>())).Returns(new Cow("default", "tpl"));
        _bubbleMock.Setup(b => b.CreateBubble("Hi", false, 72, false)).Returns("b");
        _templateMock.Setup(t => t.Process(It.IsAny<string>(), It.IsAny<Face>(), It.IsAny<char>())).Returns("c");

        CreateSut().Execute(request);

        _bubbleMock.Verify(b => b.CreateBubble("Hi", false, 72, false), Times.Once);
    }

    [Fact]
    public void Execute_NoWrapTrue_PassedToBubbleService()
    {
        var request = new CowRequest("Hi", NoWrap: true);
        _repoMock.Setup(r => r.GetCowByName(It.IsAny<string>())).Returns(new Cow("default", "tpl"));
        _bubbleMock.Setup(b => b.CreateBubble("Hi", false, 40, true)).Returns("b");
        _templateMock.Setup(t => t.Process(It.IsAny<string>(), It.IsAny<Face>(), It.IsAny<char>())).Returns("c");

        CreateSut().Execute(request);

        _bubbleMock.Verify(b => b.CreateBubble("Hi", false, 40, true), Times.Once);
    }

    [Fact]
    public void Execute_RepoThrows_PropagatesException()
    {
        var request = new CowRequest("Hi", CowName: "nonexistent");
        _repoMock.Setup(r => r.GetCowByName("nonexistent")).Throws(new FileNotFoundException("not found"));

        Should.Throw<FileNotFoundException>(() => CreateSut().Execute(request));
    }

    [Fact]
    public void Execute_DeadMode_CreatesFaceWithDeadEyes()
    {
        var request = new CowRequest("Hi", Mode: CowMode.Dead);
        _repoMock.Setup(r => r.GetCowByName(It.IsAny<string>())).Returns(new Cow("default", "tpl"));
        _bubbleMock.Setup(b => b.CreateBubble(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<bool>())).Returns("b");
        _templateMock.Setup(t => t.Process(It.IsAny<string>(), It.Is<Face>(f => f.Eyes == "xx" && f.Tongue == "U "), It.IsAny<char>())).Returns("c");

        CreateSut().Execute(request);

        _templateMock.Verify(t => t.Process("tpl", It.Is<Face>(f => f.Eyes == "xx" && f.Tongue == "U "), '\\'), Times.Once);
    }

    [Fact]
    public void Execute_EmptyMessage_Succeeds()
    {
        var request = new CowRequest("");
        _repoMock.Setup(r => r.GetCowByName(It.IsAny<string>())).Returns(new Cow("default", "tpl"));
        _bubbleMock.Setup(b => b.CreateBubble(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<bool>())).Returns("b");
        _templateMock.Setup(t => t.Process(It.IsAny<string>(), It.IsAny<Face>(), It.IsAny<char>())).Returns("c");

        var result = CreateSut().Execute(request);

        result.ShouldNotBeNullOrEmpty();
    }
}

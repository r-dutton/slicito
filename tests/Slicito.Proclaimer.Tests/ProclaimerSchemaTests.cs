using FluentAssertions;
using Slicito.Common;

namespace Slicito.Proclaimer.Tests;

[TestClass]
public class ProclaimerSchemaTests
{
    private TypeSystem? _typeSystem;
    private ProclaimerTypes? _types;

    [TestInitialize]
    public void Initialize()
    {
        _typeSystem = new TypeSystem();
        _types = new ProclaimerTypes(_typeSystem);
    }

    [TestMethod]
    public void ProclaimerTypes_Should_Initialize_All_Element_Types()
    {
        _types.Should().NotBeNull();

        _types!.Service.Should().NotBeNull();
        _types.Endpoint.Should().NotBeNull();
        _types.HttpClient.Should().NotBeNull();
        _types.Repository.Should().NotBeNull();
        _types.Database.Should().NotBeNull();
        _types.Queue.Should().NotBeNull();
        _types.Topic.Should().NotBeNull();
        _types.BackgroundService.Should().NotBeNull();
    }

    [TestMethod]
    public void ProclaimerTypes_Should_Initialize_All_Link_Types()
    {
        _types.Should().NotBeNull();

        _types!.Contains.Should().NotBeNull();
        _types.BelongsToService.Should().NotBeNull();
        _types.Calls.Should().NotBeNull();
        _types.SendsHttpRequest.Should().NotBeNull();
        _types.WritesTo.Should().NotBeNull();
        _types.ReadsFrom.Should().NotBeNull();
        _types.PublishesTo.Should().NotBeNull();
        _types.ConsumesFrom.Should().NotBeNull();
    }

    [TestMethod]
    public void ProclaimerSchema_Should_Create_Valid_Schema()
    {
        var schema = ProclaimerSchema.AddProclaimerSchema(_types!);

        schema.Should().NotBeNull();
        schema.ElementTypes.Should().NotBeEmpty();
        schema.LinkTypes.Should().NotBeEmpty();
        schema.ElementAttributes.Should().NotBeEmpty();
        schema.RootElementTypes.Should().NotBeEmpty();
    }

    [TestMethod]
    public void ProclaimerSchema_Should_Have_Expected_Root_Elements()
    {
        var schema = ProclaimerSchema.AddProclaimerSchema(_types!);

        schema.RootElementTypes.Should().Contain(_types!.Endpoint);
        schema.RootElementTypes.Should().Contain(_types.BackgroundService);
    }

    [TestMethod]
    public void ProclaimerTypes_Should_Implement_IProgramTypes()
    {
        var programTypes = _types as Slicito.ProgramAnalysis.IProgramTypes;

        programTypes.Should().NotBeNull();
        programTypes!.Procedure.Should().NotBeNull();
        programTypes.HasName(_types!.Endpoint).Should().BeTrue();
        programTypes.HasCodeLocation(_types.Endpoint).Should().BeTrue();
    }

    [TestMethod]
    public void Element_Types_Should_Be_Distinct()
    {
        var service = _types!.Service;
        var endpoint = _types.Endpoint;
        var httpClient = _types.HttpClient;

        service.Should().NotBe(endpoint);
        service.Should().NotBe(httpClient);
        endpoint.Should().NotBe(httpClient);
    }

    [TestMethod]
    public void Link_Types_Should_Be_Distinct()
    {
        var calls = _types!.Calls;
        var sendsHttpRequest = _types.SendsHttpRequest;
        var belongsToService = _types.BelongsToService;

        calls.Should().NotBe(sendsHttpRequest);
        calls.Should().NotBe(belongsToService);
        sendsHttpRequest.Should().NotBe(belongsToService);
    }
}

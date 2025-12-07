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
        // Assert
        _types.Should().NotBeNull();
        
        // Verify endpoint types
        _types!.EndpointController.Should().NotBeNull();
        _types.EndpointMinimalApi.Should().NotBeNull();
        _types.ControllerAction.Should().NotBeNull();
        _types.ControllerResponse.Should().NotBeNull();
        
        // Verify communication types
        _types.HttpClient.Should().NotBeNull();
        
        // Verify CQRS types
        _types.CqrsRequest.Should().NotBeNull();
        _types.CqrsHandler.Should().NotBeNull();
        _types.MediatrSend.Should().NotBeNull();
        _types.MediatrPublish.Should().NotBeNull();
        _types.NotificationHandler.Should().NotBeNull();
        
        // Verify data access types
        _types.EfDbContext.Should().NotBeNull();
        _types.EfEntity.Should().NotBeNull();
        _types.DbTable.Should().NotBeNull();
        _types.Repository.Should().NotBeNull();
        
        // Verify messaging types
        _types.MessagePublisher.Should().NotBeNull();
        _types.MessageContract.Should().NotBeNull();
        
        // Verify application types
        _types.AppService.Should().NotBeNull();
        _types.BackgroundService.Should().NotBeNull();
    }

    [TestMethod]
    public void ProclaimerTypes_Should_Initialize_All_Link_Types()
    {
        // Assert
        _types.Should().NotBeNull();
        
        _types!.Contains.Should().NotBeNull();
        _types.Calls.Should().NotBeNull();
        _types.SendsRequest.Should().NotBeNull();
        _types.HandledBy.Should().NotBeNull();
        _types.ProcessedBy.Should().NotBeNull();
        _types.MapsTo.Should().NotBeNull();
        _types.UsesClient.Should().NotBeNull();
        _types.UsesService.Should().NotBeNull();
        _types.UsesStorage.Should().NotBeNull();
        _types.Publishes.Should().NotBeNull();
        _types.Queries.Should().NotBeNull();
    }

    [TestMethod]
    public void ProclaimerSchema_Should_Create_Valid_Schema()
    {
        // Arrange & Act
        var schema = ProclaimerSchema.AddProclaimerSchema(_types!);

        // Assert
        schema.Should().NotBeNull();
        schema.ElementTypes.Should().NotBeEmpty();
        schema.LinkTypes.Should().NotBeEmpty();
        schema.ElementAttributes.Should().NotBeEmpty();
        schema.RootElementTypes.Should().NotBeEmpty();
    }

    [TestMethod]
    public void ProclaimerSchema_Should_Have_Expected_Root_Elements()
    {
        // Arrange & Act
        var schema = ProclaimerSchema.AddProclaimerSchema(_types!);

        // Assert
        schema.RootElementTypes.Should().Contain(_types!.EndpointController);
        schema.RootElementTypes.Should().Contain(_types.EndpointMinimalApi);
        schema.RootElementTypes.Should().Contain(_types.BackgroundService);
    }

    [TestMethod]
    public void ProclaimerTypes_Should_Implement_IProgramTypes()
    {
        // Arrange
        var programTypes = _types as Slicito.ProgramAnalysis.IProgramTypes;

        // Assert
        programTypes.Should().NotBeNull();
        programTypes!.Procedure.Should().NotBeNull();
        programTypes.HasName(_types!.EndpointController).Should().BeTrue();
        programTypes.HasCodeLocation(_types.ControllerAction).Should().BeTrue();
    }

    [TestMethod]
    public void Element_Types_Should_Be_Distinct()
    {
        // Arrange & Act
        var endpointController = _types!.EndpointController;
        var endpointMinimalApi = _types.EndpointMinimalApi;
        var httpClient = _types.HttpClient;

        // Assert - types should be distinct (not equal)
        endpointController.Should().NotBe(endpointMinimalApi);
        endpointController.Should().NotBe(httpClient);
        endpointMinimalApi.Should().NotBe(httpClient);
    }

    [TestMethod]
    public void Link_Types_Should_Be_Distinct()
    {
        // Arrange & Act
        var calls = _types!.Calls;
        var sendsRequest = _types.SendsRequest;
        var publishes = _types.Publishes;

        // Assert - types should be distinct (not equal)
        calls.Should().NotBe(sendsRequest);
        calls.Should().NotBe(publishes);
        sendsRequest.Should().NotBe(publishes);
    }
}

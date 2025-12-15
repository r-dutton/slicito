using Microsoft.AspNetCore.Http;

namespace MinimalWebApi;

public static class MinimalHandlers
{
    public static IResult GetMinimalWidget(int id) => Results.Ok($"minimal-{id}");

    public static IResult CreateMinimalWidget(WidgetInput input) => Results.Created($"/api/minimal/widgets/{input.Id}", input);
}

public record WidgetInput(int Id, string Name);

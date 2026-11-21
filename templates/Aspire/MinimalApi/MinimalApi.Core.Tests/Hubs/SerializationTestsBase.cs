using System.Text.Json;

namespace MinimalApi.Core.Tests.Hubs;

public abstract class SerializationTestsBase<TDto>
    where TDto : class, new()
{
    protected abstract TDto CreateTestDto();

    protected abstract Task AssertAreEqualAsync(TDto expected, TDto actual);

    [Test]
    public async Task SerializeToJson_ValidDto_ReturnsJson()
    {
        TDto dto = CreateTestDto();

        var json = JsonSerializer.Serialize(dto);

        await Assert.That(json).IsNotNull();
        await Assert.That(json).IsNotEmpty();
    }

    [Test]
    public async Task DeserializeFromJson_ValidJson_ReturnsDto()
    {
        TDto dto = CreateTestDto();
        var json = JsonSerializer.Serialize(dto);
        var deserializedDto = JsonSerializer.Deserialize<TDto>(json);

        await Assert.That(deserializedDto).IsNotNull();
        await AssertAreEqualAsync(dto, deserializedDto!);
    }
}



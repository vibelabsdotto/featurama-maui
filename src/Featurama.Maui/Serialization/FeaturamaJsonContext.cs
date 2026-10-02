using System.Text.Json.Serialization;
using Featurama.Maui.Models;

namespace Featurama.Maui.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(FeatureRequest))]
[JsonSerializable(typeof(PaginatedResponse<FeatureRequest>))]
[JsonSerializable(typeof(CreateFeatureRequestInput))]
[JsonSerializable(typeof(UpdateFeatureRequestInput))]
[JsonSerializable(typeof(VoteRequestBody))]
[JsonSerializable(typeof(ProjectConfig))]
[JsonSerializable(typeof(BrandingConfig))]
[JsonSerializable(typeof(Comment))]
[JsonSerializable(typeof(List<Comment>))]
[JsonSerializable(typeof(AddCommentInput))]
[JsonSerializable(typeof(Models.DeviceInfo))]
internal partial class FeaturamaJsonContext : JsonSerializerContext;

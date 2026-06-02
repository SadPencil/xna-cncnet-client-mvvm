#nullable enable
using ClientCore.Caching;

using SixLabors.ImageSharp;

namespace AvClientViewModel.Domain.Multiplayer;

public interface IMapPreviewCacheManager : ICacheManager<Map, Image> { }
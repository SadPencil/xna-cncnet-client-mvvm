#nullable enable
using ClientCore.Caching;

using SixLabors.ImageSharp;

namespace AvMainClientViewModel.Domain.Multiplayer;

public interface IMapPreviewCacheManager : ICacheManager<Map, Image> { }
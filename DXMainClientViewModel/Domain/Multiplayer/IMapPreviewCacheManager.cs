#nullable enable
using ClientCore.Caching;

using SixLabors.ImageSharp;

namespace DXMainClientViewModel.Domain.Multiplayer;

public interface IMapPreviewCacheManager : ICacheManager<Map, Image> { }
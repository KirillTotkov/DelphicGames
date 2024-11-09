using DelphicGames.Data.Models;
using Stream = DelphicGames.Models.Stream;

namespace DelphicGames.Services.Streaming;

public interface IStreamProcessor : IDisposable
{
    Stream StartStreamForPlatform(CameraPlatform cameraPlatform);
    void StopStreamForPlatform(Stream stream);
}

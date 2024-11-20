using DelphicGames.Data.Models;
using DelphicGames.Models;

namespace DelphicGames.Services.Streaming;

public interface IStreamProcessor : IDisposable
{
    Task<StreamInfo> StartStreamForPlatform(StreamEntity streamEntity);
    void StopStreamForPlatform(StreamInfo streamInfo);
}

using DelphicGames.Data.Models;
using DelphicGames.Models;

namespace DelphicGames.Services.Streaming;

public interface IStreamProcessor : IDisposable
{
    StreamInfo StartStreamForPlatform(Data.Models.StreamEntity streamEntity);
    void StopStreamForPlatform(StreamInfo streamInfo);
}

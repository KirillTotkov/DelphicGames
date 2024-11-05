using System;
using System.Runtime.Serialization;

namespace DelphicGames.Services.Streaming
{
    [Serializable]
    public class FfmpegProcessException : Exception
    {
        public int CameraId { get; }

        public FfmpegProcessException()
        {
        }

        public FfmpegProcessException(string message) : base(message)
        {
        }

        public FfmpegProcessException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public FfmpegProcessException(string message, int cameraId) : base(message)
        {
            CameraId = cameraId;
        }

        protected FfmpegProcessException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            if (info != null)
            {
                CameraId = info.GetInt32(nameof(CameraId));
            }
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info != null)
            {
                info.AddValue(nameof(CameraId), CameraId);
            }
            base.GetObjectData(info, context);
        }
    }
}
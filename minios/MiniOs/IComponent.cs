using System;
using Microsoft.SPOT;

namespace MiniOS
{
    internal interface IComponent : IDisposable
    {
        void StartInstance(params object[] parameters);
        DeviceComponentType StartOnComponentReady { get; }
        bool isRunning { get; set; }
    }

    internal enum DeviceComponentType
    {
        Network = 1,
        USB = 2,
        SDCard = 4,
        SPI = 8
    }
}

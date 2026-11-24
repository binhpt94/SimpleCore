using System;

namespace Game.Services.Popup
{
    /// <summary>The sample popup of feature-popup/cheat: its only button, "Đóng", asks to close it.</summary>
    internal interface IDevSampleView
    {
        event Action CloseClicked;
    }
}

namespace Game.Services.Popup
{
    /// <summary>Where refused requests are reported. Separated so tests can read the messages.</summary>
    internal interface IPopupLog
    {
        void Warning(string message);

        void Error(string message);
    }
}

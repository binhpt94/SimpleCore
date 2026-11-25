using System;

namespace Game.Home
{
    /// <summary>
    /// The start screen (feature-home/home-screen), shown and hidden by the screen flow. It only reports what the
    /// player pressed; it never opens another screen itself.
    /// </summary>
    public interface IHomeScreen
    {
        /// <summary>Shows the start screen with the current record; ready for one Play tap.</summary>
        void Show();

        void Hide();

        /// <summary>The player tapped Play (once per showing).</summary>
        event Action PlayPressed;
    }
}

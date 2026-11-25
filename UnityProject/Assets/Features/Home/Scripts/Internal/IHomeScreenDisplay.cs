using System;
using UnityEngine;

namespace Game.Home
{
    /// <summary>The texts the start screen shows.</summary>
    internal readonly struct HomeScreenContent
    {
        public HomeScreenContent(string title, string record, string playLabel)
        {
            Title = title;
            Record = record;
            PlayLabel = playLabel;
        }

        public string Title { get; }
        public string Record { get; }
        public string PlayLabel { get; }
    }

    /// <summary>The start screen on screen. Shows what it is given and reports a press on Play; decides nothing.</summary>
    internal interface IHomeScreenDisplay : IDisposable
    {
        event Action PlayPressed;

        /// <summary>The Unity object the view lives on, so the session can count it as held.</summary>
        UnityEngine.Object Root { get; }

        void Show(HomeScreenContent content);

        void Hide();
    }

    /// <summary>Builds the start screen's view from its layout.</summary>
    internal interface IHomeScreenFactory
    {
        IHomeScreenDisplay Create(HomeLayout layout);
    }

    /// <summary>Asset form of <see cref="IHomeScreenFactory"/>, so the installer can hold the View's factory.</summary>
    internal abstract class HomeScreenFactoryAsset : ScriptableObject, IHomeScreenFactory
    {
        public abstract IHomeScreenDisplay Create(HomeLayout layout);
    }
}

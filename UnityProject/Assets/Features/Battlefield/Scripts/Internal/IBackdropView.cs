using System.Collections.Generic;

namespace Game.Battlefield
{
    /// <summary>Draws the backdrop pieces it is given. Nothing on it blocks touches or movement.</summary>
    internal interface IBackdropView
    {
        void Show(IReadOnlyList<BackdropPiece> pieces);
    }

    /// <summary>Creates and removes the backdrop view (Unity objects), so the participant can be tested without them.</summary>
    internal interface IBackdropViewFactory
    {
        IBackdropView Create();

        void Destroy(IBackdropView view);
    }
}

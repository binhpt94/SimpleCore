using UnityEngine;

namespace Game.SceneFlow
{
    /// <summary>Holds the Game scene's gate where the Loading scene can find it. Holds nothing else.</summary>
    internal sealed class SceneGateCarrier : MonoBehaviour, ISceneGateCarrier
    {
        public SceneGate Gate { get; private set; }

        void ISceneGateCarrier.Carry(SceneGate gate)
        {
            Gate = gate;
        }
    }
}

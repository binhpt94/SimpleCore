namespace Game.Core
{
    /// <summary>
    /// Where installers publish their contract implementations and look up the contracts they need.
    /// Used only while booting; game code keeps the references it resolved.
    /// </summary>
    public interface IServiceRegistry
    {
        /// <summary>Publishes <paramref name="service"/> as contract <typeparamref name="T"/>. One implementation per contract.</summary>
        void Register<T>(T service) where T : class;

        /// <summary>Returns the implementation of <typeparamref name="T"/>; throws when the owning feature was not installed earlier.</summary>
        T Resolve<T>() where T : class;

        bool TryResolve<T>(out T service) where T : class;
    }
}

namespace Game.CheatPanel
{
    /// <summary>One cheat state the running build can build: the owning feature's title, the state's key and title, and the scene that builds it.</summary>
    internal readonly struct CheatEntry
    {
        public CheatEntry(string featureTitle, string key, string title, string scene)
        {
            FeatureTitle = featureTitle;
            Key = key;
            Title = title;
            Scene = scene;
        }

        public string FeatureTitle { get; }
        public string Key { get; }
        public string Title { get; }
        public string Scene { get; }
    }
}

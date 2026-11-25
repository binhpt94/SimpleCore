namespace Game.Ads
{
    /// <summary>The dev fake provider's stand-in for an ad: a plain screen shown for a couple of seconds.</summary>
    internal interface IFakeAdScreen
    {
        void Show(FakeAdOutcome outcome);
        void Hide();
    }
}

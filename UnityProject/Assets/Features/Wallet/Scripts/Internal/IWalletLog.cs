namespace Game.Wallet
{
    /// <summary>Where the wallet writes requests it refused. Behind an interface so tests can read what was written.</summary>
    internal interface IWalletLog
    {
        void Refused(string message);
    }
}

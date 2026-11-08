namespace GameUI.Signals
{
    public sealed class StartWindowDismissedSignal
    {
        public StartWindowDismissedSignal(bool doNotShowAgain)
        {
            DoNotShowAgain = doNotShowAgain;
        }

        public bool DoNotShowAgain { get; }
    }
}

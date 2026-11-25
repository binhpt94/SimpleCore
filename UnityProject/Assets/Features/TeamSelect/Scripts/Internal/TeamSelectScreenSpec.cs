namespace Game.TeamSelect
{
    /// <summary>
    /// Fixed values of the team select screen (design: feature-team-select/team-slots/slots-spec, Thông số; frame main).
    /// Layout numbers are px on the 1920x1080 reference screen, top-left origin.
    /// </summary>
    internal static class TeamSelectScreenSpec
    {
        /// <summary>Buttons on the screen: Enter battle and Back.</summary>
        public const int ButtonCount = 2;

        /// <summary>Smallest side of the Back button (touch size of HUD and popups).</summary>
        public const float BackMinSide = 120f;

        /// <summary>The build error line the screen flow shows (feature-game-flow/flow-routes/routes-values).</summary>
        public const string BuildErrorText = "Không vào được trận, thử lại";

        /// <summary>Effective Enter battle taps per opening.</summary>
        public const int StartTapsPerOpening = 1;

        // frame main.frame
        public const float BackX = 40f, BackY = 40f, BackW = 240f, BackH = 120f;
        public const float TitleX = 560f, TitleY = 40f, TitleW = 800f, TitleH = 120f;
        public const float CardsX = 130f, CardsY = 224f, CardW = 300f, CardH = 400f, CardGap = 40f;
        public const float SlotsX = 324f, SlotsY = 688f, Slot = 240f, SlotGap = 24f;
        public const float ErrorX = 1156f, ErrorY = 688f, ErrorW = 440f, ErrorH = 80f;
        public const float StartX = 1156f, StartY = 792f, StartW = 440f, StartH = 136f;
    }
}

namespace Game.PopupPause
{
    /// <summary>The two steps of the one pause popup.</summary>
    internal enum PausePopupStep
    {
        /// <summary>Title "Tạm dừng", buttons "Tiếp tục" (main) and "Bỏ lượt".</summary>
        Main,

        /// <summary>Question "Bỏ lượt này? Điểm tính theo số đợt đã dọn.", buttons "Bỏ lượt" and "Không" (main).</summary>
        ConfirmQuit,
    }
}

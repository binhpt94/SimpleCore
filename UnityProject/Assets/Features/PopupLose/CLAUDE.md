# PopupLose
Tầng: 6 · Features

Popup hiện khi thành bị phá: điểm lượt, kỉ lục, dòng "Kỉ lục mới!", nút "Chơi lại" và "Về màn bắt đầu". Thiết kế: `live/feature-popup-lose` (`lose-result/result-spec`, `lose-actions/actions-spec`, cheat ở `feature-popup-lose/cheat`).

## Cách chạy
- `PopupLoseInstaller` (`FeatureAssets/Public/PopupLoseInstaller.asset`) cài sau Popup và Run; cần `ISessionScene`, `IRunFlow`, `IRunScore`, `IPopupService`, `IPopupViews`, thiếu thì lỗi nêu tên.

## Cheat (chỉ `UNITY_EDITOR || DEVELOPMENT_BUILD`)
- Installer hiện thực `ICheatStateSource` của Core (partial `Scripts/Internal/Cheat/PopupLoseInstaller.Cheat.cs`): `CheatFeatureTitle` = "Popup thua"; một state `PopupLose/NewRecord` ("Popup thua đang mở, lượt vừa phá kỉ lục"), theo thứ tự ý của node cheat.
- State `NewRecord` (`LoseCheat.cs`): mở popup thua ngay khi phiên chạy, điểm 16, kỉ lục 16, cờ kỉ lục mới bật.
- Đầu `Install` lấy `ICheatSelection` của Core từ registry; thiếu thì lỗi nêu tên `ICheatSelection`. `HasPick` thì bỏ trường `_cheat` trên asset, chỉ dựng khi `PickedKey` là `PopupLose/NewRecord` (pick của feature khác hay chơi bình thường: không dựng gì); chưa chọn thì theo `_cheat`. Mỗi lần dựng state gọi `ReportRunning(key)`.
- Asset dùng chung phải để `_cheat` = `None` (có test kiểm).

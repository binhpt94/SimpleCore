# PopupRating
Tầng: 6 · Features
Popup mời đánh giá game trên Google Play: chồng lên popup thua 1.5 giây sau khi trận dừng, chỉ khi lượt vừa phá kỉ lục, mỗi lần mở game nhiều nhất một lần.

## Contracts
Không có. Feature không mở kiểu công khai nào.

## Nối dây
- `PopupRatingInstaller` (FeatureAssets/Public): cài sau Popup và Run. Cần `ISessionScene`, `IRunFlow`, `IRunScore`, `IPopupService`, `IPopupViews`, và config `FeatureAssets/Private/PopupRatingConfig` (bảng InviteParams, TimingParams).
- Popup id `PopupRating/Main` → prefab `FeatureAssets/Public/PopupRatingMain.prefab` (gốc có `RatingView`), nằm trong catalog popup của scene Game ở `_Composition`.
- Số lần đã mời là field của `RatingParticipant`, sống cùng scene Game, tức suốt một lần mở game (chơi lại không tải lại scene). Chỉ trong bộ nhớ, không lưu xuống máy.

## Hành vi
- Đếm `invite_delay` (1.5 giây thật, không tính lúc ở ngoài game) từ `IRunFlow.BattleStopped`; `BattleCleared` huỷ.
- Hết giờ: `IRunScore.IsNewBest` bật và chưa đủ `max_invites_per_launch` thì xin mở popup; đã hiện mới tính là đã mời. Dịch vụ từ chối thì ghi cảnh báo.
- Chạm đầu tiên mới tính. "Đánh giá": đóng popup, rồi mở ứng dụng Google Play theo `Application.identifier`, không được thì trình duyệt, không được nữa thì chỉ ghi lỗi. "Để sau" và nút quay lại (khi popup ở trên cùng): đóng popup.
- Cheat `invite-open`: chọn trên asset installer, chỉ bản dev, mở popup ngay khi phiên chạy.

## Cheat list (chỉ bản dev)
- `PopupRatingInstaller` hiện thực `ICheatStateSource` của Core (`Scripts/Internal/Cheat/PopupRatingInstaller.Cheat.cs`): `CheatFeatureTitle` = "Popup mời đánh giá"; `CheatStates` theo thứ tự ý của node `feature-popup-rating/cheat`: `PopupRating/InviteOpen` "Popup mời đánh giá đang mở, chưa chạm nút nào".
- Install cần `ICheatSelection` của Core (Boot đăng ký), thiếu thì lỗi ngay đầu Install. Đã có lựa chọn (`HasPick`) thì bỏ enum cheat trên asset, chỉ dựng state khi `PickedKey` là của PopupRating; chưa có thì dựng theo asset. Dựng state nào cũng gọi `ReportRunning` với key đó.

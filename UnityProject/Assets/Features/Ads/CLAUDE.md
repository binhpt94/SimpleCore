# Ads
Tầng: 6 · Features

Nơi duy nhất trong game hiện quảng cáo có thưởng: feature khác nhờ hiện một quảng cáo cho một đề nghị và nhận lại đúng một kết quả, đã xem đủ hay không xem được. Ads không biết thưởng là gì.

## Contracts

### IRewardedAds
Đường duy nhất để hiện quảng cáo; bên gọi không nói chuyện với nhà cung cấp. Một yêu cầu một lúc, không hàng chờ.
- `Show(offerId, onReply)`: xin một quảng cáo cho đề nghị `offerId`. `onReply` được gọi đúng một lần với `AdReply` mang lại `offerId`, trừ khi yêu cầu bị huỷ trước.
  - Đang có yêu cầu khác chạy: trả `NotWatched` ngay trong khung hình đó (log lí do "đang bận"); yêu cầu đang chạy không bị ảnh hưởng.
  - Còn lại: chờ mở (tối đa 8 giây), phát (không tính hạn), chờ xác nhận thưởng sau khi đóng (tối đa 5 giây), rồi trả kết quả ở khung hình đầu tiên game đã trở lại màn hình (chờ game trở lại không có hạn).
  - Xác nhận thưởng tới trước khi đóng thì vẫn là `Watched`, trả sau khi đóng.
  - Hết phiên scene (rời scene, tắt game) khi đang chờ: không có kết quả cho ai.
- `Cancel(offerId)`: bên gọi bỏ đề nghị (rời trận, đề nghị đã xử lí). Kết quả tới sau bị bỏ, chỉ ghi log. `offerId` không phải yêu cầu đang chạy thì không làm gì.
- Hạn chờ nằm trong `AdsConfig.asset` (`FeatureAssets/Private`).
- Dùng: PopupRevive (nút Xem của đề nghị hồi sinh; tự kiểm `OfferId` còn là đề nghị đang chờ, rồi chuyển kết quả cho Run).

### AdReply
Struct chỉ đọc, kết quả của một yêu cầu.
- `OfferId`: mã đề nghị bên gọi đã gửi trong `Show`; bên gọi dùng nó để bỏ kết quả của đề nghị cũ.
- `Result`: `AdResult` của yêu cầu.
- Dùng: PopupRevive (nhận qua `onReply`).

### AdResult
Enum, chỉ hai kết quả; lí do lỗi chỉ ghi log, bên gọi không phân biệt.
- `Watched`: nhà cung cấp xác nhận đã xem đủ để nhận thưởng.
- `NotWatched`: hết quảng cáo, mất mạng, đóng sớm, quá hạn chờ, hoặc đang bận.
- Dùng: PopupRevive.

### IAdsCheat
Chỉ bản dev (`#if UNITY_EDITOR || DEVELOPMENT_BUILD`), điều khiển nhà cung cấp giả để dựng state cheat. Không có ở bản phát hành.
- `HoldNextAd()`: yêu cầu kế tiếp đứng ở "đang phát" mãi, không trả kết quả, không hạn, không hiện màn quảng cáo giả (state chờ của bên gọi vẫn thấy), tới khi bị `Cancel` hay hết phiên scene. Các yêu cầu sau đó chạy như thường.
- Dùng: feature dựng state "popup hồi sinh đang chờ quảng cáo" (PopupRevive).

## Bên trong
- `AdRequests` (C# thuần, hiện thực `IRewardedAds`): máy bốn bước chờ mở, đang phát, chờ xác nhận, có kết quả; chạy qua `Tick(deltaGiâyThật, gameOnScreen)`.
- `IAdProvider`: nhà cung cấp. Bản dev: `FakeAdProvider` (kết quả chọn trước trong `AdsConfig`: `Watched`, `NotWatched`, `TimedOut`; màn chờ 2 giây; cũng hiện thực `IAdsCheat`). Bản phát hành: `NoAdProvider`, luôn `NotWatched` (chưa có nhà cung cấp thật).
- Mọi `NotWatched` ghi lí do qua `IAdsLog` (`AdFailReason`).
- `AdsSessionParticipant`: tạo view khi phiên chạy (nhịp khung hình `IAdsFrames`, màn quảng cáo giả `IFakeAdScreen`); hết phiên thì bỏ yêu cầu đang chạy rồi huỷ view. Yêu cầu đang chờ tính là một `TrackJob`.
- `AdsInstaller.asset` trong `FeatureAssets/Public`. Cài sau SessionScene. Thiếu config hay prefab view thì lỗi ngay đầu `Install`.

Test: assembly `Game.Ads.Tests` (EditMode).

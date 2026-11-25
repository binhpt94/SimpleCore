# Hud

HUD trận: nút tạm dừng, máu thành, tiền, số đợt, thông báo "Đợt N", ba nút kĩ năng thành. Chỉ hiện, không giữ hay tính số nào của feature khác.

- Không có Contracts. Không ai gọi Hud.
- Dùng: `ICastle`, `ICastleSkills` (Castle), `IWalletBalance` (Wallet), `IWaveProgress` (Wave), `IRunFlow` (Run), `Game.Core.ISessionScene`. Bản dev dùng thêm `IWalletIncome`, `IWalletService`, `ICastleCheat`, `IWaveCheat` cho cheat.
- `HudSession` (lớp con của `SessionParticipantBase`): tạo view trong `Run`, chỉ hiện ở các bước trận (`Playing`, `Paused`, `Ending`, `Lost`), huỷ khi phiên kết thúc.
- `HudStatusPresenter`: máu (`"hiện tại/tối đa"`, làm tròn xuống), tiền, "Đợt N"; thông báo đợt hiện khi `WaveStarted`, tự tắt sau `bannerSeconds`, đứng yên khi tạm dừng.
- `HudSkillPresenter`: mỗi khung hình đọc `GetState`; số giây làm tròn lên, lớp phủ theo tỉ lệ còn lại, nảy một lần khi hồi xong. Chạm chỉ gọi `TryUse` khi `Step == Playing` và kĩ năng sẵn sàng.
- `HudSkillSlots`: cụm 3 ô cố định góc dưới phải (x 1420..1880, y 920..1060): Sấm sét ô 0 (x 1420), Băng giá ô 1 (x 1580), Cổ vũ ô 2 (x 1740). Nút nằm ở ô của kĩ năng nó; kĩ năng trận không có thì ô trống, không có nút khoá. Khay luôn rộng 460 px. Sprite nút chọn theo ô.
- `HudText`: hàm thuần đổi số ra chữ. Đồng hồ khung hình qua `IHudClock` (`UnityHudClock`, vòng UniTask).
- View: `HudView` tự dựng canvas theo frame `feature-hud/hud-layout/layout-spec` (màn chuẩn 1920x1080, mỗi phần bám mép hoặc góc của nó). Chữ dùng font dựng sẵn `LegacyRuntime.ttf` để có dấu tiếng Việt. `sortingOrder` 10, popup phải nằm cao hơn.
- Số cân bằng: `HudConfig.asset` trong `FeatureAssets/Private` (thông báo 1.5 giây, nảy 1.2 lần trong 0.3 giây, `showPauseButton` tắt ở IDEA).
- Prefab view: `HudView.prefab` trong `FeatureAssets/Private`.
- Sprite (IDEA, @ClaudeImage, chấm mức 3): `FeatureAssets/Private/Generated/` gồm `hud-band/band` (nền dải trên), `hud-hp/hp` (khung thanh máu, phần đỏ nằm lọt trong viền 6 px), `hud-coin/coin`, `skill-tray/tray` (khay dưới cụm kĩ năng), `skill-buttons/skill-thunder|frost|rally`. Band, hp, tray là 9-slice viền 24 px. Gán trên prefab; ô nào trống thì về ô màu tạm. Nút đang hồi tô xám bằng màu, không cần sprite riêng.
- `HudInstaller.asset` trong `FeatureAssets/Public`. Cài sau SessionScene, Run, Castle, Wallet, Wave.
- Cheat `mid-battle` (`Scripts/Internal/Cheat`, chỉ bản dev): nút tím trong dải trên, giữa thanh máu và ô đợt (x 1290..1550, y 15..65), không che dải chiến trường. Thứ tự: `IWaveCheat.StartWave(7)`, `ICastleCheat.SetHealth(300)`, hồi chiêu Sấm sét 0, Băng giá 12, Cổ vũ 0, rồi tiền 245 qua Wallet. Kĩ năng không có trong `ICastleSkills.Skills` thì bỏ qua và ghi cảnh báo "không có ở mức này".
- `HudPausePresenter`: nút tạm dừng (góc trên trái, x 40..160, y 10..130, hai vạch đứng, không chữ; có `pauseSprite` trên prefab thì dùng sprite). Chạm chỉ gọi `IRunFlow.Pause()` khi `Step == Playing`, không tạm dừng và thành chưa bị phá (chạm cùng khung hình thành bị phá thì bỏ). Nút sống từ `Run` tới hết phiên, hiện cùng HUD ở mọi bước trận. Bật tắt bằng cờ `showPauseButton` trong `HudConfig.asset` (IDEA: tắt, không có nút và không nhận chạm; PROTOTYPE tuỳ chọn; FULL bật).
- Vùng an toàn: mọi phần nằm trong khung `Safe` (con của canvas). `HudView.Update` đọc `Screen.safeArea`, `HudSafeArea.Insets` đổi ra đơn vị màn chuẩn và chỉ dời phần vượt lề sẵn có (trái phải 40, trên 10, dưới 20).
- Thanh nâng cấp: mép dưới bên trái cụm kĩ năng luôn trống, ít nhất 1420 đơn vị ở 16:9; cụm kĩ năng không bao giờ nhường chỗ.

Test: assembly `Game.Hud.Tests` (EditMode).

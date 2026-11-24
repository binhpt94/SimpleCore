# Hud

HUD trận: máu thành, tiền, số đợt, thông báo "Đợt N", ba nút kĩ năng thành. Chỉ hiện, không giữ hay tính số nào của feature khác.

- Không có Contracts. Không ai gọi Hud.
- Dùng: `ICastle`, `ICastleSkills` (Castle), `IWalletBalance` (Wallet), `IWaveProgress` (Wave), `IRunFlow` (Run), `Game.Core.ISessionScene`. Bản dev dùng thêm `IWalletIncome`, `IWalletService` cho cheat.
- `HudSession` (lớp con của `SessionParticipantBase`): tạo view trong `Run`, chỉ hiện ở các bước trận (`Playing`, `Paused`, `Ending`, `Lost`), huỷ khi phiên kết thúc.
- `HudStatusPresenter`: máu (`"hiện tại/tối đa"`, làm tròn xuống), tiền, "Đợt N"; thông báo đợt hiện khi `WaveStarted`, tự tắt sau `bannerSeconds`, đứng yên khi tạm dừng.
- `HudSkillPresenter`: mỗi khung hình đọc `GetState`; số giây làm tròn lên, lớp phủ theo tỉ lệ còn lại, nảy một lần khi hồi xong. Chạm chỉ gọi `TryUse` khi `Step == Playing` và kĩ năng sẵn sàng.
- `HudText`: hàm thuần đổi số ra chữ. Đồng hồ khung hình qua `IHudClock` (`UnityHudClock`, vòng UniTask).
- View: `HudView` tự dựng canvas theo frame `feature-hud/hud-layout/layout-spec` (màn chuẩn 1920x1080, mỗi phần bám mép hoặc góc của nó). Chữ dùng font dựng sẵn `LegacyRuntime.ttf` để có dấu tiếng Việt. `sortingOrder` 10, popup phải nằm cao hơn.
- Số cân bằng: `HudConfig.asset` trong `FeatureAssets/Private` (thông báo 1.5 giây, nảy 1.2 lần trong 0.3 giây).
- Prefab view: `HudView.prefab` trong `FeatureAssets/Private`.
- `HudInstaller.asset` trong `FeatureAssets/Public`. Cài sau SessionScene, Run, Castle, Wallet, Wave.
- Cheat `mid-battle` (`Scripts/Internal/Cheat`, chỉ bản dev): nút tím dưới góc trên trái. Hiện chỉ ép được tiền 245 qua Wallet; máu thành 300/1000, đợt 7, Băng giá 12/35 giây chờ Castle và Wave có đường ép.
- Nút tạm dừng: chưa làm (không thuộc IDEA).

Test: assembly `Game.Hud.Tests` (EditMode).

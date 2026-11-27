# CheatPanel
Tầng: 6 · Features

Nút cheat nổi trên mọi scene, chạm vào mở panel cheat để người test nhảy ngay tới state cheat của các feature; một config `cheat_enabled` bật hoặc tắt cả nút lẫn panel, ở Editor và mọi loại bản build.

## Contracts
Không có contract công khai. Feature khác không gọi CheatPanel.

## Dùng
- `ISessionScene` (Core): khi `cheat_enabled` bật, installer join `CheatOverlayParticipant`; overlay (nút, nền che, panel) tạo trong `Run`, huỷ khi phiên scene kết thúc.
- Sẽ dùng (đợt sau): `ICheatSelection`, `ICheatStateCatalog` của Core (chỉ bản dev), `ISceneSwitch` của SceneFlow (mọi build).

## Config, công tắc (node `feature-cheat-panel/cheat-access/access-spec`)
- `CheatPanelConfig.asset` trong `FeatureAssets/Private`, bảng `AccessParams` cột IDEA: `cheat_enabled` = bật, `button_label` = "Cheat", `panel_title` = "Cheat", `normal_row_label` = "Chơi bình thường", `empty_label` = "Bản này không có state cheat".
- Chỉ `cheat_enabled` quyết có nút hay không. Đường code này không có `#if UNITY_EDITOR || DEVELOPMENT_BUILD` hay kiểm loại build.
- Tắt: installer trả về ngay, không join phiên, không resolve gì, không cần prefab, không tạo nút, panel hay nền che.
- Thiếu config: `Install` ném lỗi nêu `CheatPanelConfig`. Bật mà thiếu prefab overlay: lỗi nêu `overlayPrefab`. Bật mà thiếu `ISessionScene`: lỗi nêu tên đó.

## Nối dây
- `CheatPanelInstaller.asset` trong `FeatureAssets/Public`, cài sau SessionScene, ở mọi scene.
- Prefab overlay (nút, panel) làm ở đợt sau. Tới lúc đó, config bật mà chưa gán `overlayPrefab` thì `Install` báo lỗi.

## Logic
- `CheatPanelInstaller`: chỉ nối dây theo config.
- `CheatOverlayParticipant` (`SessionParticipantBase`): tạo overlay qua `ICheatOverlaySpawner` trong `Run`, `TrackView`, huỷ khi dispose.
- `PrefabCheatOverlaySpawner`: tạo và huỷ prefab overlay.

Test: assembly `Game.CheatPanel.Tests` (EditMode), `CheatAccessTests`.

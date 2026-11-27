# CheatPanel
Tầng: 6 · Features

Nút cheat nổi trên mọi scene, chạm vào mở panel cheat để người test nhảy ngay tới state cheat của các feature; một config `cheat_enabled` bật hoặc tắt cả nút lẫn panel, ở Editor và mọi loại bản build.

## Contracts
Không có contract công khai. Feature khác không gọi CheatPanel.

## Dùng
- `ISessionScene` (Core): khi `cheat_enabled` bật, installer join `CheatOverlayParticipant`; overlay (nút, nền che, panel) tạo trong `Run`, huỷ khi phiên scene kết thúc (đổi scene thì panel đóng, scene mới có nút với panel đóng).
- `ISceneSwitch` (SceneFlow, mọi build): `Reload(scene)` khi chọn dòng state, `Restart()` khi chọn "Chơi bình thường", `IsSwitching` khoá mọi dòng. SceneFlow cài cuối scene, nên installer chỉ giữ lối tìm nó và tìm khi phiên chạy; overlay có view mà không có nó thì lỗi nêu `ISceneSwitch`.
- Bản dev: `ICheatStateCatalog` (dòng state, chỉ state bản build dựng được) và `ICheatSelection` (chọn, state đang chạy; chỉ trong bộ nhớ) của Core, đọc trong `Cheat/CheatPanelInstaller.Cheat.cs`. Bản phát hành: `NoCheatStates`, chỉ có dòng "Chơi bình thường" và nhãn không có state.

## Config, công tắc (node `feature-cheat-panel/cheat-access/access-spec`)
- `CheatPanelConfig.asset` trong `FeatureAssets/Private`, bảng `AccessParams` cột IDEA: `cheat_enabled` = bật, `button_label` = "Cheat", `panel_title` = "Cheat", `normal_row_label` = "Chơi bình thường", `empty_label` = "Bản này không có state cheat".
- Chỉ `cheat_enabled` quyết có nút hay không. Đường code này không có `#if UNITY_EDITOR || DEVELOPMENT_BUILD` hay kiểm loại build.
- Tắt: installer trả về ngay, không join phiên, không resolve gì, không cần prefab, không tạo nút, panel hay nền che.
- Thiếu config: `Install` ném lỗi nêu `CheatPanelConfig`. Bật mà thiếu prefab overlay: lỗi nêu `overlayPrefab`. Bật mà thiếu `ISessionScene`: lỗi nêu tên đó.

## Dòng và áp state (node `feature-cheat-panel/cheat-states/states-rules`)
- `CheatRows.Build`: "Chơi bình thường" đầu, rồi state theo tên feature A–Z, trong feature theo thứ tự của nó; chữ "<tên feature>: <tiêu đề state>".
- `CheatPanelPresenter`: nút đóng/mở; chạm dòng thì đóng panel, `Pick(key)` rồi `Reload(scene)` (cả khi là state đang chạy); "Chơi bình thường" thì `Pick(null)` rồi `Restart()`; đang chuyển scene thì dòng khoá, chạm không làm gì. Đánh dấu dòng của `RunningKey`, không có thì dòng đầu. Chạm nền che không làm gì.
- Không ghi gì xuống máy: chọn nằm ở `ICheatSelection`; Save tự bỏ ghi khi có `RunningKey`.

## Cheat (node `feature-cheat-panel/cheat`, bản dev)
- `_cheat` trên installer: `PanelOpen` thì panel mở ngay khi scene chạy, báo `CheatPanel/PanelOpen` đang chạy. Bỏ qua khi đã có lựa chọn trong lần chạy game. Không phải nguồn của catalog (một asset dùng chung mọi scene).

## Nối dây
- `CheatPanelInstaller.asset` trong `FeatureAssets/Public`, trong `Boot.Loading` và `Boot.Game`, ngay trước SceneFlow.
- `overlayPrefab` = `FeatureAssets/Private/CheatOverlay.prefab`: một object mang `CheatOverlayView`, view dựng canvas (sortingOrder 30000, trên popup 1000), nút, nền che, panel, danh sách cuộn hai cột theo frame `wide` bằng code. Prefab thiếu view thì lỗi lúc Install.

## Logic
- `CheatPanelInstaller`: chỉ nối dây theo config.
- `CheatOverlayParticipant` (`SessionParticipantBase`): tạo overlay qua `ICheatOverlaySpawner` trong `Run`, gắn `CheatPanelPresenter` vào `ICheatPanelDisplay` của overlay, huỷ khi dispose.
- `PrefabCheatOverlaySpawner`: tạo và huỷ prefab overlay.

Test: assembly `Game.CheatPanel.Tests` (EditMode), `CheatAccessTests`, `CheatPanelTests`.

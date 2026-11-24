# Luật Unity mà validator không kiểm được
Validator ép ranh giới. File này ghi phần còn lại; người duyệt sẽ soi theo từng mục.

## Khung
- Khung do @CodeLead viết và sửa; phiên sở hữu chỉ đọc (guard ép): `Scripts/Contracts`, `Scripts/Internal/Base`, `Scripts/View/Base`, mọi `*.asmdef`, `AssemblyInfo.cs`; ở Core thêm các interface `I*.cs`. `Assets/_Composition` (nối dây) là của @CodeLead cả vùng. Bảng đầy đủ ở `.claude/rules/codelead.md`.
- Code mới của feature là lớp con của lớp base, đặt ở phần còn lại của `Internal`/`View`. Thiếu chỗ móc trong lớp base thì nhờ @CodeLead thêm, không chép lớp base ra sửa.

## Contract
- Chưa có feature khác cần thì để trong Internal. Không mở contract "phòng khi cần".
- Contract chỉ gồm interface, enum, struct dữ liệu chỉ đọc. Không có logic, không có trạng thái sửa được.
- Tên và tham số nói bằng từ của feature mình, không nhắc tới feature gọi nó.
- Contract đã có feature khác dùng: không đổi nghĩa, không đổi chữ ký. Cần khác thì đề xuất.

## Internal và View
- Luật game nằm trong lớp C# thuần ở Internal, chạy được trong test không cần scene.
- Việc phải đụng Unity (tạo, huỷ object, thời gian, lưu file) đặt sau một interface trong Internal để test thay bằng đồ giả.
- View chỉ hiển thị và chuyển thao tác người chơi xuống Internal. Không tính toán, không quyết định kết quả.
- View không biết lớp logic: một presenter trong Internal đẩy dữ liệu xuống view qua interface internal (ví dụ `ICoinsDisplay`).
- Không static giữ trạng thái, không singleton, kể cả trong nội bộ feature. View không tự đi tìm logic.
- Feature có view hoặc cần tải thì tham gia `ISessionScene`: tải trong `LoadAsync`, tạo view trong `Run`, huỷ trong `DisposeAsync`. Không đặt sẵn view trong scene.
- Số liệu cân bằng (giá, số xu ban đầu...) nằm trong asset cấu hình ở `FeatureAssets/Private`, không viết cứng trong code.
- Cheat: dựng đúng các state của node `<feature>/cheat` trong thiết kế (`gd_pack.py` in mục CHEAT), để test và Play mà không phải chơi tới nơi. Code cheat nằm trong thư mục của chính feature và chỉ chạy ở bản dev (`#if UNITY_EDITOR || DEVELOPMENT_BUILD`); không lọt vào bản phát hành. State gộp nhiều feature do feature chủ trận dựng, gọi feature khác qua Contracts.

## Nối giữa các feature
- Lấy dịch vụ của feature khác chỉ qua `IServiceRegistry`, và chỉ trong `Install`. Không static, không singleton dùng chung.
- Installer chỉ nối dây: tạo lớp logic, đăng ký contract. Không chứa luật game. Trong `Install` chỉ tạo object Unity khi đang Play.
- Thiếu phụ thuộc, config hay prefab thì lỗi ngay đầu `Install`, thông báo nêu tên thứ bị thiếu. Không âm thầm bỏ qua.
- Mỗi scene một variant của `Boot.prefab` (`Boot.Loading`, `Boot.Game`) giữ danh sách installer và catalog asset (id → asset) của scene đó; scene không override. Thứ tự cài: SessionScene đầu, bên SceneFlow của scene cuối.
- Cần dùng chung thứ gì với feature khác: đề xuất. Không chép code sang, không đẩy vào `Core`.
- `Core` không chứa luật game và không biết tên feature nào.

## Test
- Mỗi hành vi người chơi thấy được có ít nhất một test EditMode; tên test nói hành vi, không nói tên hàm.
- Feature khác được thay bằng đồ giả viết trong chính file test. Không test bằng Internal thật của feature khác.
- Sửa lỗi thì thêm test tái hiện lỗi trước.
- Mỗi asset installer có test nạp asset thật và gọi `Install` trong EditMode.

## Asset
- Không đổi GUID trong `.meta` đã có. Xoá hay đổi tên asset ở `FeatureAssets/Public` là đổi contract: thống nhất với @CodeLead trước.
- Asset giao cho dịch vụ (ví dụ prefab popup) là của feature gọi, đặt ở `FeatureAssets/Public` của nó, và đi qua id `<Owner>/<Name>` (ví dụ `Shop/Purchased`), không qua object Unity. Id khai báo một lần, `static readonly` (giá trị bất biến, không phải trạng thái), trong Internal của chủ; validator SRC-05 kiểm.

## unity-mcp (guard ép theo tác động của lệnh)
| Loại | Lệnh | Ai |
|---|---|---|
| Đọc | `read_console get`, `find_gameobjects`, `manage_scene get_*`, `manage_asset search/get_info/get_components`, `manage_prefabs get_info/get_hierarchy`, `manage_material get_material_info`, `manage_ui read/get_visual_tree`, `find_in_file`, `get_sha`, `validate_script`, docs, resource | mọi phiên code; path phải đọc được, `search` phải có `path` |
| Ghi asset | `manage_asset create/create_folder/duplicate/move/rename/modify/import/delete`, `manage_prefabs modify_contents`, `manage_material create/set_*`, `manage_texture`, `manage_ui create/update/create_panel_settings` | phiên sở hữu, mọi path trong vùng mình, ngoài khung; không `.cs`, `.asmdef`. @CodeLead: trong `Assets/_Composition` |
| Biên dịch | `refresh_unity` | mọi phiên code, khi không chat khác giữ Editor |
| Scene, prefab stage | `manage_scene create/load/save`, `manage_gameobject`, `manage_components`, `manage_prefabs create_from_gameobject`, `manage_material` gán cho renderer, `manage_ui attach_ui_document`, `manage_editor *_prefab_stage`, `read_console clear` | phiên code, scene/prefab trong vùng mình (@CodeLead: lưu chỉ trong `Assets/_Composition`, vùng khác chỉ mở xem); giữ khoá Editor |
| Play, test, cài đặt | `manage_editor play/pause/stop`, `run_tests`, `execute_menu_item`, tag/layer, package, camera, graphics, physics, ProBuilder, profiler | chỉ @CodeLead; giữ khoá Editor |
| Viết code | `create_script`, `script_apply_edits`, `apply_text_edits`, `delete_script`, `manage_script` | không ai: dùng Edit/Write |
- **Khoá Editor**: một chat một lúc, guard giữ cho chat đó tới hết lượt (Stop hook trả khoá) hoặc 10 phút không dùng. Chat khác đang giữ thì chờ; không tìm đường vòng. Người gỡ bằng `python .claude/hooks/stop_unlock.py --release`.
- Sửa object trong scene chỉ sau khi đã mở (`load`) hoặc tạo (`create`) scene của vùng mình trong lượt đó; guard chỉ cho lưu đúng scene đó. Trước khi kết thúc lượt: lưu scene hoặc prefab stage đã mở, không để scene dở dang.
- Prefab của feature thì sửa headless (`manage_prefabs modify_contents`), không cần scene và không cần khoá.
- Biên dịch xong thì đọc console (`read_console`), sửa hết lỗi của mình rồi mới báo xong. Lỗi của feature khác thì báo @CodeLead, không sửa hộ.

## Không lách
- Không viết code để né regex của validator (tách chuỗi, gọi gián tiếp API cấm).
- Validator báo sai thì dừng và báo người; sửa `rules.json` chỉ sau khi hỏi và được người đồng ý, từng lần.

---
paths:
  - "UnityProject/**"
---
# Luật Unity mà validator không kiểm được
Validator ép ranh giới. File này ghi phần còn lại; người duyệt sẽ soi theo từng mục.

## Khung
- Khung do @CodeLead viết và sửa; phiên sở hữu chỉ đọc (guard ép): `Scripts/Contracts`, `Scripts/Internal/Base`, `Scripts/View/Base`, mọi `*.asmdef`, `AssemblyInfo.cs`; ở Core thêm các interface `I*.cs` và lớp base `*Base.cs`. `Assets/_Composition` (nối dây) là của @CodeLead cả vùng. Bảng đầy đủ ở `.claude/rules/codelead.md`.
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
- Feature có view hoặc cần tải thì tham gia `ISessionScene` (hợp đồng vòng đời phiên nằm ở `Game.Core`; lớp tham gia kế thừa `SessionParticipantBase` của Core, không tự viết lại): tải trong `LoadAsync`, tạo view trong `Run`, huỷ trong `DisposeAsync`. Không đặt sẵn view trong scene.
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

## Phiếu Editor, kiểm biên dịch (người quyết 2026-11-27)
Unity biên dịch chung cả project: một lỗi chặn mọi bên. Worker viết code trong worktree riêng, qua cổng kiểm thì `worker.py` nạp vào `Assets`; Unity chỉ biên dịch theo phiếu, lần lượt.
- **Kiểm biên dịch ngoài Unity:** `python Management/tools/compile_check.py <Tên> | --all` (vài giây, chạy song song được, chạy cả trong worktree). `LỖI`: lỗi của assembly đó; `CHẶN`: một phụ thuộc đang lỗi; `BỎ`: defineConstraints không đạt. Là một cổng của `worker.py`.
- **Phiếu Editor:** `editor_ticket.py add|done|drop|release --session <Tên>`, `list`. Mã 32 bit: 10 bit đầu dùng chung (`refresh` nạp code mới, `clean` project sạch lỗi, `test` đã chạy test), 22 bit cho việc riêng. Mỗi phiên một phiếu (mỗi việc worker một phiếu); đến trước làm trước; chỉ phiếu "sửa lỗi biên dịch" của CodeLead (server tạo) đứng đầu.
  - Mọi phiếu chỉ-code (worker đã nạp code) được gộp thành một lượt: một phiên Haiku biên dịch và đọc console. Lỗi thì các việc worker trong lượt tự sửa một lần; lượt có phiếu khác thì CodeLead sửa trước.
  - Phiếu có việc riêng (asset, prefab, scene của CodeLead; đợt chơi thử) được cấp lần lượt; hết lượt là trả Editor (Stop hook), phần chưa làm xếp lại.
  - Khi không còn việc cần Editor: một lô test EditMode cho các assembly bị đụng và phụ thuộc của chúng; test của yêu cầu qua hết thì server ghi `done`; test hỏng thì việc worker đó tự sửa một lần.
- **Editor đang cấp cho bên khác hay người đang dùng:** guard chặn lệnh cần Editor (biên dịch, ghi asset, scene, test). Nộp phiếu, làm việc khác; không BÁO CHẶN. Editor trống và không ai chờ thì cứ dùng.
- **Người dùng Editor:** bấm Play (khi không chat nào giữ Editor), hoặc menu `SimpleCore/Tôi đang dùng Editor` (tự hết sau 30 phút), ghi `~/.coresimple-guard/human-editor.json`: không ai được cấp Editor cho tới khi Stop hay tắt menu. Script ở `Assets/SimpleCoreTools/Editor` (chỉ người sửa).

## unity-mcp (guard ép theo tác động của lệnh)
**Mọi vai đã gán dùng unity-mcp** (người quyết 2026-11-26), kể cả @Manager, @GameDesigner, @UIDesigner, @DesignAdvisor: theo bảng dưới, luật vùng và khoá Editor. Guard không kiểm `unity_instance`; không cần ghi nó. Phiên chưa gán FEATURE thì bị chặn.

| Loại | Lệnh | Ai |
|---|---|---|
| Đọc | `read_console get`, `find_gameobjects`, `manage_scene get_*`, `manage_asset search/get_info/get_components`, `manage_prefabs get_info/get_hierarchy`, `manage_material get_material_info`, `manage_ui read/get_visual_tree`, `find_in_file`, `get_sha`, `validate_script`, docs, resource | mọi phiên code; path phải đọc được, `search` phải có `path` |
| Ghi asset | `manage_asset create/create_folder/duplicate/move/rename/modify/import/delete`, `manage_prefabs modify_contents`, `manage_scriptable_object create/modify` (asset config, installer; `create`: `folder_path` + `asset_name`; `modify`: `target` chỉ là `{"path": "Assets/..."}`, không guid), `manage_material create/set_*`, `manage_texture`, `manage_ui create/update/create_panel_settings` | phiên sở hữu, mọi path trong vùng mình, ngoài khung; không `.cs`, `.asmdef`. @CodeLead: trong `Assets/_Composition`; `delete` thêm khung và mọi thứ trong `Assets/Features` |
| Biên dịch | `refresh_unity` | mọi phiên code, khi Editor trống hoặc đang giữ phiếu Editor |
| Scene, prefab stage | `manage_scene create/load/save`, `manage_gameobject`, `manage_components`, `manage_prefabs create_from_gameobject`, `manage_material` gán cho renderer, `manage_ui attach_ui_document`, `manage_editor *_prefab_stage`, `read_console clear` | phiên code, scene/prefab trong vùng mình (@CodeLead: lưu chỉ trong `Assets/_Composition`, vùng khác chỉ mở xem); giữ khoá Editor |
| Play, test, cài đặt | `manage_editor play/pause/stop`, `run_tests`, `execute_menu_item`, tag/layer, package, camera, graphics, physics, ProBuilder, profiler | chỉ @CodeLead; giữ khoá Editor |
| Chơi thử | đọc, `manage_editor play/pause/stop`, `run_tests`, `manage_scene load` (không save), `refresh_unity`, `execute_menu_item`, `manage_camera`, `execute_code` | nhóm Testers (@UnityAutoPlay): không ghi, không sửa gì; giữ khoá Editor; `execute_code` chỉ nhóm này chạy được (cổng mềm, luật ở `.claude/rules/tester.md`) |
| Viết code | `create_script`, `script_apply_edits`, `apply_text_edits`, `delete_script`, `manage_script` | không ai: dùng Edit/Write |
- **Khoá Editor**: một chat một lúc, guard giữ cho chat đó tới hết lượt (Stop hook trả khoá) hoặc 10 phút không dùng. Chat khác đang giữ thì chờ; không tìm đường vòng. Người gỡ bằng `python .claude/hooks/stop_unlock.py --release`.
- **Tester đang chơi thử** (khoá Editor ghi chủ là @UnityAutoPlay): guard từ chối mọi Edit/Write và mọi lệnh unity-mcp ghi, xoá asset vào `UnityProject/Assets` của phiên khác, vì file mới làm Unity nạp lại giữa lúc Play và đợt chơi thử mất số đo. Bị chặn thì dừng, ghi trong báo cáo phần còn lại, không tìm đường vòng; tester xong thì server tự gửi một tin để làm tiếp. Trong lúc đó hộp thư cũng giữ lại tin việc code gửi feature, service, @Core. Phiên `@human` không bị chặn.
- Sửa object trong scene chỉ sau khi đã mở (`load`) hoặc tạo (`create`) scene của vùng mình trong lượt đó; guard chỉ cho lưu đúng scene đó. Trước khi kết thúc lượt: lưu scene hoặc prefab stage đã mở, không để scene dở dang.
- Prefab của feature thì sửa headless (`manage_prefabs modify_contents`), không cần scene và không cần khoá.
- Biên dịch xong thì đọc console (`read_console`), sửa hết lỗi của mình rồi mới báo xong. Lỗi biên dịch của feature khác thì báo @CodeLead (CodeLead tự sửa, người quyết 2026-11-27), không sửa hộ.
- Ai đang giữ khoá Editor, tới mấy giờ, scene nào: mọi phiên Read được `~/.coresimple-guard/editor-lock.json` (chỉ file đó). Không có file là không ai giữ.
- Chụp màn hình (`manage_camera screenshot`) phải ghi `output_folder: "Captures"` (ngoài `Assets`, không commit). Thêm `include_image: true` để thấy ảnh. Guard chặn khi thiếu hoặc trỏ chỗ khác.
- Xoá file: chỉ file trong vùng của mình, qua unity-mcp (`manage_asset`, action `delete`, path bắt đầu bằng `Assets/`).
- Xoá thư mục: xoá hết file trong đó trước. Thư mục rỗng thì xoá được, trừ thư mục khung (`Scripts`, `Tests`, `FeatureAssets` và tầng ngay dưới).
- Biên dịch: mọi phiên code. Test, Play: @human, @CodeLead hoặc nhóm Testers.

## Không lách
- Không viết code để né regex của validator (tách chuỗi, gọi gián tiếp API cấm).
- Validator báo sai thì dừng và báo @CodeLead; sửa `rules.json`: @CodeLead hỏi người qua Manager (`ask.py`), từng lần.

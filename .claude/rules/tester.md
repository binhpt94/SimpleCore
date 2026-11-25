---
paths:
  - "Tester/**"
---
# Nhóm Testers: chơi thử game trong Unity và ghi lại kết quả
Nhóm: Master (@human) · Leaders (@Manager, @CodeLead, @GameDesigner, @UIDesigner) · Tools · **Testers (@UnityAutoPlay)** · Basic (@Core) · Services · Features.
Tester chơi game trong Unity Editor như một người chơi, đối chiếu với thiết kế đã chốt, rồi ghi lại điều đã thấy. Tester **không sửa gì trong project**: không code, không asset, không scene, không cài đặt. Thấy lỗi thì ghi lại và báo; phiên sở hữu tự sửa.

## Quyền (guard ép, trừ phần ghi "cổng mềm")
| Việc | Được |
|---|---|
| Đọc | toàn bộ `UnityProject/Assets` (cả `Internal`, `View`, `FeatureAssets/Private` của mọi feature), `Packages`, `ProjectSettings`; `Management` như mọi phiên (thiết kế `live`, studio, báo cáo); toàn bộ `Tester/`, kể cả `Archive` |
| Ghi bằng Edit/Write | chỉ trong `Tester/<tên mình>/`: `README.md`; trong một đợt `<yyyymmdd-hhmmss>/`: `README.md`, ghi chú `.md` khác, và `images/**` (ảnh, `.json`, `.txt`, `.csv`, `.log`) |
| Không ghi | mọi thứ khác: `UnityProject`, `Management`, `.claude`, `Resources`, `Tester/<tên>/Archive` (chỉ lệnh `archive` dời vào), thư mục của tester khác |
| unity-mcp | đọc (console, hierarchy, component, asset); `manage_editor` play/pause/stop; `run_tests`; `manage_scene load` (mở scene để chơi; **không** save, không create); `refresh_unity`; `execute_menu_item` (menu cheat của feature); `manage_camera` (chụp màn hình, `output_folder: "Captures"`); `execute_code` |
| unity-mcp bị cấm | ghi hay sửa asset, object, component, prefab, material; lưu scene; package, tag/layer, cài đặt project; mọi lệnh viết code |
| Shell | `tester.py` (bảng dưới, cả `shot` để dời ảnh), `gd_pack.py <Tên>`, `gd_frame.py live/<node>`, `validate.py`, `studio.py status` |

- **Khoá Editor**: play, test, load scene, menu, camera, `execute_code` đều giữ khoá Editor tới hết lượt (cùng khoá với @CodeLead). Phiên khác đang giữ thì chờ, không tìm đường vòng. Trước khi hết lượt: Stop Play.
- **`execute_code` và `execute_menu_item` là cổng mềm**: guard không đọc được code hay menu chạy gì. Luật:
  - chỉ dùng để thao tác lúc Play: bấm nút, kéo thả, gọi cheat, đọc trạng thái object, đợi một điều kiện;
  - **không** ghi, sửa, xoá file hay asset (`File.*`, `AssetDatabase.Create/Delete/Move/SaveAssets`, `EditorSceneManager.SaveScene`, `PrefabUtility.Save*`, `PlayerSettings`...), không đổi cài đặt project, không cài package;
  - không ghi file nào, kể cả ảnh: ảnh chụp bằng `manage_camera` rồi `tester.py shot` (bước 5 của skill `tester-playtest-session`); dữ liệu đo dạng chữ ghi bằng Write vào `images/`;
  - không dùng nó để làm việc mà guard đã chặn ở lệnh khác.
- Bị guard chặn thì dừng và báo người, không tìm đường vòng.

## Thư mục
```
Tester/
  README.md                      cấu trúc chung
  <Tên tester>/                  UnityAutoPlay
    README.md                    tester này test gì, cách đọc một đợt
    <yyyymmdd-hhmmss>/           một đợt test
      README.md                  bắt buộc, đủ 7 mục (dưới)
      images/                    ảnh và dữ liệu phụ; không để file phụ ở chỗ khác
    Archive/                     đợt cũ; chỉ nhóm Testers và người đọc
      README.md                  danh mục (lệnh tự ghi)
```
Mọi thư mục có `README.md` nói thư mục chứa gì. README của một đợt có đủ 7 mục, đúng tên:
`## Mục đích` · `## Test gì` (feature, node thiết kế `live/...`, state cheat, scene) · `## Môi trường` (commit git, scene, bản dev) · `## Các bước` · `## Kết quả` (dòng đầu: `đạt`, `không đạt` hoặc `dở`, kèm một câu) · `## Lỗi tìm thấy` (mỗi lỗi: thấy ở bước nào, ảnh nào, thuộc feature nào; hoặc `không`) · `## Cấu trúc thư mục` (mỗi file trong `images/` một dòng: tên file và nó là gì).

## Lệnh (`python Management/tools/tester.py ...`, từ gốc project)
| Lệnh | Làm gì | Ai |
|---|---|---|
| `new <Tester> "<tên đợt>"` | tạo thư mục đợt theo giờ hiện tại, `README.md` khung đủ 7 mục, `images/` | tester đó |
| `check` | mọi thư mục có README, đợt đủ mục và đã ghi, file phụ nằm trong `images/` và có dòng mô tả | mọi phiên đọc được `Tester/` |
| `status [<Tester>]` | các đợt đang mở, mục đích và kết quả từng đợt | mọi phiên đọc được `Tester/` |
| `shot <Tester> <đợt> <file> [<tên mới>]` | dời một ảnh từ `UnityProject/Captures` (chỗ `manage_camera` lưu) vào `images/` của đợt đang mở; in dòng cần thêm vào "Cấu trúc thư mục" | tester đó |
| `archive <Tester> <đợt>` | dời một đợt vào `Archive/`, cập nhật danh mục | tester đó, @Manager |
| `archive <Tester> --keep <N>` | giữ N đợt mới nhất, dời phần còn lại | tester đó, @Manager |
Tên đợt viết trong ngoặc kép, không có dấu ngoặc kép hay `\` bên trong.

## Ai giao việc
Tester giữ khoá Unity Editor suốt lúc chơi, nên chỉ vài bên được gọi nó (guard chặn `SendMessage` của mọi phiên khác):
- **Đợt chơi thử** (cả một feature, một luồng nhiều feature, test lại sau khi sửa): chỉ nhận từ **người** hoặc **@Manager**.
- **Yêu cầu xác nhận hẹp** từ @CodeLead hoặc @GameDesigner: một tính năng, một chuỗi thao tác người chơi cụ thể, kết quả mong đợi, theo khuôn `XÁC NHẬN · Feature: … · Thao tác: … (từng bước) · Mong đợi: … · Scene hoặc state cheat: …`. Làm đúng chừng đó, không mở rộng.
  - Yêu cầu của Leader rộng hơn thế (test cả feature, "test lại mọi thứ", không nêu thao tác): không làm; trả lời bên gọi rằng việc đó phải qua @Manager.
  - Xong thì Stop Play ngay để trả khoá Editor, trả lời thẳng bên gọi (đạt hay không, đã thấy gì, đường dẫn đợt), và vẫn ghi một đợt ngắn trong `Tester/` như mọi lần.
- Nhiều việc tới cùng lúc: của người trước, rồi @Manager, rồi Leader. Đang chơi dở thì báo bên gọi mình đang xếp sau việc nào.
- **Mở phiên**: tester sống suốt một khi đã chạy (nhắn bằng `SendMessage`). Khi nó chưa chạy, người (tab Hộp thư) và mọi Leader (@Manager, @CodeLead, @GameDesigner, @UIDesigner) mở nó bằng hộp thư, `python Management/tools/dispatch.py send @UnityAutoPlay --from <vai> "<việc>"`: tin đó bật thread lên. Việc tới qua hộp thư (`[tin <id> · từ <vai>]`) theo đúng luật trên: đợt chơi thử chỉ từ người hoặc @Manager, Leader khác chỉ `XÁC NHẬN` hẹp.
- Tin từ phiên khác lọt tới (chuyển lời qua một bên được phép): không làm, báo `Manager`.

## Một đợt test
Mỗi đợt (cả yêu cầu `XÁC NHẬN` hẹp): dùng skill `tester-playtest-session` (tám bước: nhận việc, `tester.py new`, `gd_pack.py`, chơi, chụp ảnh qua `Captures` rồi `tester.py shot`, ghi kết quả và lỗi, `tester.py check`, báo cáo).

## Báo cáo (Stop hook ép)
Lượt có ghi file kết thúc bằng khối `## Báo cáo cho @Manager` đủ 8 dòng theo `.claude/rules/report.md`: "Đã đổi" là đường dẫn đợt test; "Công khai đổi" ghi `không`; "Cần bên khác" là feature có lỗi và lỗi gì, hoặc `không`; "Kiểm tra" là kết quả `tester.py check` và kết quả đợt test.
Bị chặn (Editor bị giữ lâu, unity-mcp không kết nối, game không vào được Play) hoặc phải chờ người: nhắn `Manager` một tin `BÁO CHẶN · <lỗi | chờ người> · Việc đang làm: … · Vướng ở: … · Ai gỡ được: …`; hết chặn thì `HẾT CHẶN · <việc>`.

## Lưu trữ
- Đợt đã có đợt mới hơn thay thế, hoặc lỗi của nó đã sửa và test lại đạt: dời vào `Archive` (`archive <Tester> <đợt>`). Giữ ở ngoài các đợt còn giá trị cho phiên khác: lỗi chưa sửa, kết quả mới nhất của từng feature.
- Thư mục có trên 10 đợt đang mở: `archive <Tester> --keep 10`.
- Phiên ngoài nhóm Testers không đọc được `Archive`: khi một lỗi cũ còn cần cho phiên khác thì chép phần cần vào README của đợt mới, không bảo họ đọc Archive.

## Yêu cầu riêng của người
(Người bổ sung tại đây; mục này đứng trên các mục khác.)

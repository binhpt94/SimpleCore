---
name: gd-round-4
description: Chỉ phiên FEATURE=@GameDesigner (@UIDesigner và @DesignAdvisor đọc để hiểu). Luật riêng của vòng 4 cây thiết kế, quyết định chi tiết ở ai/idea/<feature>/<detail>/<tech>/. Dùng trước khi viết hoặc sửa một node vòng 4, ui.md, thông số theo giai đoạn, hoặc node cheat <feature>/cheat.
---
# GD vòng 4: Quyết định chi tiết
Luật chung: `.claude/rules/gamedesigner.md`. Skill này chỉ thêm phần riêng của vòng 4 (`ai/idea/<feature>/<detail>/<tech>/`).

## Mục tiêu
Sau vòng 4, phiên code làm được game chơi được ở **mức IDEA** mà không phải hỏi lại thiết kế.

## Viết gì
- Nên làm gì.
- Làm với thông số nào.
- Thông số theo giai đoạn phát triển (xem mục riêng bên dưới).
- Trường hợp biên và ngoại lệ của các quyết định ở vòng 3 (ví dụ: hero HP = 0 nhưng còn thuốc hồi sinh).

## Cụ thể tới mức nào
- **Popup**: có bao nhiêu cái; mỗi cái có bao nhiêu nút và UI component nào bắt buộc phải có để chơi.
- **HUD**: hiện cái gì, lúc nào, mỗi icon đại diện cho gì.
- **Unit**:
  - loại nào;
  - 3D hay 2D, bounding box bao nhiêu;
  - bao nhiêu con trong từng màn;
  - chỉ số từng unit;
  - skill và chỉ số từng skill;
  - danh sách effect, lúc xuất hiện và lúc biến mất;
  - hành vi di chuyển.
- **Balance**: dự đoán và đưa ra bộ chỉ số cùng cách tính.

Con số ghi kèm đơn vị và lí do; con số phỏng đoán ghi là "dự kiến".
Con số dựa trên node khác (chỉ số quái, giá, sát thương của thành...) thì nêu đường dẫn node đó trong backtick và khai trong refs: nhánh kia đổi số thì node này phải làm lại, nhờ vậy số giữa các nhánh không lệch nhau.

## UI cần có (`ui.md`): bạn nêu cần gì, @UIDesigner xếp bố cục
Node có giao diện (popup, màn hình, HUD) viết `ui.md` cạnh `data.md`. Bạn **không** vẽ frame: vị trí, kích thước, nhóm, khoảng cách là việc của @UIDesigner (`.frame-concept/*.frame`, luật ở `uidesigner.md`); guard chặn bạn ghi frame.
```
## Phần tử
- pause | nút | chính | tạm dừng trận | trạng thái: bật, khoá khi đang chuyển màn
- money | nhãn | phụ | tiền hiện có
## Tương tác
- pause chạm: mở popup tạm dừng, dừng thời gian trận
- pause giữ: không làm gì (tránh bấm nhầm)
## Flow
- HUD trận → popup tạm dừng (chồng lên, làm mờ nền)
```
- Phần tử: `- <id> | <loại> | <chính|phụ> | <dùng để làm gì> [| trạng thái: a, b]`. Loại: nút, nhãn, thanh, ô, icon, danh sách, khung. **Chính** là thứ phải thấy và dùng được ngay; frame bắt buộc có mọi phần tử chính. Id viết như key (chữ thường, gạch nối), trùng id hình trong frame.
- Tương tác: mỗi thao tác (chạm, giữ, kéo, vuốt) của mỗi phần tử tương tác được, và kết quả. Flow: màn nào mở màn nào, popup nào chồng lên màn nào, cách đóng.
- `ui.md` tính vào hash data như `data.md`. Viết hay đổi xong thì nhắn `UIDesigner` (SendMessage) node nào và đổi gì; frame cũ thành "chờ @UIDesigner" tới khi nó xếp lại. Node chỉ đạt khi ý của bạn và frame của @UIDesigner cùng đạt (người chấm cả hai).
- Node có bố cục chiến trường (vị trí, tầm, khoảng cách: frame `kind: field`) không có `ui.md`: nêu số trong ý, nhắn @UIDesigner vẽ frame. Node không có bố cục thì khai `frame: none` ở phần đầu `data.md`.
- `ui.md` không ghi kích thước, vị trí, màu. Thứ tự quan trọng nói bằng mức chính/phụ, không bằng toạ độ.
- Xem frame đã có: `python Management/tools/gd_frame.py ai/idea/<node>`. Frame hay dùng số của node khác (`use`): node đó phải nằm trong refs.
- Ảnh art-concept (Codex) không đặt cho node vòng 4. Feature có frame `kind: ui` phải có node `cheat` (xem mục "Node cheat"); ảnh art chỉ đặt ở đó.

## Không cụ thể tới mức này (có chuyên gia riêng)
- Giao diện tương tác: màu, kiểu dáng. Vị trí và kích thước chỉ nằm trong frame của @UIDesigner. Sai: "Nút có màu xanh".
- Hình ảnh: Sai: "Unit sói có mắt đỏ khi dùng skill", "effect màu đỏ hình lưỡi đao".
- Cách viết code: lớp, hàm, kiến trúc là việc của phiên code và @CodeLead.

## Không mơ hồ
Mỗi ý phải làm theo được và kiểm được. Sai: "Unit sói hay vẫy đuôi" (bao lâu một lần, lúc nào, để làm gì?).

## Thông số theo giai đoạn
Vòng 4 là vòng duy nhất được ghi dữ liệu theo giai đoạn, và chỉ ở một chỗ:
- `CLAUDE.md` của node có mục `## Thông số theo giai đoạn`: bảng mỗi hàng một thông số, ba cột IDEA, PROTOTYPE, FULL.
- Node mới viết, hoặc node cũ khi sửa số: số liệu nằm trong bảng Excel của node (`<node>/.excel/`, skill `gd-excel-data`: bảng `params` có cột `idea`, `prototype`, `full`; bảng `list` có cột `stage`), và hai mục `## Thông số`, `## Thông số theo giai đoạn` chỉ còn tên bảng và phần lí giải. Node cũ còn bảng markdown vẫn hợp lệ.
- Ý cốt lõi trong `data.md` nêu giá trị của mức IDEA và không nhắc tới giai đoạn nào.

## Mức IDEA là gì
- Các tính năng đủ để thấy được gameplay.
- Unit 2D: hiển thị được hình, sprite có nghĩa (ảnh @ClaudeImage vẽ là đủ); anim đơn giản bằng scale, move, rotate hoặc đổi màu.
- Unit 3D: model low poly (làm qua blender mcp); anim cơ bản bằng scale, move, rotate, đổi màu.

## Mở khoá code
Guard chỉ cho phiên feature sửa code khi nhánh của nó có ít nhất một node vòng 4 đã chốt trong `live`. Vì vậy vòng 4 phải đủ rõ để code theo; đừng viết sơ sài chỉ để mở khoá.

## `CLAUDE.md` của node
Các mục: Làm gì, Thông số, Thông số theo giai đoạn, Trường hợp biên, Chưa rõ.

## Node cheat (`<feature>/cheat/`)
Mỗi feature một node `cheat`. Nó vừa là bộ state để phiên code dựng ngay khi test và Play (không phải chơi tới nơi), vừa là **chỗ duy nhất đặt ảnh art** của feature. Vì vậy mỗi feature tối đa 3 ảnh màu, còn các node vòng 4 cứ có frame thoải mái.
- **Vị trí:** ngay dưới node feature, ở chỗ của vòng 3, không có node con. Luật vòng 3 không áp cho nó.
- **Bắt buộc** khi feature có ít nhất một frame giao diện (`kind: ui`), kể cả feature hạ tầng. Thiếu thì mọi node vòng 4 có frame `ui` của feature mang lỗi cấu trúc, không chốt được. Feature chỉ có frame chiến trường (`kind: field`) không cần.
- **`data.md`:** mỗi ý là một state, tối đa 3 ý.
  - Tiêu đề nói state đó là gì.
  - Chi tiết ghi giá trị cần ép, ví dụ "điểm 16, kỉ lục 16, cờ kỉ lục mới bật".
- **`.frame-concept/`:** @UIDesigner vẽ đúng một frame cho mỗi state, tên `<id của ý>.frame`; bạn viết state xong thì nhắn nó. Thiếu frame, hay frame không trùng id nào, là lỗi; không khai được `frame: none`.
- **Chọn state:** mỗi state chứa nhiều thành phần nhất có thể. State chỉ ẩn bớt phần so với state khác thì không thành state riêng.
- **Không lặp số:** frame cheat `use` mọi số từ node vòng 4 sở hữu bố cục và khai node đó trong `refs` (kèm hash). Node gốc đổi thì cheat thành "cần làm lại", không lệch được. Frame chưa `use` số nào thì `gd_status.py` nhắc.
- **State gộp nhiều feature** (ví dụ "đợt 10 có trùm, thành còn 30% máu, 3 hero") đặt ở cheat của feature chủ trận (`feature-run`), `use` số từ các feature kia.
- **`priority.json`:** chấm theo mức cần để review, thường là IDEA (không có ảnh thì không review nhanh được).
- **Cheat chỉ chạy ở bản dev.** Code cheat nằm trong thư mục của chính feature, sau cờ dev; không lọt vào bản phát hành.

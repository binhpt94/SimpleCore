---
paths:
  - "Management/studio/**"
  - "Resources/**"
---
# Studio: order ảnh và 3D cho @Codex, @Blender, @ClaudeImage
Nhóm Tools: `@Codex` (ảnh, tham vấn GPT), `@Blender` (model 3D) và `@ClaudeImage` (ảnh Claude tự vẽ bằng SVG; thư mục và tên tool là `claudeimage`). Không code, không sửa `Assets/`. Chỉ nhận order của feature và làm đúng order đó.
Lệnh duy nhất chạy tool: `python Management/tools/studio.py` (bảng lệnh ở đầu file). Guard ép ai được chạy lệnh nào.

## Thư mục
```
Management/studio/<codex|blender|claudeimage>/
  lessons.md                       bài học từ lỗi đã gặp; studio.py chèn vào đầu mọi prompt      phiên tool ghi
  orders/<Feature>/<id>/
    order.md                       đơn hàng                                                       feature tạo, một lần
    review.json                    mức chấm và phản hồi của từng item đã giao                     studio.py, trang
    status.json, log.jsonl         trạng thái, lịch sử mọi lần chạy, giao và chấm                 studio.py
    refs/                          bản chép ảnh tham chiếu trong order                            studio.py
    work/                          prompt (<item>.md), script Blender (<item>.py), nháp            phiên tool ghi
    work/out/<item>.<đuôi>         kết quả của từng item, chờ giao
Resources/<Feature>/<tool>/<id>/v<n>/   mỗi lần giao một bản mới, không ghi đè                     studio.py
```
Mọi phiên đã gán đọc được `Management/studio` và `Resources`. `<Feature>` là thư mục code (`Hero`, `_Services/Popup`); `<id>`, `<item>`: chữ thường, số, gạch nối.

## Khung order (feature viết)
```
---
tool: codex | blender | claudeimage
kind: image | consult | model          codex: image, consult; blender: model; claudeimage: image (art: chỉ order của node thiết kế, xem dưới)
count: 3                               đúng bằng số dòng của Danh sách
deliver: Private | Public              FeatureAssets nào sẽ nhận hàng
format: png 512x512, nền trong suốt
references: Assets/Features/Hero/FeatureAssets/Private/ref.png   (không bắt buộc; file của mình, Public của feature khác, hoặc Resources/)
design: feature-hero/hero-list/hero-roster-values                (không bắt buộc; node thiết kế liên quan)
---
# <tên order>
## Mô tả
<dùng vào đâu, phong cách, kích thước thật trong game>
## Danh sách
- hero-knight: hiệp sĩ cầm khiên, nhìn ngang, quay phải
- hero-archer: cung thủ, nhìn ngang, quay phải
- hero-mage: pháp sư, nhìn ngang, quay phải
## Tiêu chí nhận
<kiểm được: nền trong suốt, đọc rõ ở 64px, khớp tỉ lệ của ref.png...>
```
- Mỗi dòng Danh sách là đúng một kết quả. Muốn 3 ảnh thì 3 dòng. `kind: consult`: mỗi dòng là một câu hỏi cho GPT.
- Không mơ hồ: viết điều kiện đo được trong Tiêu chí nhận. Phong cách thì đưa ảnh tham chiếu.
- Viết xong: `python Management/tools/studio.py check`. Order hợp lệ tự vào hộp thư của tool (`dispatch.py`, mỗi order một lần): không nhắn tool bằng SendMessage (tool chạy theo việc, guard chặn).
- Huỷ một order không cần nữa: `python Management/tools/studio.py cancel <F>/<id> "<lí do>"` (feature đã đặt order đó, @CodeLead, hoặc người). Order thành "đã huỷ": tool không được báo nữa, `check` bỏ qua, mọi lệnh chạy tool, giao, chấm cho nó bị từ chối, tin của nó còn trong hộp thư tự huỷ; bản đã giao vẫn ở `Resources`. Order đã xong thì không huỷ. Cần lại thì đặt order mới với id khác.
- Đặt order chỉ khi feature đã mở khoá code (cùng cổng thiết kế).
- **Giai đoạn IDEA: ảnh đặt @ClaudeImage** (`tool: claudeimage`, `kind: image`) là đủ, không cần ảnh @Codex. Hình Claude vẽ là hình phẳng, rõ nghĩa, đúng kích thước; cần ảnh đẹp hơn (PROTOTYPE, FULL) thì đặt order mới cho @Codex.

## Làm một order
Nhận, làm, tự kiểm, giao, làm lại: dùng skill `studio-fulfill-order` (cả trần lượt gen, đúng số lượng, `lessons.md`, ba đường chạy).

## Mức chấm (`review.json`; mặc định mỗi item vừa giao là "Chưa duyệt")
| Mức | Tên | Hệ quả |
|---|---|---|
| 0 | Làm lại ngay | được chạy lại 1 lượt, chỉ khi có phản hồi |
| 1 | Lỗi nhưng chưa cần làm ngay | dùng tạm; mở phiếu nợ art "người chấm" |
| 2 | Đúng phần lớn chi tiết | dùng |
| 3 | Đạt | dùng; đóng mọi phiếu nợ của item; ảnh concept được đặt làm mốc style |
- Chấm lại bất cứ lúc nào bản giao còn là bản mới nhất; mức sau ghi đè mức trước. Đang làm lại thì chờ bản mới rồi chấm.
- Chấm của feature (`studio.py review … 0`) tự đưa tin làm lại vào hộp thư của tool. Chấm trên trang (ảnh art) chỉ lưu: muốn tool làm lại thì @GameDesigner giao bằng `dispatch.py send`.

## Phiếu nợ art (`Management/studio/debt.json`, chỉ studio.py và trang ghi)
- Mở khi tool giao item chưa chuẩn (loại "chưa chuẩn"), hoặc khi item bị chấm mức 1 (loại "người chấm", lý do là phản hồi).
- Tự đóng khi: item được chấm mức 3; phiếu "người chấm" còn đóng khi item được chấm sang mức khác; hoặc về sau tool giao một bản tự kiểm **đạt** cho cùng owner (feature hay node thiết kế) và cùng item. Người đóng tay bằng nút "Chấp nhận nợ" ở tab Studio của trang quản lý.
- Item bên đặt order đã chấm thì bản giao sau không mở thêm phiếu "chưa chuẩn" cho nó: mức chấm đứng trên tự kiểm của tool.
- Feature vẫn dùng hàng mức 1, 2: `review` chép như thường và ghi `Generated/<id>/QUALITY.md`. Code tiếp; khi cần ảnh tốt hơn thì chấm mức 0 kèm phản hồi (còn lượt) hoặc đặt order mới cho đúng item đó.
- Tab Studio của trang hiện mọi order, lượt đã dùng, mức chấm, item chưa chuẩn và phiếu nợ; ảnh art chưa chuẩn có nhãn ngay trên ảnh ở tab Game design.

## @ClaudeImage: ảnh Claude tự vẽ
- **File SVG** (`work/<item>.svg`): một hình tự đủ trong một file. Gốc `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 <rộng> <cao>">`, viewBox cùng tỉ lệ với kích thước ở `format` của order. Cấm `<script>`, `<foreignObject>`, thuộc tính sự kiện, DOCTYPE, và mọi thứ trỏ ra ngoài file (`href`, `url()`, `@import`; chỉ `#id` và `data:image/`). Order đòi nền trong suốt thì không vẽ hình phủ kín nền.
- `studio.py claude` từ chối SVG sai luật mà **không tính lượt**; SVG hợp lệ thì tính một lượt, cùng trần như mọi tool (2 lượt đầu mỗi item, 1 lượt mỗi lần bị chấm mức 0 có phản hồi). Không có trần 20 lượt mỗi ngày của art Codex.
- Máy đổi SVG ra PNG bằng `STUDIO_SVG2PNG`, `rsvg-convert`, `inkscape`, `magick`, hoặc Chrome/Edge chạy không cửa sổ. Không có cái nào thì lệnh dừng: báo người, không tìm đường vòng (không tự chạy trình duyệt hay công cụ khác bằng shell, không dùng unity-mcp).
- Phản hồi của item bị chấm mức 0 không tự vào đâu cả: đọc ở `studio.py status claudeimage` rồi sửa SVG theo đó. Đọc toàn bộ `claudeimage/lessons.md` trước khi vẽ.
- **Tự kiểm do script làm:** `studio.py claude` tự ghi `work/<item>.check.md`: mọi PNG đúng kích thước được yêu cầu, và file ghi rõ order, item, các hình đã thay để không nhận nhầm order. Khớp thì dòng đầu là `đạt`, lệch thì `chưa đạt: ...`. @ClaudeImage không ghi `check.md` bằng tay và không phải mở ảnh ra xem nội dung.
- **Chạy song song:** @ClaudeImage được giao việc cho tối đa 5 agent con, mỗi agent đúng một frame hoặc một item và chỉ ghi file của frame, item đó; phiên chính gom lại rồi `deliver`. studio.py từ chối lần chạy `claude` thứ 6 cùng lúc và hai lần chạy cùng một item. Frame có `same-as` và state của node cheat chạy sau khi các frame nguồn đã giao.
- **Claude-concept** (`kind: art`, order của node thiết kế trong `claudeimage/orders/_design/`, @UIDesigner đặt, không ai chấm): dùng skill `studio-claude-concept` (vẽ từng phần theo frame, `parts.json`, 9-slice, `same-as`, tool ghép, node cheat, chữ, ghép lại, `redo`).

## Ảnh art-concept của node thiết kế (`kind: art`)
@GameDesigner đặt order chỉ cho node `cheat` của một feature (`<feature>/cheat`, đã ở `live`); `check` từ chối node khác. Đường dẫn: `orders/_design/<feature>.cheat/<id>/`. Mỗi item là id một state của node cheat (cũng là tên frame của state đó), mỗi feature tối đa 3 ảnh; `style:` là các style của vòng 1; `deliver: concept`.
- **Không có bước duyệt gen:** @GameDesigner viết order, `check` qua thì order tự vào hộp thư của @Codex; @Codex chạy được ngay.
  - Phanh là trần lượt ở trên và trần chung: toàn project tối đa 20 lần chạy art mỗi ngày.
  - Hết lượt: dừng, báo người. Không tìm đường vòng (không gọi Codex bằng shell, không qua unity-mcp để né trần).
- **Chấm:** người chấm từng ảnh trên tab Game design bằng thanh trượt (Chưa duyệt và 4 mức ở bảng trên) kèm ô phản hồi; trang lưu ngay vào `review.json` và không nhắn ai. Không phiên nào chấm thay người (`review` từ chối order `_design`).
- **Làm lại:** khi người bảo, @GameDesigner đọc `studio.py status codex` rồi giao @Codex các ảnh mức 0 có phản hồi: `python Management/tools/dispatch.py send @Codex --from GameDesigner "Làm lại <ref> · <item>, …"` (ảnh art do người chấm trên trang nên không có tin tự động). Ảnh mức 0 thiếu phản hồi thì @GameDesigner báo lại người, @Codex không chạy.
- **Khi chạy:** `studio.py codex` tự đưa vào prompt nội dung các style, và đưa cho Codex, theo thứ tự:
  1. frame vẽ ra PNG (bố cục và tỉ lệ phải giữ), có sẵn chữ hiển thị bằng Roboto, không có số đo hay chú thích;
  2. ảnh tham chiếu của style;
  3. tối đa 3 ảnh mốc cùng style.
  `work/<item>.md` chỉ thêm chi tiết cảnh; không viết lại style hay bố cục.
  **Chữ:** ảnh art giữ nguyên các chữ hiển thị của frame (label của `label` và `button`, bỏ phần ngoặc đơn ở cuối): prompt liệt kê từng chữ và yêu cầu đúng chữ, đúng dấu, đúng vị trí, kiểu chữ không chân như Roboto. Không thêm chữ khác, số đo hay đường kẻ. Chữ sai thì người chấm mức 0 kèm phản hồi.
- **Giao:** `deliver codex <ref>` ghi `Resources/_design/<node>/codex/<id>/v<n>/` kèm `meta.json` (hash node, frame, style), rồi @Codex nhắn @GameDesigner. Ảnh không chép vào `Assets`.

## Không làm
- Không viết code game, không sửa `Assets/`, `Resources/`, `order.md`, `review.json` hay `status.json`.
- Không gen thứ order không có, không đổi item hay số lượng.
- Không chạy `codex`, `blender` hay công cụ đổi ảnh bằng shell trực tiếp, chỉ qua studio.py.
- Bị guard chặn thì dừng và báo người.

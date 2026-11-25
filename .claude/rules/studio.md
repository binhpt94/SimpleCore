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
- Viết xong: `python Management/tools/studio.py check`, rồi nhắn @Codex hoặc @Blender bằng SendMessage.
- Đặt order chỉ khi feature đã mở khoá code (cùng cổng thiết kế).
- **Giai đoạn IDEA: ảnh đặt @ClaudeImage** (`tool: claudeimage`, `kind: image`) là đủ, không cần ảnh @Codex. Hình Claude vẽ là hình phẳng, rõ nghĩa, đúng kích thước; cần ảnh đẹp hơn (PROTOTYPE, FULL) thì đặt order mới cho @Codex.

## Quy trình
1. **Tool nhận**: `studio.py status <tool>` (in mức chấm và phản hồi của từng item). Đọc `order.md`, `refs/`, và **toàn bộ `lessons.md`** trước khi làm.
   - **Báo đã nhận**: bên đặt order nhắn tới thì trả lời ngay một tin ngắn bằng SendMessage: đã nhận order nào, đang xếp sau order nào, sẽ nhắn lại khi giao. Đang gen dở thì trả lời ở đầu lượt kế tiếp, trước khi gen item tiếp theo. Làm theo thứ tự order tới; order làm lại (mức 0) chen lên trước.
2. **Làm từng item**, mỗi lần một item:
   - @Codex, ảnh: viết `work/<item>.md` (prompt chi tiết của item), rồi `studio.py codex <F>/<id> <item>`.
   - @Codex, tham vấn: `studio.py consult <F>/<id> <item>`.
   - @ClaudeImage, order của feature: tự vẽ item thành SVG, ghi `work/<item>.svg` bằng Write, rồi `studio.py claude <F>/<id> <item>` (claude-concept của node thiết kế làm theo từng phần: skill `studio-claude-concept`). Lệnh kiểm SVG, đổi ra `work/out/<item>.png` đúng kích thước ghi ở `format` (nền trong suốt), in số đo và tự ghi `work/<item>.check.md`. Luật của file SVG ở mục "@ClaudeImage" bên dưới.
   - @Blender: viết `work/<item>.py` (script bpy; `sys.argv` sau `--` là `<thư mục out> <item>`; lưu đúng một file `<out>/<item>.<đuôi>`), rồi `studio.py blender <F>/<id> <item>`.
3. **Tự kiểm** kết quả theo Tiêu chí nhận, ghi vào `work/<item>.check.md`: dòng đầu `đạt`, hoặc `chưa đạt: <lý do theo Tiêu chí nhận>`; dòng sau so sánh các lượt nếu có.
   - Thứ đo được thì đo, không ước bằng mắt: `studio.py measure <F>/<id> [<item>]` (mọi phiên chạy được, chỉ đọc) in kích thước px, nền có trong suốt không (4 góc, % điểm trong suốt), hình chiếm bao nhiêu % khung và lệch tâm bao nhiêu, và so với dòng `format` của order. Chép các số đó vào `check.md`. Chỉ đo PNG; nội dung hình (đúng đối tượng, phong cách, đọc được khi thu nhỏ) vẫn tự xem ảnh.
   - Chưa đạt và **còn lượt** thì sửa prompt hoặc script rồi chạy lại item đó.
   - **Hết lượt**: studio.py từ chối chạy. Không tìm đường vòng (shell, MCP, unity-mcp). Chọn bản gần order nhất bằng `studio.py pick <F>/<id> <item> <n>` (mọi lượt nằm ở `work/attempts/<item>-<n>.<đuôi>`; pick không tốn lượt), ghi `chưa đạt: <lý do>`, rồi giao.
4. **Giao**: `studio.py deliver <tool> <F>/<id>`. Nó chỉ chấp nhận đúng một file cho mỗi item. Item ghi `chưa đạt` hoặc thiếu tự kiểm được giao với nhãn **chưa chuẩn** (`quality.json`) và mở một phiếu nợ art. Sau đó nhắn feature bằng câu mà lệnh in ra, nói rõ item nào chưa chuẩn.
5. **Bên đặt order chấm từng item** của bản giao (bảng dưới): `studio.py review <F>/<id> <item> <0-3> "<phản hồi>"`. Item mức 1–3 được chép ngay vào `FeatureAssets/<deliver>/Generated/<id>/` (refresh Unity để có `.meta`); mức 1, 2 ghi vào `QUALITY.md` ở đó. Order tham vấn thì đọc câu trả lời ở `Resources/`, vẫn chấm nhưng không chép. Phản hồi viết trong ngoặc kép, không dùng `;` `&` `|` `<` `>` `$` `(` `)` `{` `}` hay dấu ngoặc kép bên trong.
6. **Có item mức 0**: bên đặt order nhắn tool. Tool chạy lại đúng item đó **1 lượt**; studio.py tự chèn phản hồi vào prompt. Item mức 0 mà không có phản hồi thì studio.py từ chối chạy.
7. **Tool giao bản mới** (`deliver`, đủ mọi item như lần đầu): item vừa làm lại về "Chưa duyệt", item không làm lại giữ mức cũ.

## Mức chấm (`review.json`; mặc định mỗi item vừa giao là "Chưa duyệt")
| Mức | Tên | Hệ quả |
|---|---|---|
| 0 | Làm lại ngay | được chạy lại 1 lượt, chỉ khi có phản hồi |
| 1 | Lỗi nhưng chưa cần làm ngay | dùng tạm; mở phiếu nợ art "người chấm" |
| 2 | Đúng phần lớn chi tiết | dùng |
| 3 | Đạt | dùng; đóng mọi phiếu nợ của item; ảnh concept được đặt làm mốc style |
- Chấm lại bất cứ lúc nào bản giao còn là bản mới nhất; mức sau ghi đè mức trước. Đang làm lại thì chờ bản mới rồi chấm.
- Chấm chỉ lưu giá trị, không nhắn ai: muốn tool làm lại thì bên đặt order phải nhắn.

## Trần lượt gen (studio.py ép, mọi order: ảnh, art, model, tham vấn)
| | Lượt mỗi item |
|---|---|
| Trước lần giao đầu | 2 (một lần thử lại khi tự kiểm chưa đạt) |
| Mỗi lần item bị chấm mức 0 kèm phản hồi | 1 |
| Tối đa | 2 lần làm lại mỗi item, tức 4 lượt |
- Trần tính theo từng item. Item chưa bị chấm mức 0, hoặc mức 0 mà thiếu phản hồi, thì không chạy lại được.
- Item đã làm lại 2 lần mà vẫn bị chấm mức 0: studio.py từ chối chạy. Bên đặt order chấm mức 1 để ghi nợ, hoặc đặt order mới.
- `studio.py status` in số lượt đã dùng, còn lại, và lý do khi một item mức 0 chưa làm lại được.

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
- **Không có bước duyệt gen:** @GameDesigner viết order, `check` qua thì nhắn @Codex; @Codex chạy được ngay.
  - Phanh là trần lượt ở trên và trần chung: toàn project tối đa 20 lần chạy art mỗi ngày.
  - Hết lượt: dừng, báo người. Không tìm đường vòng (không gọi Codex bằng shell, không qua unity-mcp để né trần).
- **Chấm:** người chấm từng ảnh trên tab Game design bằng thanh trượt (Chưa duyệt và 4 mức ở bảng trên) kèm ô phản hồi; trang lưu ngay vào `review.json` và không nhắn ai. Không phiên nào chấm thay người (`review` từ chối order `_design`).
- **Làm lại:** khi người bảo, @GameDesigner đọc `studio.py status codex` rồi nhắn @Codex các ảnh mức 0 có phản hồi. Ảnh mức 0 thiếu phản hồi thì @GameDesigner báo lại người, @Codex không chạy.
- **Khi chạy:** `studio.py codex` tự đưa vào prompt nội dung các style, và đưa cho Codex, theo thứ tự:
  1. frame vẽ ra PNG (bố cục và tỉ lệ phải giữ), có sẵn chữ hiển thị bằng Roboto, không có số đo hay chú thích;
  2. ảnh tham chiếu của style;
  3. tối đa 3 ảnh mốc cùng style.
  `work/<item>.md` chỉ thêm chi tiết cảnh; không viết lại style hay bố cục.
  **Chữ:** ảnh art giữ nguyên các chữ hiển thị của frame (label của `label` và `button`, bỏ phần ngoặc đơn ở cuối): prompt liệt kê từng chữ và yêu cầu đúng chữ, đúng dấu, đúng vị trí, kiểu chữ không chân như Roboto. Không thêm chữ khác, số đo hay đường kẻ. Chữ sai thì người chấm mức 0 kèm phản hồi.
- **Giao:** `deliver codex <ref>` ghi `Resources/_design/<node>/codex/<id>/v<n>/` kèm `meta.json` (hash node, frame, style), rồi @Codex nhắn @GameDesigner. Ảnh không chép vào `Assets`.

## Đúng số lượng
- Mỗi item một kết quả. Không tự gen thêm biến thể, bản nháp, ảnh xem trước hay item ngoài Danh sách.
- studio.py ép luật này:
  - một lần chạy sinh ra nhiều hơn một file thì không lấy file nào (dời vào `work/_rejected/`);
  - file không thuộc item nào thì không được giao.
- Cần thêm hay bớt item thì feature đặt order mới. Tool không tự sửa order.

## Bài học (`lessons.md`): để không gen nhiều đối tượng cùng sai một lỗi
Mỗi mục một dòng, ghi lỗi dưới dạng luật làm được, kèm nguồn:
```
- Icon nhỏ: nét viền tối thiểu 2px ở 64px, không chi tiết mảnh hơn (codex/orders/Wallet/coin-icons: phản hồi của coin, v1)
- Model quái: pivot ở chân, mặt quay +Z, áp scale trước khi xuất (blender/orders/Enemy/wolf/log: lần chạy 14:02)
```
- Thêm mục khi: phản hồi của một item nói lên một luật chung, tự kiểm thấy lỗi, hoặc log có lỗi lặp lại. Phản hồi riêng của một item đã tự vào prompt của item đó, không cần chép; bài học không còn là điều kiện để chạy lại.
- Viết thành luật chung, áp được cho order sau, không viết riêng cho một item.
- Gộp các mục trùng nhau; không xoá mục cũ khi chưa hỏi người.

## Ba đường chạy (theo thứ tự, chỉ chuyển khi đường trước không được)
**@Codex, ảnh**
1. `studio.py codex`: Codex CLI ghi trực tiếp (`-s workspace-write`, chỉ trong `work/` của order).
2. Cũng lệnh đó, tự chuyển sang chỉ đọc (`-s read-only`), rồi lấy đúng ảnh mới Codex lưu trong `~/.codex/generated_images/`.
3. Cả hai không được (lệnh in hướng dẫn): đi gián tiếp qua unity-mcp. Tìm cấu hình tool của Codex trong unity-mcp, rồi gọi `execute_custom_tool` hoặc `execute_menu_item`. Guard giữ khoá Editor cho chat đó tới hết lượt.

**@Blender, model**
1. `studio.py blender`: Blender headless (`blender -b --factory-startup -P work/<item>.py`).
2. Không được: blender MCP (`mcp__blender__*`, chỉ @Blender). Chạy cùng nội dung script và lưu vào `work/out/<item>.<đuôi>`.
3. MCP không kết nối được: qua unity-mcp như Codex.

Đường 2 của Blender và đường 3 của cả hai là cổng mềm: guard không đọc được code bên trong. Vì vậy:
- kết quả chỉ được để ở `work/out/` của đúng order;
- không ghi vào `Assets/`, `Resources/` hay nơi khác;
- sản phẩm luôn đi qua `deliver`;
- ghi vào `work/<item>.md` đã đi đường nào và vì sao.

## Không làm
- Không viết code game, không sửa `Assets/`, `Resources/`, `order.md`, `review.json` hay `status.json`.
- Không gen thứ order không có, không đổi item hay số lượng.
- Không chạy `codex`, `blender` hay công cụ đổi ảnh bằng shell trực tiếp, chỉ qua studio.py.
- Bị guard chặn thì dừng và báo người.

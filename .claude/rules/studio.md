# Studio: order ảnh và 3D cho @Codex, @Blender
Nhóm Tools: `@Codex` (ảnh, tham vấn GPT) và `@Blender` (model 3D). Không code, không sửa `Assets/`. Chỉ nhận order của feature và làm đúng order đó.
Lệnh duy nhất chạy tool: `python Management/tools/studio.py` (bảng lệnh ở đầu file). Guard ép ai được chạy lệnh nào.

## Thư mục
```
Management/studio/<codex|blender>/
  lessons.md                       bài học từ lỗi đã gặp; studio.py chèn vào đầu mọi prompt      phiên tool ghi
  orders/<Feature>/<id>/
    order.md                       đơn hàng                                                       feature tạo, một lần
    feedback-<n>.md                phản hồi khi nhận hàng                                         feature tạo, một lần
    status.json, log.jsonl         trạng thái, lịch sử mọi lần chạy và giao                       studio.py
    refs/                          bản chép ảnh tham chiếu trong order                            studio.py
    work/                          prompt (<item>.md), script Blender (<item>.py), nháp            phiên tool ghi
    work/out/<item>.<đuôi>         kết quả của từng item, chờ giao
Resources/<Feature>/<tool>/<id>/v<n>/   mỗi lần giao một bản mới, không ghi đè                     studio.py
```
Mọi phiên đã gán đọc được `Management/studio` và `Resources`. `<Feature>` là thư mục code (`Hero`, `_Services/Popup`); `<id>`, `<item>`: chữ thường, số, gạch nối.

## Khung order (feature viết)
```
---
tool: codex | blender
kind: image | consult | model          codex: image, consult; blender: model (art: chỉ order của node thiết kế, xem dưới)
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

## Quy trình
1. **Tool nhận**: `studio.py status <tool>`. Đọc `order.md`, `refs/`, các feedback, và **toàn bộ `lessons.md`** trước khi làm.
2. **Làm từng item**, mỗi lần một item:
   - @Codex, ảnh: viết `work/<item>.md` (prompt chi tiết của item), rồi `studio.py codex <F>/<id> <item>`.
   - @Codex, tham vấn: `studio.py consult <F>/<id> <item>`.
   - @Blender: viết `work/<item>.py` (script bpy; `sys.argv` sau `--` là `<thư mục out> <item>`; lưu đúng một file `<out>/<item>.<đuôi>`), rồi `studio.py blender <F>/<id> <item>`.
3. **Tự kiểm** kết quả theo Tiêu chí nhận, ghi vào `work/<item>.check.md`: dòng đầu `đạt`, hoặc `chưa đạt: <lý do theo Tiêu chí nhận>`; dòng sau so sánh các lượt nếu có.
   - Chưa đạt và **còn lượt** thì sửa prompt hoặc script rồi chạy lại item đó.
   - **Hết lượt**: studio.py từ chối chạy. Không tìm đường vòng (shell, MCP, unity-mcp). Chọn bản gần order nhất bằng `studio.py pick <F>/<id> <item> <n>` (mọi lượt nằm ở `work/attempts/<item>-<n>.<đuôi>`; pick không tốn lượt), ghi `chưa đạt: <lý do>`, rồi giao.
4. **Giao**: `studio.py deliver <tool> <F>/<id>`. Nó chỉ chấp nhận đúng một file cho mỗi item. Item ghi `chưa đạt` hoặc thiếu tự kiểm được giao với nhãn **chưa chuẩn** (`quality.json`) và mở một phiếu nợ art. Sau đó nhắn feature bằng câu mà lệnh in ra, nói rõ item nào chưa chuẩn.
5. **Feature nhận**: `studio.py take <F>/<id>`, chép vào `FeatureAssets/<deliver>/Generated/<id>/`; refresh Unity để có `.meta`. Order tham vấn thì đọc câu trả lời ở `Resources/`.
6. **Feature trả**: viết `feedback-<n>.md` (item nào, sai gì, cần thế nào), rồi `studio.py reject <F>/<id>`, rồi nhắn tool.
7. **Tool làm lại**: trước hết thêm một mục vào `lessons.md`. Thiếu mục này thì studio.py không cho chạy lại. Mỗi lần bị trả chỉ thêm 1 lượt cho mỗi item.

## Trần lượt gen (studio.py ép, mọi order: ảnh, art, model, tham vấn)
| | Lượt mỗi item |
|---|---|
| Lần đầu | 2 (một lần thử lại khi tự kiểm chưa đạt) |
| Mỗi lần bị trả | thêm 1 |
| Tối đa | 2 lần làm lại, tức 4 lượt |
- Lần trả thứ 3 không mở vòng mới: order **đóng** (`closed`), mỗi item một phiếu nợ "trả quá số lần". Vẫn ghi bài học từ feedback đó. Cần làm lại thì đặt order mới.
- `studio.py status` in số lượt đã dùng và còn lại của từng item.

## Phiếu nợ art (`Management/studio/debt.json`, chỉ studio.py và trang ghi)
- Mở khi giao item chưa chuẩn, hoặc khi order bị đóng vì trả quá số lần.
- Tự đóng khi về sau một bản giao **đạt** cho cùng owner (feature hay node thiết kế) và cùng item. Người đóng tay bằng nút "Chấp nhận nợ" ở tab Studio của trang quản lý.
- Feature vẫn dùng hàng chưa chuẩn: `take` chép như thường và ghi `Generated/<id>/QUALITY.md` (item nào, vì sao, mã phiếu). Code tiếp; khi cần ảnh tốt hơn thì đặt order mới cho đúng item đó.
- Tab Studio của trang hiện mọi order, lượt đã dùng, item chưa chuẩn và phiếu nợ; ảnh art chưa chuẩn có nhãn ngay trên ảnh ở tab Game design.

## Ảnh art-concept của node thiết kế (`kind: art`)
@GameDesigner đặt order chỉ cho node `cheat` của một feature (`<feature>/cheat`, đã ở `live`); `check` từ chối node khác. Đường dẫn: `orders/_design/<feature>.cheat/<id>/`. Mỗi item là id một state của node cheat (cũng là tên frame của state đó), mỗi feature tối đa 3 ảnh; `style:` là các style của vòng 1; `deliver: concept`.
- **Cổng duyệt:** order chờ người bấm "Duyệt gen" (nhập PIN) trên tab Game design. Trang ghi `approval.json`; không phiên nào ghi được file này.
  - Approval gắn với đúng nội dung `order.md` và hash của node trong live; một trong hai đổi thì phải duyệt lại.
  - Mỗi item được chạy tối đa 2 lần cho một lần duyệt (một lần thử lại khi tự kiểm chưa đạt). Toàn project tối đa 20 lần chạy art mỗi ngày.
  - Hết lượt: dừng, báo người. Không tìm đường vòng (không gọi Codex bằng shell, không qua unity-mcp để né cổng).
- **Khi chạy:** `studio.py codex` tự đưa vào prompt nội dung các style, và đưa cho Codex, theo thứ tự:
  1. frame vẽ ra PNG (bố cục và tỉ lệ phải giữ);
  2. ảnh tham chiếu của style;
  3. tối đa 3 ảnh mốc cùng style.
  `work/<item>.md` chỉ thêm chi tiết cảnh; không viết lại style hay bố cục.
- **Giao:** `deliver codex <ref>` ghi `Resources/_design/<node>/codex/<id>/v<n>/` kèm `meta.json` (hash node, frame, style). Không có `take`: người nhận hoặc trả trên trang, rồi @Codex nhắn @GameDesigner.
- **Bị trả:** ghi bài học vào `lessons.md` như mọi order, rồi chờ người duyệt lại.

## Đúng số lượng
- Mỗi item một kết quả. Không tự gen thêm biến thể, bản nháp, ảnh xem trước hay item ngoài Danh sách.
- studio.py ép luật này:
  - một lần chạy sinh ra nhiều hơn một file thì không lấy file nào (dời vào `work/_rejected/`);
  - file không thuộc item nào thì không được giao.
- Cần thêm hay bớt item thì feature đặt order mới. Tool không tự sửa order.

## Bài học (`lessons.md`): để không gen nhiều đối tượng cùng sai một lỗi
Mỗi mục một dòng, ghi lỗi dưới dạng luật làm được, kèm nguồn:
```
- Icon nhỏ: nét viền tối thiểu 2px ở 64px, không chi tiết mảnh hơn (codex/orders/Wallet/coin-icons/feedback-1.md)
- Model quái: pivot ở chân, mặt quay +Z, áp scale trước khi xuất (blender/orders/Enemy/wolf/log: lần chạy 14:02)
```
- Thêm mục khi: feature trả hàng (bắt buộc, ghi đường dẫn feedback), tự kiểm thấy lỗi, hoặc log có lỗi lặp lại.
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
- Không viết code game, không sửa `Assets/`, `Resources/`, `order.md`, feedback hay `status.json`.
- Không gen thứ order không có, không đổi item hay số lượng.
- Không chạy `codex` hay `blender` bằng shell trực tiếp, chỉ qua studio.py.
- Bị guard chặn thì dừng và báo người.

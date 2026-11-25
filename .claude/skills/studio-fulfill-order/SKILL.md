---
name: studio-fulfill-order
description: Chỉ nhóm Tools FEATURE=@Codex, @Blender, @ClaudeImage. Làm một order của studio từ lúc nhận tới lúc giao - báo đã nhận, làm từng item (codex, consult, claude, blender), tự kiểm và đo, trần lượt gen, pick, deliver, làm lại item mức 0, đúng số lượng, ghi lessons.md, ba đường chạy. Dùng khi nhận, làm hoặc làm lại một order.
---
# Studio: làm một order
Thư mục, khung order, mức chấm, phiếu nợ art, luật riêng của @ClaudeImage và "Không làm" vẫn ở `.claude/rules/studio.md`. Claude-concept: skill `studio-claude-concept`. Chỉ theo quy trình này; không dùng các skill toàn cục khác về ảnh hay 3D (`popup-reskin-codex`, `popup-concept-match`, `gen3d-asset-pipeline`).

## Quy trình
1. **Tool nhận**: `studio.py status <tool>` (in mức chấm và phản hồi của từng item). Đọc `order.md`, `refs/`, và **toàn bộ `lessons.md`** trước khi làm.
   - **Báo đã nhận**: bên đặt order nhắn tới thì trả lời ngay một tin ngắn bằng SendMessage: đã nhận order nào, đang xếp sau order nào, sẽ nhắn lại khi giao. Đang gen dở thì trả lời ở đầu lượt kế tiếp, trước khi gen item tiếp theo. Làm theo thứ tự order tới; order làm lại (mức 0) chen lên trước.
2. **Làm từng item**, mỗi lần một item:
   - @Codex, ảnh: viết `work/<item>.md` (prompt chi tiết của item), rồi `studio.py codex <F>/<id> <item>`.
   - @Codex, tham vấn: `studio.py consult <F>/<id> <item>`.
   - @ClaudeImage, order của feature: tự vẽ item thành SVG, ghi `work/<item>.svg` bằng Write, rồi `studio.py claude <F>/<id> <item>` (claude-concept của node thiết kế làm theo từng phần: skill `studio-claude-concept`). Lệnh kiểm SVG, đổi ra `work/out/<item>.png` đúng kích thước ghi ở `format` (nền trong suốt), in số đo và tự ghi `work/<item>.check.md`. Luật của file SVG ở mục "@ClaudeImage" của `studio.md`.
   - @Blender: viết `work/<item>.py` (script bpy; `sys.argv` sau `--` là `<thư mục out> <item>`; lưu đúng một file `<out>/<item>.<đuôi>`), rồi `studio.py blender <F>/<id> <item>`.
3. **Tự kiểm** kết quả theo Tiêu chí nhận, ghi vào `work/<item>.check.md`: dòng đầu `đạt`, hoặc `chưa đạt: <lý do theo Tiêu chí nhận>`; dòng sau so sánh các lượt nếu có.
   - Thứ đo được thì đo, không ước bằng mắt: `studio.py measure <F>/<id> [<item>]` (mọi phiên chạy được, chỉ đọc) in kích thước px, nền có trong suốt không (4 góc, % điểm trong suốt), hình chiếm bao nhiêu % khung và lệch tâm bao nhiêu, và so với dòng `format` của order. Chép các số đó vào `check.md`. Chỉ đo PNG; nội dung hình (đúng đối tượng, phong cách, đọc được khi thu nhỏ) vẫn tự xem ảnh.
   - Chưa đạt và **còn lượt** thì sửa prompt hoặc script rồi chạy lại item đó.
   - **Hết lượt**: studio.py từ chối chạy. Không tìm đường vòng (shell, MCP, unity-mcp). Chọn bản gần order nhất bằng `studio.py pick <F>/<id> <item> <n>` (mọi lượt nằm ở `work/attempts/<item>-<n>.<đuôi>`; pick không tốn lượt), ghi `chưa đạt: <lý do>`, rồi giao.
4. **Giao**: `studio.py deliver <tool> <F>/<id>`. Nó chỉ chấp nhận đúng một file cho mỗi item. Item ghi `chưa đạt` hoặc thiếu tự kiểm được giao với nhãn **chưa chuẩn** (`quality.json`) và mở một phiếu nợ art. Sau đó nhắn feature bằng câu mà lệnh in ra, nói rõ item nào chưa chuẩn.
5. **Bên đặt order chấm từng item** của bản giao (bảng "Mức chấm" ở `studio.md`): `studio.py review <F>/<id> <item> <0-3> "<phản hồi>"`. Item mức 1–3 được chép ngay vào `FeatureAssets/<deliver>/Generated/<id>/` (refresh Unity để có `.meta`); mức 1, 2 ghi vào `QUALITY.md` ở đó. Order tham vấn thì đọc câu trả lời ở `Resources/`, vẫn chấm nhưng không chép. Phản hồi viết trong ngoặc kép, không dùng `;` `&` `|` `<` `>` `$` `(` `)` `{` `}` hay dấu ngoặc kép bên trong.
6. **Có item mức 0**: bên đặt order nhắn tool. Tool chạy lại đúng item đó **1 lượt**; studio.py tự chèn phản hồi vào prompt. Item mức 0 mà không có phản hồi thì studio.py từ chối chạy.
7. **Tool giao bản mới** (`deliver`, đủ mọi item như lần đầu): item vừa làm lại về "Chưa duyệt", item không làm lại giữ mức cũ.

## Trần lượt gen (studio.py ép, mọi order: ảnh, art, model, tham vấn)
| | Lượt mỗi item |
|---|---|
| Trước lần giao đầu | 2 (một lần thử lại khi tự kiểm chưa đạt) |
| Mỗi lần item bị chấm mức 0 kèm phản hồi | 1 |
| Tối đa | 2 lần làm lại mỗi item, tức 4 lượt |
- Trần tính theo từng item. Item chưa bị chấm mức 0, hoặc mức 0 mà thiếu phản hồi, thì không chạy lại được.
- Item đã làm lại 2 lần mà vẫn bị chấm mức 0: studio.py từ chối chạy. Bên đặt order chấm mức 1 để ghi nợ, hoặc đặt order mới.
- `studio.py status` in số lượt đã dùng, còn lại, và lý do khi một item mức 0 chưa làm lại được.

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

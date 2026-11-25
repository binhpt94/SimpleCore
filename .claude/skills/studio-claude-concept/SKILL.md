---
name: studio-claude-concept
description: Chỉ phiên FEATURE=@ClaudeImage. Vẽ claude-concept của một node thiết kế (order kind art trong claudeimage/orders/_design/) - vẽ từng phần theo frame, bảng parts.json, 9-slice, same-as, tool ghép, node cheat, chữ trên concept, ghép lại khi frame đổi, redo. Dùng khi nhận hoặc làm lại một order claude-concept.
---
# Claude-concept: vẽ từng phần theo frame, tool ghép
Luật chung của @ClaudeImage (file SVG hợp lệ, trần lượt, tự kiểm, chạy song song tối đa 5 agent con) vẫn ở `.claude/rules/studio.md`, mục "@ClaudeImage". Skill này là phần riêng của order claude-concept.

## Order là gì
- `kind: art`, do @UIDesigner đặt ở `Management/studio/claudeimage/orders/_design/<node, '/' thay bằng '.'>/<id>/`. Mỗi item là một frame của một node vòng 4 đã ở `live`.
- **Không ai chấm** (không có thanh trượt, giao xong là xong). `meta.json` như art-concept (hash node, frame, style). Giao vào `Resources/_design/<node>/claudeimage/<id>/v<n>/`.
- Không vẽ ảnh cả màn: **vẽ từng phần theo thông số của frame, tool ghép**.

## Các bước cho một frame
1. **Xem hình nào phải vẽ:** `python Management/tools/gd_frame.py live/<node>` in ba nhóm cho mỗi frame.
   - "Của node" (hình dùng `let` của chính frame, hoặc chỉ có số viết thẳng): vẽ.
   - "Phụ" (chỉ `use` số của node khác): tool giữ hình cơ bản của frame.
   - "Nền phụ" (phủ từ 90% màn): bỏ qua, để trong suốt.
   - Chữ (`label`) và nét đo (`ring`, `line`, `arrow`) tool tự vẽ.
2. **Bảng phần** `work/<frame>.parts.json`:
   ```
   {"parts": [{"id": "btn", "w": 320, "h": 120, "slice": [24, 24, 24, 24], "shapes": ["replay", "home"]}, ...]}
   ```
   - Mỗi hình "của node" nằm trong đúng một phần. Nhiều hình cùng kiểu thì gom vào **một phần dùng chung**.
   - `slice` là viền trái, trên, phải, dưới (px) khi phần **9-slice** được (dùng cho hình khác cỡ). `null` thì phần phải cùng tỉ lệ với mọi hình nó thay (lệch quá 2% bị từ chối).
   - Phần đã có ở claude-concept còn dùng được của node khác thì không vẽ lại: `{"id": "panel", "same-as": "<node>#<id phần>", "shapes": [...]}`.
   - Một `label` được khai phần riêng nếu cần hình cho nó.
   - Chia sao cho ít hình nhất mà vẫn đúng: đây là căn cứ để feature biết cần bao nhiêu sprite.
   - Frame chỉ có chữ hay hình phụ thì bảng phần rỗng (`{"parts": []}`).
3. **Hình của từng phần:** `work/<frame>/<id phần>.svg`, viewBox cùng tỉ lệ `w`:`h`, theo style trong `Management/game-design/live/.art-style`. Phần 9-slice vẽ sao cho bốn góc nằm gọn trong viền, cạnh và giữa kéo giãn được.
4. **Chạy** `python Management/tools/studio.py claude <ref> <frame>`:
   - kiểm bảng phần và từng SVG (sai thì từ chối, không tính lượt);
   - đổi mỗi phần ra PNG đúng `w`x`h`;
   - ghép ảnh cả frame vào `work/out/<frame>.png` (phần đặt đúng toạ độ của frame, 9-slice giữ góc), để phần ở `work/parts/<frame>/`;
   - in số phần, phần nào 9-slice, phần nào dùng nhiều chỗ.
   Một lần chạy là một lượt của frame đó.
5. **Giao:** `python Management/tools/studio.py deliver claudeimage <ref>` giao ảnh ghép kèm `parts/<frame>/` (PNG, SVG, `parts.json`), rồi nhắn @UIDesigner. Tiêu chí nhận của order chỉ áp cho **các phần được vẽ**; hình cơ bản tool giữ lại trong ảnh ghép không thuộc tiêu chí.

## Chữ trên concept
- Được hiện. Chữ hiển thị là label của hình loại `label` và `button`, bỏ phần ngoặc đơn ở cuối ("Chơi lại (nút chính)" hiện "Chơi lại"). Label của hình khác là chú thích của người thiết kế, không lên ảnh; số đo không bao giờ lên ảnh.
- Tool tự in chữ hiển thị bằng font **Roboto** (file ở `Management/view/fonts/`, nhúng vào ảnh): lên hình giữ lại và lên trên phần đã vẽ của nút, nhãn.
- Phần đã tự vẽ chữ thì khai `"text": false` trong bảng phần để tool không in đè.
- Chữ trong SVG của phần viết `font-family="Roboto"` (Regular 400, Bold 700); studio.py tự nhúng font khi đổi ra PNG.

## Node cheat
Không viết gì. `studio.py claude <ref> <state>` tự dựng state từ phần của các node mà frame cheat `use` (hình cùng id, không có thì cùng loại và cùng cỡ); không tính lượt. Thiếu phần thì lệnh không ra ảnh, không giao được, và in node, frame nào phải có claude-concept trước: order đó đang chờ thì làm và giao nó trước; chưa có order thì nhắn @UIDesigner đặt. Không vẽ bù trong cheat. Frame có `same-as` và state của cheat chạy sau khi các frame nguồn đã giao.

## Khi frame đổi
Node làm lại và chốt thì tool xét từng phần, không đánh cũ cả ảnh:
- node chỉ đổi chữ: không sao;
- bố cục đổi mà mọi hình vẫn có phần vừa: nhãn "Đã giao · bố cục đổi, chờ ghép lại";
- nguồn của cheat hoặc của `same-as` giao bản mới: "… phần nguồn có bản mới, chờ ghép lại".

**Ghép lại** là chạy lại `claude` với bảng phần và SVG giữ nguyên: không vẽ gì, không tính lượt, không cần lệnh của ai, rồi `deliver`.

Chỉ khi phải vẽ (hình mới chưa có phần, phần không còn vừa, style đổi) ảnh mới mang nhãn "CẦN LÀM LẠI · cần vẽ: …". **Vẽ sau khi đã giao phải có lệnh của một Leader** (@UIDesigner, @GameDesigner, @CodeLead, @Manager, hoặc người): `python Management/tools/studio.py redo <ref> <frame> "<lí do>"` mở đúng 1 lượt cho frame đó; `status` in lí do. Không có lệnh thì `claude` từ chối mọi lần chạy có phần mới hoặc phần đổi hình; @ClaudeImage không tự quyết việc vẽ lại.

## Bộ phần là tham chiếu
`gd_pack.py <Tên>` in bảng phần và tổng số hình của feature; phiên feature tự đặt order sprite theo đó, không chép phần vào `Assets`. Giai đoạn IDEA chỉ cần claude-concept; art-concept độc lập với nó, ảnh mốc style chỉ lấy từ ảnh Codex.

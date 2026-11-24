# Thêm feature-battlefield (Nền chiến trường), có ngay ở IDEA

Người giao (2026-11-25): thêm feature riêng cho nền chiến trường, thư mục code `Battlefield`, **bắt buộc có ở mức IDEA** (node lá chấm độ quan trọng từ 8).

## Vì sao
- Thiết kế chưa có node nào lo phần nền, dù vòng 1 đã nói style `world-cartoon` dùng cho cả nền.
- Nền không thuộc Castle, Enemy hay Hero, nhưng cả ba đều đứng trên nó. Tách riêng thì một phiên lo trọn, và các feature khác không phải chép số dải đất.

## Nội dung người đề xuất
- **Bầu trời:** một lớp màu hoặc gradient, phủ sau cùng.
- **Cảnh xa:** đồi, núi, rừng phía sau; parallax ở giai đoạn sau.
- **Dải đất:** trùng mặt đất y=0, phủ hết dải thấy được x = −16 … +16u, để quái và hero đứng lên.
- **Decor:** cỏ, đá, cờ, hàng rào, đặt dọc hai bên thành. Decor không chặn đường đi và không che dải chiến trường mà HUD đang chừa.

## Theo giai đoạn
| | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Nền | một ảnh trời, một dải đất, vài decor tĩnh | cảnh xa có parallax, decor đa dạng hơn | hiệu ứng nhẹ (mây trôi, cỏ lay); có thể đổi nền theo mốc đợt |
| Art | @ClaudeImage | @Codex | @Codex |

## Chuỗi việc
1. **@GameDesigner** viết đủ 4 vòng:
   - vòng 2 `feature-battlefield`, kèm `code.json` `{"unity": ["Battlefield"]}`, bắt buộc hỏi GPT qua @DesignAdvisor;
   - vòng 3 và vòng 4: frame `kind: field`, `use` số dải đất và vị trí thành từ `feature-balance/balance-config/number-table` và `feature-castle/castle-core/castle-body-values`, khai refs;
   - node lá chấm độ quan trọng từ 8; không cần node cheat, vì feature chỉ có frame chiến trường;
   - rà thêm lớp vẽ của nền so với thành, quái, hero, và dải HUD chừa (`feature-hud/hud-layout/layout-spec`).
2. **Claude-concept:** @GameDesigner đặt order cho node vòng 4 đã chốt; @ClaudeImage vẽ phần.
3. **Người chốt sang live.** `gd_status.py`, mục "Cổng code theo feature", phải ghi `Battlefield: ĐƯỢC CODE`.
4. **@Manager** chạy `scaffold.py`; người mở phiên `Battlefield` ở tab Setup.
5. **@CodeLead** dựng khung (asmdef, AssemblyInfo; Contracts chỉ khi có feature khác cần).
6. **Phiên Battlefield:**
   - đọc `gd_pack.py Battlefield`, đặt order sprite `tool: claudeimage` theo bảng phần, mỗi cỡ một order;
   - tham gia `ISessionScene` (`SessionParticipantBase`), số liệu trong config ở `FeatureAssets/Private`, installer, test EditMode;
   - `validate.py --feature Battlefield`, báo cáo.
7. **@CodeLead** nối installer vào `Boot.Game`, chạy test, Play kiểm: nền đúng y=0, phủ đủ dải, decor không chặn, nằm sau thành, quái, hero; rồi đánh IDEA `done`.

## Song song
- Bước 1-3 chạy song song với buổi chơi thử chốt IDEA phần còn lại (`2026-11-25-choi-thu-chot-idea.md`).
- Order sprite ở bước 6 song song với phần code logic.
- Play ở bước 7 dùng chung khoá Editor: xếp sau buổi chơi thử, hoặc gộp vào buổi đó.

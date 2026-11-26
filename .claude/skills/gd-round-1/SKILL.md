---
name: gd-round-1
description: Chỉ phiên FEATURE=@GameDesigner (@DesignAdvisor đọc để hiểu). Luật riêng của vòng 1 cây thiết kế, ý tưởng sơ khai ở node gốc ai/idea/. Dùng trước khi viết hoặc sửa node gốc - game là gì, hướng màn hình, style hình ảnh (.art-style).
---
# GD vòng 1: Ý tưởng sơ khai
Luật chung: `.claude/rules/gamedesigner.md`. Skill này chỉ thêm phần riêng của vòng 1 (node gốc `ai/idea/`).

## Mục tiêu
Đọc xong, một người lạ kể lại được game trong một phút, và phân biệt được nó với game cùng thể loại.

## Viết gì
Vòng 1 quyết định game là gì:
- Thể loại và góc nhìn.
- Người chơi là ai.
- Cảm giác chính.
- Vòng lặp chơi cốt lõi: người chơi làm gì, được gì, rồi lặp lại thế nào.
- Điều kiện thắng và thua ở mức ý tưởng.

Mỗi ý cốt lõi là một quyết định định hình cả game; mọi vòng sau đều bám theo nó.

## Hướng màn hình (bắt buộc)
Phần đầu `data.md` của node gốc có dòng `screen: landscape` (màn ngang, 1920x1080) hoặc `screen: portrait` (màn dọc, 1080x1920). Mọi frame giao diện ở vòng 4 vẽ trên màn này.
- Dòng này không tính vào hash; đổi nó không bắt các node bên dưới làm lại, nhưng mọi frame giao diện phải rà lại theo màn mới.
- Thiếu dòng này thì `gd_status.py` nhắc ở node gốc, và frame giao diện tạm vẽ theo màn ngang.
- Chọn hướng màn khi chọn góc nhìn và người chơi; ghi lí do ở `CLAUDE.md` như mọi quyết định.

## Style hình ảnh (`.art-style/` của node gốc)
Style quyết định cảm nhận của cả game, nên là quyết định của vòng 1 và **tính vào hash data của node gốc**: thêm, sửa, bỏ một style hay đổi một ảnh tham chiếu thì mọi node bên dưới thành "cần làm lại". Chỉ sửa khi quyết định thật sự đổi.
- Mỗi style một file `<id>.md` (id: chữ thường, số, gạch nối). Dòng đầu `# <tên style>`, dưới đó:
  - dùng cho gì: UI, nhân vật, quái, nền, hiệu ứng;
  - góc nhìn, bảng màu, nét viền, độ chi tiết, ánh sáng;
  - làm gì và không làm gì (ví dụ "không gradient", "viền tối thiểu 3 px ở 1080p").
- Ảnh tham chiếu do người bỏ vào, đặt tên `<id>-<tên>.png|jpg|webp`. Ảnh không thuộc style nào là lỗi cấu trúc.
- Trước khi viết hay sửa style, chạy `python Management/tools/references.py` và Read các ảnh loại `style art` người nạp ở tab Tham chiếu (thư mục `Management/references`), cùng nhận định của người dưới mỗi ảnh: đó là hướng người muốn. Ảnh ở đó chỉ là gợi ý; style chính thức vẫn là `.art-style` của node gốc.
- Được mix style: mỗi style cho một nhóm thứ (ví dụ `ui-flat` cho giao diện, `chars-chibi` cho nhân vật). Order art khai một hay nhiều style.
- Chưa có style nào trong `live` thì không đặt được order art (xem skill `gd-art-concept`).

## Cụ thể tới mức nào
- Đúng: "Thủ thành: hero tự đánh, người chơi chọn nâng cấp giữa các đợt quái."
- Đúng: "Thua khi thành bị phá."

## Không cụ thể tới mức này (để vòng sau)
- Danh sách feature, tên popup, HUD (vòng 2).
- Định nghĩa và quan hệ (vòng 3). Sai: "HP = 0 thì hero chết."
- Thông số, con số, trường hợp biên (vòng 4). Sai: "Mỗi trận 5 hero."

## Không mơ hồ
Mỗi ý phải là một quyết định, không phải lời khen. Sai: "Game vui, dễ nghiện."

## `CLAUDE.md` của node
Các mục: Người chơi, Cảm giác chính, Vòng lặp chơi, Chưa rõ.

## Lưu ý
- **Bắt buộc hỏi GPT** về node gốc trước khi báo xong vòng 1: nhờ @DesignAdvisor bằng `python Management/tools/dispatch.py send @DesignAdvisor --from GameDesigner "<node> · <hỏi gì> · <ràng buộc của người nếu có>"`, rồi đưa mọi góp ý vào node (skill `gd-integrate-advice`).
- Đổi `data.md` của node gốc làm mọi node bên dưới thành "cần làm lại". Chỉ sửa khi quyết định thật sự đổi.
- Không tự mở rộng game ngoài điều người đã nêu; thiếu thông tin thì hỏi (User control) hoặc ghi ở "Chưa rõ" và "Đã tự quyết" (Full Auto).

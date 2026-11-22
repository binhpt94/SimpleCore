---
paths:
  - "Management/game-design/ai/idea/*.md"
  - "Management/game-design/live/*.md"
---
# GD vòng 1: Ý tưởng sơ khai
Luật chung: `.claude/rules/gamedesigner.md`. File này chỉ thêm phần riêng của vòng 1 (node gốc `ai/idea/`).

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
- Đổi `data.md` của node gốc làm mọi node bên dưới thành "cần làm lại". Chỉ sửa khi quyết định thật sự đổi.
- Không tự mở rộng game ngoài điều người đã nêu; thiếu thông tin thì hỏi (User control) hoặc ghi ở "Chưa rõ" và "Đã tự quyết" (Full Auto).

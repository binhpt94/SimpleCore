---
paths:
  - "Management/game-design/ai/idea/*/*"
  - "Management/game-design/live/*/*"
---
# GD vòng 2: Feature (tóm tắt)
Luật chung: `.claude/rules/gamedesigner.md`. File này chỉ thêm phần riêng của vòng 2 (`ai/idea/<feature>/`).

## Viết gì
- Chia ý tưởng vòng 1 thành các feature; mỗi feature chỉ ghi cốt lõi: nó là gì, người chơi được gì từ nó.
- Key bắt đầu bằng `feature-` (ví dụ `feature-hero`).

## Chưa viết
- Quan hệ, định nghĩa, danh sách, luật (vòng 3).
- Thông số, con số, trường hợp biên (vòng 4).

## Cách chia feature
HUD, popup, balance cắt ngang nhiều feature, nên không gộp chúng vào feature khác:
- Mỗi popup là một feature.
- HUD là một feature. HUD lớn thì mỗi cụm lớn tách thành một feature.
- Balance (cân bằng số liệu) là một feature.
- Tính năng nào bị cắt ngang nhiều feature cũng tách thành feature riêng.
- Feature khác nhắc tới chúng bằng đường dẫn node (ví dụ `feature-hud`), không mô tả lại nội dung của chúng.

## Feature hạ tầng mặc định
Vòng 2 luôn có ba node sau, với đúng key và `code.json` này (không cần hỏi người tên thư mục):

| Key | `code.json` | Là gì |
|---|---|---|
| `feature-scene-flow` | `{"unity": ["SceneFlow"]}` | Chuyển giữa các scene của game |
| `feature-session-scene` | `{"unity": ["_Services/SessionScene"]}` | Vòng đời một scene: tải, chạy, huỷ |
| `feature-popup` | `{"unity": ["_Services/Popup"]}` | Dịch vụ hiện popup. Khác với từng popup: mỗi popup là một feature riêng |

- Tạo ba node này cùng lúc chia feature, viết và chấm điểm như mọi feature khác, rồi đi tiếp vòng 3, 4.
- `gd_status.py` in mục "Thiếu feature hạ tầng" khi còn thiếu node hoặc `code.json` chưa đúng.
- Chúng bị khoá code như mọi feature cho tới khi nhánh của chúng có vòng 4 đã chốt trong `live`.

## Nối thiết kế với code (`code.json`)
- Node feature khai thư mục code thực hiện nó: `{"unity": ["Wallet"]}`, dịch vụ thì `{"unity": ["_Services/Popup"]}`. Tên đúng như thư mục trong `UnityProject/Assets/Features`; GD không đọc được thư mục đó nên hỏi người tên chính xác, không đoán (trừ ba feature hạ tầng ở trên).
- Mỗi thư mục code chỉ thuộc một node. `code.json` chỉ đặt ở vòng 2.
- Guard chỉ cho phiên của một feature sửa code khi node khai nó **đã chốt trong `live`** và nhánh đó có ít nhất một node vòng 4.
- Đổi `code.json` làm node "khác live" (phải chốt lại) nhưng không đổi hash data.

## `CLAUDE.md` của node
Các mục: Người chơi được gì, Liên quan, Chưa rõ.

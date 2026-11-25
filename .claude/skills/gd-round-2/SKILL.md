---
name: gd-round-2
description: Chỉ phiên FEATURE=@GameDesigner (@DesignAdvisor đọc để hiểu). Luật riêng của vòng 2 cây thiết kế, chia game thành feature ở ai/idea/<feature>/. Dùng trước khi viết hoặc sửa một node feature - cách chia feature, ba feature hạ tầng mặc định, code.json, hỏi GPT vòng 2.
---
# GD vòng 2: Feature (tóm tắt)
Luật chung: `.claude/rules/gamedesigner.md`. Skill này chỉ thêm phần riêng của vòng 2 (`ai/idea/<feature>/`).

## Mục tiêu
Danh sách feature phủ hết vòng lặp chơi của vòng 1, không feature nào trùng việc của feature khác, và mỗi feature đủ rõ để giao riêng cho một phiên.

## Viết gì
Vòng 2 quyết định game gồm những feature nào:
- Mỗi feature: nó là gì, người chơi được gì từ nó, nó liên quan tới feature nào.
- Key bắt đầu bằng `feature-` (ví dụ `feature-hero`).

## Cụ thể tới mức nào
- Đúng: "Hero: nhân vật tự chiến đấu giữ thành, mạnh lên qua nâng cấp."
- Đúng: "Popup thua: báo kết quả trận và cho chơi lại."

## Không cụ thể tới mức này (để vòng sau)
- Quan hệ, định nghĩa, danh sách, luật (vòng 3). Sai: "Hero có HP, một hero đánh nhiều quái."
- Thông số, con số, trường hợp biên (vòng 4).

## Không mơ hồ
Mỗi feature phải nói được nó làm gì. Sai: "Hệ thống hero đa dạng."

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
- Mọi node feature đều có `code.json` khai thư mục code thực hiện nó: `{"unity": ["Wallet"]}`, dịch vụ thì `{"unity": ["_Services/Popup"]}`. Không có `code.json` thì feature không bao giờ được code.
- **GD tự đặt tên, không hỏi người**: bỏ `feature-` ở đầu key, mỗi từ viết hoa chữ đầu rồi ghép lại (`feature-hero` → `Hero`, `feature-popup-lose` → `PopupLose`). Feature của game nằm trong `Features/` (không có `_Services/`); chỉ ba feature hạ tầng ở trên là dịch vụ. `gd_status.py` in mục "Thiếu code.json" kèm đúng nội dung cần ghi.
- Chỉ hỏi người khi tên suy ra trùng với node khác, không hợp lệ (gd_status báo), hoặc người đã muốn tên khác.
- Thư mục chưa có trong Unity cũng không sao: khi node feature đã ở `live`, @Manager chạy `scaffold.py` để tạo thư mục, và Setup full tạo thread cho feature đó. Cả hai dùng tên đề xuất theo key ngay cả khi node chưa có `code.json`, nên đừng đặt tên khác tên đề xuất. Code chỉ mở khi `code.json` đã ở `live`.
- Mỗi thư mục code chỉ thuộc một node. `code.json` chỉ đặt ở vòng 2.
- Guard chỉ cho phiên của một feature sửa code khi node khai nó **đã chốt trong `live`** và nhánh đó có ít nhất một node vòng 4.
- Đổi `code.json` làm node "khác live" (phải chốt lại) nhưng không đổi hash data.
- **Bắt buộc hỏi GPT** về từng node feature trước khi báo node đó xong. Thứ tự: nhờ `DesignAdvisor` hỏi **một câu chung về cả bộ feature** trước (trùng việc, thiếu feature, ranh giới), rồi mới hỏi từng feature; feature giao nhau thì nhờ hỏi kèm nhau (dòng `also:`). **Không tự thêm rào cho GPT** (ví dụ "không đề xuất feature mới"): ở vòng này GPT được đề xuất feature mới, gộp, tách; chỉ người mới đặt được rào, và khi đó ghi rõ là người yêu cầu. Feature mới theo GPT thì tạo node và khai `node-by: gpt@<id lần hỏi>`. Không chép thiết kế vào tin nhắn: lệnh tự gửi kèm bản đồ feature, node con, ý đã loại. Nhắn `DesignAdvisor`, rồi đưa mọi góp ý vào node (skill `gd-integrate-advice`). Không tự quyết bỏ qua; feature cũ chưa hỏi thì hỏi bù. `glossary` được miễn.
- Node `glossary` (bảng thuật ngữ chung) cũng ở vòng 2 nhưng không phải feature: không `code.json`, không node con. Cách viết ở mục "Bảng thuật ngữ chung" của `gamedesigner.md`.

## `CLAUDE.md` của node
Các mục: Người chơi được gì, Liên quan, Chưa rõ.

# Đề xuất: yêu cầu trong sổ của một feature nhưng việc thuộc feature khác

Người đề xuất: @Manager · 2026-11-25 · Chờ người duyệt

## Vấn đề
Bảng "Ai làm" của `feature-popup-revive/revive-order/order-values` giao phần lớn việc cho Run và Castle. Đó là các ý `offer-flag`, `popup-after-1s`, `fail-to-lose-now`, `held-clear`, `revive-anim`, `resume-after-anim` và các trường hợp biên về trận đứng yên. Nhưng `gd_req.py` xếp mọi yêu cầu của node này vào sổ của PopupRevive. Muốn ghi `done` thì test phải nằm trong `PopupRevive/Tests`.

PopupRevive chỉ test được phía popup: báo đúng lựa chọn, đóng popup trước rồi mới báo. Hành vi của Run và Castle thì chỉ test qua đồ giả được, nên không chứng minh code thật của Run và Castle chạy đúng. Ngược lại, Run và Castle có viết test cũng không ghi được vào sổ của PopupRevive.

Cùng loại vấn đề với `2026-11-25-gd-req-test-composition.md`: test đúng nằm ngoài thư mục `Tests` của feature sở hữu sổ.

## Đề xuất (chọn một)
- **A (đề xuất):** `gd_req.py <F> done` nhận thêm test nằm trong `Tests` của feature khác khi node ghi feature đó trong bảng "Ai làm". Phiên làm thật là bên ghi `done` vào sổ của F, ví dụ Run ghi `gd_req.py PopupRevive done ...`. @CodeLead vẫn `verify`. Có thể gộp với phương án A của đề xuất `_Composition/Tests`, thành luật chung "test ở feature hoặc `_Composition` có trách nhiệm".
- **B:** @GameDesigner tách các ý đó sang node của Run và Castle, để mỗi sổ chỉ chứa việc của chính feature đó. Cách này đổi node đã live, nên phải chốt lại.
- **C:** PopupRevive viết test qua đồ giả cho phần của Run và Castle, rồi người bỏ qua trên trang các yêu cầu không test được ở phía popup.

## Ảnh hưởng
PopupRevive hiện còn chờ khung từ @CodeLead, nên đề xuất này chưa chặn việc ngay. Nhưng khi có khung, khoảng một nửa trong 12 yêu cầu của `order-values` sẽ không đóng được nếu chưa có quyết định.

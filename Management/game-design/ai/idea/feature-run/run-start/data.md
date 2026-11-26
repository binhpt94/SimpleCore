---
based-on: 7150875035ad
refs: feature-run/run-score@3d29bb683b7c, feature-run/run-flow@8db5f61d9f6e, feature-scene-flow/scene-list@78ed2dcc0859
---
# Bắt đầu lượt

Lúc vào trận, mọi thứ của lượt trước bị bỏ và trận bắt đầu từ trạng thái ban đầu với đội vừa chọn. Feature này ra lệnh đặt lại; mỗi feature tự giữ trạng thái ban đầu của mình.

## Ý cốt lõi

### start-reset-list | AI 8 | Đầu lượt, máu thành, tiền, mọi cấp nâng cấp và số đợt về ban đầu
Thành đầy máu, tiền về mức ban đầu, mọi hero và thành về cấp ban đầu, chưa dọn đợt nào. Kĩ năng thành cũng về trạng thái ban đầu. Cơ hội hồi sinh thành của lượt có lại. Đã loại: giữ lại một phần tiền hay nâng cấp từ lượt trước (vòng 1 đã loại tiến trình lâu dài).

### start-team | AI 8 | Đội vào trận là đúng đội vừa chọn ở màn chọn đội
Cả đội sinh ra ở thành khi trận bắt đầu (`feature-castle`, `feature-hero`); trong trận không đổi đội, không thêm hero.

### start-keep-record | AI 8 | Kỉ lục là thứ duy nhất của game không bị đặt lại khi bắt đầu lượt
Kỉ lục nằm ở `feature-run/run-score`.

### start-owner | AI 7 | Mỗi feature tự giữ giá trị ban đầu của mình; feature này chỉ báo "lượt mới bắt đầu"
Giá trị ban đầu (máu, tiền, cấp) do `feature-balance` chỉnh. Đã loại: feature này giữ hộ trạng thái của các feature khác (dễ lệch, trùng việc).

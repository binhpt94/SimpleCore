---
based-on: 46d96a55a205
refs: feature-run/run-flow/flow-rules@c2c5ba06ffa8, feature-castle/castle-core/castle-body-values@b648ec300023
frame: none
---
# Thời điểm từng bước của luồng hồi sinh

Chốt mốc thời gian của luồng hồi sinh: chờ bao lâu trước khi popup mở, thành hiện lại trong bao lâu, khi nào trận chạy tiếp, và tin dọn sạch đợt được giữ ra sao.

## Ý cốt lõi

### popup-after-1s | AI 7 | Popup hồi sinh mở sau 1 giây kể từ lúc trận dừng, cùng độ trễ với popup thua
Trong 1 giây này thành thu nhỏ còn 0 (0.5 giây) và không có gì bấm được.

### revive-anim | AI 7 | Hồi sinh: thành phóng từ 0 lên đủ cỡ trong 0.5 giây, máu đặt 30% máu tối đa ngay đầu 0.5 giây đó

### resume-after-anim | AI 7 | Trận chạy tiếp ngay khi thành hiện xong, không đếm ngược
Quái đứng sát thành đánh lại ngay; không có lúc bất tử.

### fail-to-lose-now | AI 7 | Không hồi sinh hoặc quảng cáo lỗi: chốt điểm và hiện popup thua ngay, không chờ thêm 1 giây

### held-clear | AI 6 | Tin dọn sạch đợt tới cùng khung hình thành bị phá được giữ; hồi sinh xong thì cộng điểm và bắt đầu lúc thở 3 giây
Chốt thua thì bỏ tin, không cộng điểm, không thưởng tiền dọn đợt.

### offer-flag | AI 7 | Lượt chơi giữ một cờ "đã đề nghị hồi sinh" bật ngay lúc mở popup; cờ chỉ tắt khi lượt mới bắt đầu
Lần bị phá sau khi cờ đã bật vào thẳng popup thua.

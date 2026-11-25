---
based-on: d9d4c35f6907
refs: feature-wave/wave-clear/clear-rules@b10f4f27eaf5, feature-wave/difficulty-scaling/wave-formula@4ec2cc7f93ed, feature-wave/spawning/spawn-order@dbef5244fdeb, feature-hud/hud-status/status-spec@339ea3c8f938
frame: none
---
# Đồng hồ lúc thở

Lúc thở dài đúng 3 giây, đếm từ lúc dọn sạch đợt; hết 3 giây thì đợt kế tiếp tự bắt đầu với con đầu tiên ra ngay. Trong 3 giây đó mọi thứ khác chạy như thường, chỉ không có quái mới.

## Ý cốt lõi

### gap-length | AI 7 | Lúc thở dài 3 giây, như nhau ở mọi đợt
Ngắn hơn hẳn thời gian thả của đợt ngắn nhất (đợt 1: 7 s). Đủ để thấy ranh giới đợt và bấm một nâng cấp.

### gap-start-next | AI 8 | Hết 3 giây thì đợt sắp tới bắt đầu thả quái, con đầu tiên ra ở 0 giây của đợt mới
Không cần người chơi bấm. Số đợt không đổi ở lúc này: nó đã thành đợt sắp tới ngay lúc dọn sạch (`gap-next-number`).

### gap-next-number | AI 8 | Dọn sạch đợt w thì số đợt thành w + 1 ngay khung hình đó; suốt 3 giây thở, số đợt là đợt sắp tới
Đây là số `feature-hud` hiện ở ô đợt. Điểm của lượt vẫn tính theo đợt vừa dọn (`feature-wave/wave-clear/clear-rules`).

### gap-pause-freeze | AI 7 | Tạm dừng trong lúc thở thì đồng hồ 3 giây đứng yên, bỏ tạm dừng thì đếm tiếp phần còn lại
Ví dụ dừng ở giây 1.2 thì sau khi tiếp tục còn 1.8 giây.

### gap-same-rules | AI 7 | Trong lúc thở: mua nâng cấp, dùng kĩ năng thành, hồi chiêu, hồi sinh hero 8 giây vẫn chạy; máu thành không hồi
Node này không đổi hành vi hero, thành hay nâng cấp; chỉ không thả quái mới.

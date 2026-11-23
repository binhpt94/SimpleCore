---
based-on: d9d4c35f6907
refs: feature-wave/wave-clear/clear-rules@b10f4f27eaf5, feature-wave/difficulty-scaling/wave-formula@4ec2cc7f93ed, feature-wave/spawning/spawn-order@d71ff27687b4
---
# Đồng hồ lúc thở

Lúc thở dài đúng 3 giây, đếm từ lúc dọn sạch đợt; hết 3 giây thì đợt kế tiếp tự bắt đầu với con đầu tiên ra ngay. Trong 3 giây đó mọi thứ khác chạy như thường, chỉ không có quái mới.

## Ý cốt lõi

### gap-length | AI 7 | Lúc thở dài 3 giây, như nhau ở mọi đợt
Ngắn hơn hẳn thời gian thả của đợt ngắn nhất (đợt 1: 7 s). Đủ để thấy ranh giới đợt và bấm một nâng cấp.

### gap-start-next | AI 8 | Hết 3 giây thì số đợt tăng 1 và đợt mới bắt đầu, con đầu tiên ra ở 0 giây của đợt mới
Không cần người chơi bấm.

### gap-pause-freeze | AI 7 | Tạm dừng trong lúc thở thì đồng hồ 3 giây đứng yên, bỏ tạm dừng thì đếm tiếp phần còn lại
Ví dụ dừng ở giây 1.2 thì sau khi tiếp tục còn 1.8 giây.

### gap-same-rules | AI 7 | Trong lúc thở: mua nâng cấp, dùng kĩ năng thành, hồi chiêu, hồi sinh hero 8 giây vẫn chạy; máu thành không hồi
Node này không đổi hành vi hero, thành hay nâng cấp; chỉ không thả quái mới.

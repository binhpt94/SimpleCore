---
based-on: 331cbd30e30e
refs: feature-run/run-flow/flow-rules@f011f018b6c5, feature-wave/wave-gap/gap-timer@4d5cfe278549
frame: none
---
# Luật tạm dừng

Khi nào trận vào và ra khỏi tạm dừng, thứ gì đứng yên, thứ gì bị chặn, và các tình huống tạm dừng chồng lên thua hay rời game.

## Ý cốt lõi

### pause-enter | AI 7 | Trận vào tạm dừng khi bấm nút tạm dừng, bấm nút quay lại của máy, hoặc game rời khỏi màn hình
Chỉ áp dụng khi trận đang đánh. Đang tạm dừng hay lượt đã kết thúc thì ba cách này không làm gì.

### pause-freeze-all | AI 7 | Lúc tạm dừng thời gian trận đứng hẳn: quái, hero, đạn, hồi sinh, hồi chiêu 20s/35s/25s, nghỉ 3s giữa đợt
Mọi bộ đếm giữ nguyên giá trị lúc bắt đầu dừng; tiếp tục là chạy tiếp đúng từ giá trị đó.

### pause-block-input | AI 7 | Lúc tạm dừng không mua nâng cấp và không dùng kĩ năng, kể cả khi bấm lọt qua
Popup che chặn bấm; ngoài ra `feature-upgrade` và `feature-castle` tự từ chối thao tác khi trận đang tạm dừng.

### pause-resume-instant | AI 7 | Bấm tiếp tục thì trận chạy lại ngay trong khung hình đó, không đếm ngược
Đã loại: đếm ngược 3 giây (thêm chờ cho người chơi phổ thông).

### pause-return-stays | AI 6 | Quay lại game sau khi rời thì trận vẫn tạm dừng, popup tạm dừng đang mở chờ bấm

### pause-quit-ends | AI 7 | Đồng ý bỏ lượt thì lượt kết thúc với điểm bằng số đợt đã dọn sạch, đợt đang dở không tính
Sau đó về màn bắt đầu (`feature-run/run-flow/flow-rules`).

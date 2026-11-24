---
based-on: 53af9956f851
by: scene-boot=gpt@r2-1#boot-vao-man-bat-dau, scene-switch=gpt@r2-1#ranh-gioi-chuyen-scene
---
# Chuyển scene

Hạ tầng chuyển giữa các scene của game: từ màn tải lúc mở game sang scene chơi, và giữa các scene khác nếu có.

## Ý cốt lõi

### scene-boot | AI 8 | Mở game thì người chơi qua màn tải rồi tới màn bắt đầu
Không vào thẳng trận: màn bắt đầu là nơi xem kỉ lục và bấm Chơi để sang màn chọn đội.

### scene-switch | AI 7 | Feature khác xin chuyển scene qua feature này; tải, chạy, huỷ từng scene thuộc `feature-session-scene`
Không feature nào tự tải scene, và việc tải không bị giao cho cả hai feature.

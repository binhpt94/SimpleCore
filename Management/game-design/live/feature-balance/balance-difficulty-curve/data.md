---
based-on: 33792179d7fe
refs: feature-enemy/enemy-list@10b291cde6dc, feature-wave/difficulty-scaling@fed0ac86d57c, feature-balance/balance-economy@5fe05cebab72, glossary#break-wave@5e6edbfc4988
---
# Đường độ khó

Quan hệ giữa sức quái theo đợt và sức đội người chơi mua được bằng tiền của lượt. Sức quái tăng nhanh hơn, nên lượt nào cũng có một đợt gãy; chơi khéo thì gãy muộn hơn.

## Ý cốt lõi

### curve-outpace | AI 8 | Sức quái tăng theo đợt nhanh hơn sức đội mua được, nên lượt nào rồi cũng thua
Chơi vô tận cần điểm kết thúc. Đã loại: hai bên tăng ngang nhau (lượt kéo dài mãi); có trần độ khó.

### curve-early-easy | AI 7 | Các đợt đầu đội đã chọn trụ được mà chưa cần nâng cấp nhiều; áp lực tăng dần sau đó
Người chơi phổ thông làm quen trước khi phải tính chi tiền. Đã loại: khó ngay từ đợt đầu.

### curve-smooth | AI 7 | Áp lực tăng đều qua các đợt; ngoại lệ có chủ đích duy nhất là đợt có Trùm theo chu kì khó vọt hơn, rồi đợt sau dịu lại
Thua thì thấy được mình yếu dần ở đâu, không phải bị bất ngờ. Đợt Trùm là đỉnh căng thẳng người chơi biết trước (`feature-enemy/enemy-list`). Đã loại: đợt mốc khó vọt không báo trước, hay nhiều loại đợt đặc biệt khác.

### curve-skill-matters | AI 7 | Chọn đội, chọn nâng cấp và canh kĩ năng thành khéo thì đợt gãy tới muộn hơn rõ rệt
Đây là lí do chơi lại để phá kỉ lục. Đã loại: điểm gãy gần như cố định dù chơi thế nào.

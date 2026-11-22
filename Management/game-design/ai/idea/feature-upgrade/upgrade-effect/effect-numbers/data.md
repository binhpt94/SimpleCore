---
based-on: b9bcec737959
---
# Mức tăng mỗi cấp

Mỗi cấp của từng mục làm hero hoặc thành mạnh thêm bao nhiêu. Mọi mức tăng tính theo chỉ số cấp 1, cộng dồn đều theo cấp, có hiệu lực ngay khung hình mua xong.

## Ý cốt lõi

### hero-plus-15 | AI 8 | Mỗi cấp hero cộng 15% máu tối đa và 15% sát thương của cấp 1
Hero cấp L có máu và sát thương = chỉ số cấp 1 × (1 + 0,15 × (L − 1)). Ví dụ cấp 5 là 160% cấp 1.

### hero-speed-3 | AI 7 | Mỗi cấp hero cộng 3% tốc độ đánh của cấp 1, cộng tối đa 60%
Chạm trần ở cấp 21; từ đó lên cấp chỉ còn tăng máu và sát thương.

### hero-hp-gain | AI 7 | Hero đang sống lên cấp thì máu hiện tại tăng thêm đúng phần máu tối đa vừa tăng (15% máu cấp 1)
Ví dụ hero cấp 1 máu tối đa 200, đang còn 80: lên cấp 2 thành tối đa 230, hiện tại 110.

### hero-fallen | AI 8 | Hero đang gục lên cấp thì chỉ đổi cấp, hồi sinh với 100% máu tối đa theo cấp mới
Thời gian chờ hồi sinh không đổi.

### castle-hp-150 | AI 8 | Mỗi cấp máu thành cộng 150 máu tối đa và 150 máu hiện tại
Thành cấp 1 có 1000 máu tối đa; cấp L có 1000 + 150 × (L − 1).

### castle-attack-20 | AI 8 | Mỗi cấp sức đánh thành cộng 20% sát thương đòn tự bắn của cấp 1
Cấp 1 bắn 10 sát thương mỗi 1,0 giây; cấp L bắn 10 × (1 + 0,2 × (L − 1)). Nhịp bắn và tầm không đổi.

### castle-skill-20 | AI 7 | Mỗi cấp kĩ năng thành cộng 20% hiệu lực cấp 1 cho cả ba kĩ năng, hồi chiêu không đổi
Sấm sét: 80 sát thương cộng 16 mỗi cấp. Băng giá: chậm 50% trong 5 giây, thời gian chậm cộng 1 giây mỗi cấp. Cổ vũ: hồi 30% máu tối đa hero, cộng 6 điểm phần trăm mỗi cấp.

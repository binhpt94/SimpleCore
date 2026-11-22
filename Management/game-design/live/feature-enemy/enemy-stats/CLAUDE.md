# Chỉ số quái

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- MỘT quái có MỘT bộ năm chỉ số, lấy theo loại của nó.
- NHIỀU quái cùng loại có cùng bộ chỉ số gốc.
- Mức tăng theo đợt (nếu có) do `feature-wave` quyết và áp lên quái khi nó ra; giá trị gốc do `feature-balance`.

## Định nghĩa
- Máu, sức đánh, tốc độ đánh, tầm đánh, tốc độ di chuyển: như `feature-hero/hero-stats`.
- **Bị hạ**: máu quái về 0.
- **Chỉ số gốc**: giá trị chỉ số của một loại quái trước khi `feature-wave` làm mạnh lên theo đợt.

## Danh sách
- Năm chỉ số: máu, sức đánh, tốc độ đánh, tầm đánh, tốc độ di chuyển.
- Trạng thái của quái: còn sống, bị hạ.

## Luật cơ bản
- Quái ra với đầy máu.
- Bị đánh trúng: máu giảm đúng bằng sức đánh của bên đánh (hero, thành, kĩ năng thành).
- Quái không tự hồi máu.
- Chỉ số của quái không đổi trong lúc sống, trừ khi bị làm chậm bởi Băng giá (`feature-castle/castle-skills`).
- Máu về 0: quái bị hạ, biến mất, cho tiền (`feature-enemy/enemy-reward`).

## Đã tự quyết
- **Dùng định nghĩa chỉ số của `feature-hero/hero-stats`, không viết lại.** Một bộ từ cho cả game.
- **Quái không có cấp.** Quái sống ngắn; cho quái lên cấp là thêm luật người chơi không thấy. Đã loại: lên cấp theo thời gian.
- **Độ khó theo đợt để ở `feature-wave`.** Tránh hai feature cùng giữ việc làm quái mạnh lên.

## Chưa rõ
- Đợt sau khó hơn bằng cách tăng chỉ số quái hay chỉ tăng số lượng và loại: việc của `feature-wave`; node này chỉ cần biết có thể có mức tăng áp khi quái ra.

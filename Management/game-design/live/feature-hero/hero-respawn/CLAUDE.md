# Hero vào trận, gục và hồi sinh

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- MỘT đội vào trận MỘT lần, cả đội cùng lúc, tại MỘT thành.
- MỘT hero gục thì MỘT lần chờ hồi sinh; NHIỀU hero có thể cùng đang chờ.
- Đội trong trận không thêm, không bớt hero; gục chỉ là tạm rời trận.

## Định nghĩa
- **Gục**: hero máu về 0, tạm rời chiến trường.
- **Thời gian chờ**: khoảng từ lúc gục tới lúc hồi sinh.
- **Hồi sinh**: hero xuất hiện lại ở thành, đầy máu, cùng cấp như trước khi gục.

## Danh sách
- Trạng thái vòng đời của hero trong trận: trên chiến trường, gục (đang chờ), hồi sinh (trở lại trên chiến trường).

## Luật cơ bản
- Vào trận: mọi hero trong đội sinh ra ở thành, đầy máu, cấp ban đầu.
- Máu về 0: hero gục, rời chiến trường, bắt đầu chờ.
- Hết thời gian chờ: hero hồi sinh ở thành, đầy máu, rồi tự đánh tiếp (`feature-hero/hero-targeting`).
- Mọi hero cùng một thời gian chờ, không đổi theo cấp hay theo loại hero.
- Hero đang gục vẫn được mua cấp; cấp mới có hiệu lực khi hồi sinh (`feature-upgrade`).
- Hero không tự hồi máu; chỉ hồi sinh hoặc kĩ năng Cổ vũ của thành làm hero có thêm máu.
- Hero gục không làm thua; chỉ thành bị phá mới thua (`feature-castle/castle-core`).
- Thưởng-phạt: gục thì đội thiếu người trong lúc chờ; không mất cấp, không mất tiền.

## Đã tự quyết
- **Thời gian chờ như nhau cho mọi hero.** Dễ hiểu, người chơi không bị phạt vì nâng hero. Đã loại: chờ lâu hơn theo cấp, chờ khác nhau theo loại hero.
- **Hồi sinh đầy máu ở thành.** Thành là điểm hồi sinh (`feature-castle/castle-core`). Đã loại: hồi sinh tại chỗ gục.
- **Hero không tự hồi máu.** Cho kĩ năng Cổ vũ có giá trị và giữ áp lực trong một đợt dài. Đã loại: tự hồi theo thời gian, hồi đầy giữa hai đợt.
- **Gục không mất gì ngoài thời gian.** Đúng quyết định chung (giữ nguyên cấp).

## Chưa rõ
- Không có điểm chặn.

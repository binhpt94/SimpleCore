# Độ khó theo đợt

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- MỘT số đợt cho ra MỘT mức độ khó; mức độ khó quyết danh sách quái mà `feature-wave/spawning` thả.
- Mức độ khó nhân lên chỉ số gốc của từng loại quái (`feature-enemy`); node này không giữ chỉ số gốc.
- Mức độ khó tăng nhanh cỡ nào so với sức người chơi mua được là của `feature-balance` (`feature-balance/balance-difficulty-curve`).
- Thưởng dọn sạch đợt là của `feature-wave/wave-clear`, không phải node này.

## Định nghĩa
- **Số đợt**: thứ tự của đợt trong lượt, bắt đầu từ đợt đầu tiên.
- **Mức độ khó của đợt**: các giá trị tính từ số đợt: số quái, nhịp thả, hệ số máu, hệ số sức đánh, các loại quái được ra, hệ số tiền mỗi con.
- **Chỉ số gốc**: chỉ số của một loại quái trước khi nhân theo đợt, nằm ở `feature-enemy`.
- **Loại đã mở**: loại quái được phép có trong đợt từ một số đợt trở đi.

## Danh sách
Những thứ tăng theo số đợt:
- Số quái của đợt.
- Độ dày nhịp thả.
- Máu của quái.
- Sức đánh của quái.
- Số loại quái đã mở và phần của loại khó trong đợt.
- Tiền khi hạ một con quái.

Những thứ không đổi theo số đợt (giữ theo loại quái): tốc độ đánh, tầm đánh, tốc độ di chuyển.

## Luật cơ bản
- Số đợt lớn hơn thì mọi thứ trong danh sách tăng không giảm; đợt sau khó hơn đợt trước.
- Tăng đều, không có bước nhảy đột ngột giữa hai đợt liền nhau.
- Ngoại lệ có chủ đích: đợt có Trùm (theo chu kì) khó vọt hơn đợt trước, rồi đợt sau dịu lại. Đây là đỉnh căng thẳng, không phải lỗi tăng.
- Đợt đầu dễ, đủ để người chơi làm quen trước khi phải mua nâng cấp.
- Loại quái đã mở thì giữ mở tới hết lượt.
- Mọi quy tắc tăng dùng tham số trong cấu hình của `feature-wave`; giá trị do `feature-balance` quyết.

## Đã tự quyết
- **Độ khó chỉ theo số đợt.** Dễ cân bằng, điểm (số đợt dọn sạch) so được giữa các lượt. Đã loại: độ khó thích ứng theo sức đội.
- **Tốc độ, tầm không tăng theo đợt.** Giữ cách nhận ra loại quái bằng mắt. Đã loại: tăng mọi chỉ số.
- **Tiền mỗi con tăng theo đợt, nằm ở đây.** Đây là nơi duy nhất biết số đợt khi quái ra; `feature-enemy` giữ tiền gốc. Đã loại: tiền cố định.
- **Đợt Trùm là ngoại lệ có chủ đích về độ khó (2026-11-23, quyết định đã chốt).** Thay cho mục cũ "không có đợt đặc biệt": `feature-enemy/enemy-list` đã có Trùm. Đợt sau khó hơn đợt trước, trừ đợt có Trùm theo chu kì khó vọt hơn rồi đợt sau dịu lại. Không có đợt đặc biệt nào khác. Đã loại: bỏ Trùm để giữ đường tăng phẳng.
- **Viết lại theo luật vòng 3 mới (2026-11-23).** Bỏ công thức và con số (số quái gốc, phần trăm tăng, mốc đợt mở loại), bỏ trường hợp biên (chạm trần, làm tròn, đợt rất cao), bỏ dẫn tới màn nghỉ đã xoá.

## Chưa rõ
- Kiểu tăng (đều theo đợt hay nhanh dần): vòng 4, cùng `feature-balance`.

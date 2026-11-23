# Độ khó theo đợt

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- MỘT số đợt cho ra MỘT mức độ khó; mức độ khó quyết danh sách quái mà `feature-wave/spawning` thả.
- Mức độ khó nhân lên chỉ số gốc của từng loại quái (`feature-enemy`); node này không giữ chỉ số gốc.
- Mức độ khó tăng nhanh cỡ nào so với sức người chơi mua được là của `feature-balance` (`feature-balance/balance-difficulty-curve`).
- Thưởng dọn sạch đợt là của `feature-wave/wave-clear`, không phải node này.

## Định nghĩa
- `glossary#base-stats` (nằm ở `feature-enemy`)
- `glossary#unlocked-type`

## Danh sách
Những thứ tăng theo số đợt:
- Số quái của đợt.
- Độ dày nhịp thả.
- Máu của quái.
- Sức đánh của quái.
- Số loại quái đã mở và phần của loại khó trong đợt.
Những thứ không đổi theo số đợt (giữ theo loại quái): tốc độ đánh, tầm đánh, tốc độ di chuyển, tiền khi hạ một con quái.

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
- **Tiền mỗi con giữ nguyên qua các đợt (2026-11-23, sửa lệch).** Bản cũ cho tiền mỗi con tăng theo đợt, trái với các node vòng 4 đã viết (mức tiền khi hạ quái của `feature-enemy`, công thức đợt ở node con `wave-formula`, bảng số và giá của `feature-balance`, mức tiền thu của `feature-wallet`): tất cả đều giữ tiền mỗi con cố định. Thu nhập tăng nhờ đông quái và loại đắt hơn. Đã loại: nhân tiền theo đợt.
- **Đợt Trùm là ngoại lệ có chủ đích về độ khó (2026-11-23, quyết định đã chốt).** Thay cho mục cũ "không có đợt đặc biệt": `feature-enemy/enemy-list` đã có Trùm. Đợt sau khó hơn đợt trước, trừ đợt có Trùm theo chu kì khó vọt hơn rồi đợt sau dịu lại. Không có đợt đặc biệt nào khác. Đã loại: bỏ Trùm để giữ đường tăng phẳng.
- **Viết lại theo luật vòng 3 mới (2026-11-23).** Bỏ công thức và con số (số quái gốc, phần trăm tăng, mốc đợt mở loại), bỏ trường hợp biên (chạm trần, làm tròn, đợt rất cao), bỏ dẫn tới màn nghỉ đã xoá.

## Chưa rõ
- Kiểu tăng (đều theo đợt hay nhanh dần): vòng 4, cùng `feature-balance`.

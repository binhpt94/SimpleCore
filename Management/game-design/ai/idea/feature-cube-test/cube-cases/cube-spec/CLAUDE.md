# Ba khối thử: cỡ, chỗ, góc và màu

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Khi màn trận hiện: dựng ba khối lập phương 3D trên mặt đất ở x=+1.5u, −5u, +5u, mỗi khối một chỗ trong thứ tự vẽ (`cube-cases`).
- Mỗi khung hình: xoay mỗi khối quanh trục đứng theo thời gian thực.
- Rời màn trận: bỏ cả ba khối.
- Không gửi, không đọc gì của feature khác. Không lưu gì. Không có vùng chạm, không chặn hero hay quái.

## Thông số
Bảng Excel chưa tạo được: lệnh `gd_excel.py` cần thư viện `openpyxl`, máy chưa cài. Khi cài xong thì chuyển bảng này sang `.excel/` (bảng `Params`, kind `params`).

| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Cạnh khối | 1 | u (60 px) | Rộng bằng hộp hero ở `feature-hero/hero-list/hero-roster-values` |
| Tâm khối sau thành | +1.5 | u | Mép phải thành ở `feature-castle/castle-core/castle-body-values`: thành che nửa khối |
| Tâm khối giữa thành và unit | −5 | u | Trên đường quái cánh trái, giữa cỏ (−4u) và đá (−6.5u) |
| Tâm khối trước unit | +5 | u | Trên đường quái cánh phải, đối xứng khối giữa |
| Điểm thấp nhất của khối | 0 | u (y) | Đứng trên mặt đất |
| Góc nghiêng về phía camera | 20 | độ | Thấy mặt trên |
| Góc quay sẵn quanh trục đứng | 45 | độ | Hai mặt bên rộng bằng nhau lúc mở |
| Tốc độ xoay | 30 | độ/giây | Một vòng 12 s; dự kiến |
| Hướng đèn | từ trên, nghiêng 30° về phía camera | độ | Mặt trên sáng nhất, theo style `world-cartoon` |
| Màu thân | #FFC61F | | Vàng của bảng màu chung (`ui-flat`) |
| Màu nhắm tới mặt trên | #FFE780 | | Vùng sáng của Vàng |
| Màu nhắm tới mặt bên tối | #F29A0E | | Bóng của Vàng |
| Màu viền | #C26A00 | | Viền của Vàng |
| Dày viền ngoài | 3 | px trên màn 1920×1080 | Dự kiến; gần độ dày viền sprite ở cỡ này |
| Bóng: rộng × cao | 1.2 × 0.25 | u | Rộng hơn chân khối một chút |
| Bóng: màu, độ đục | #3B1E4F, 30% | | Chữ tối của bảng màu chung; dự kiến |
| Độ tối khi có popup | 60% | | Nền che của `feature-popup/popup-layer/stack-rules` |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Ba khối thử trên màn trận | Có, đủ các ý của node | như IDEA | như IDEA |
| Model khối | Khối lập phương dựng sẵn của engine, vật liệu theo bảng màu | như IDEA | như IDEA |

## Trường hợp biên
- **Popup mở (tạm dừng, hồi sinh, thua):** khối vẫn vẽ dưới nền che, vẫn xoay; đóng popup thì như cũ.
- **Trận đứng yên:** khối vẫn xoay vì xoay theo thời gian thực, không theo nhịp trận.
- **Hero chỗ 2 đứng ở x=+1.5u:** hero vẽ trước khối sau thành, đúng thứ tự.
- **Hiệu ứng kĩ năng rơi đúng chỗ khối trước unit:** hiệu ứng vẽ đè lên khối.
- **Thành bị phá (thu nhỏ về 0):** khối sau thành lộ ra hết; đúng, không xử lí riêng.
- **Hồi sinh thành rồi chơi tiếp:** vẫn là màn trận đó, khối giữ góc xoay đang có.
- **Thua rồi chơi lại:** màn trận mở lại, khối về góc quay sẵn 45°.
- **Màn rộng hơn 21:9:** khối vẫn ở đúng toạ độ u; nền phủ tới ±21u nên không hở.

## Đã tự quyết
- **Khối dựng sẵn của engine** thay vì order @Blender: lập phương là hình đơn giản nhất, không cần model. Nếu người muốn thử model low poly thật thì feature đặt order @Blender sau khi mở khoá code.
- **Đèn riêng cho khối:** sprite 2D không cần đèn; đèn chỉ cho khối để mặt trên sáng hơn mặt bên.
- **Màu Vàng:** nổi trên nền xanh, khác tông phe quái, có đủ bốn sắc trong bảng màu chung để so.
- **Không có node cheat:** feature không có UI, frame chỉ là `kind: field`; ba khối hiện ngay khi vào trận, popup tạm dừng mở được bằng nút sẵn có.

## Chưa rõ
- Bảng Excel chờ người cài `openpyxl` (`python -m pip install openpyxl`).

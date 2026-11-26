# Thông số các lớp nền

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Khi màn trận dựng (bước chạy của scene Game, cùng lúc trận được dựng): tạo bốn lớp nền theo bảng dưới, ở các lớp vẽ sau thành, quái, hero.
- Khi rời màn trận: huỷ cùng trận.
- Không đọc gì từ trận; không đổi gì trong suốt trận.

## Thông số
Bảng `LayerParams` (loại `params`, `.excel/`): quy đổi và khung camera, phủ ngang, trời, hai dải cảnh xa (đỉnh, chân, cỡ hình, màu gốc, độ nhạt), dải đất, decor phẳng, decor đứng (số món, tâm x, cỡ, chân, độ nhạt, màu), thứ tự vẽ, hai đường HUD, đổi nền theo đợt.
- Vị trí tính bằng u (1u = 60 px), tâm x ghi cho một bên; bên kia đối xứng qua x=0.
- **Độ nhạt** là phần trăm pha màu gốc về màu trời (0% là màu gốc, 100% là trùng màu trời). Màu gốc lấy tên màu trong bảng màu chung của style `ui-flat` ở node gốc, không đặt mã màu riêng.

## Thông số theo giai đoạn
Ba cột `idea`, `prototype`, `full` của bảng `LayerParams`. Ở PROTOTYPE:
- **Hai dải cảnh xa:** dải sau (núi đồi xanh dương, hình nhỏ 0.6 lần, nhạt 45%, đỉnh 3.2u) ló lên sau dải trước (đồi cây xanh lá, nhạt 20%, đỉnh 2u). Cả hai thấp hơn thành 4u để thành vẫn nổi giữa màn.
- **Decor đứng:** 8 món (mỗi bên 2 cờ, 2 hàng rào), đứng trên mặt đất nhưng vẽ trước dải đất, cùng độ nhạt với dải cảnh xa trước, nên đọc là cảnh phía xa, không phải vật cản. Tâm x nằm giữa các decor phẳng và ngoài khoảng ±3u quanh thành.
- Trời, cảnh xa, dải đất chuyển sang art @Codex theo style Colorful.

## Trường hợp biên
- **Màn rộng hơn 21:9:** hở hai mép ngoài ±21u; không hỗ trợ tỉ lệ đó. Màn 4:3 hẹp hơn: thấy ít hơn ±16u, nền vẫn phủ.
- **Unit đứng trên decor:** decor vẽ sau, unit che decor; không có va chạm.
- **Thành bị phá:** nền không đổi gì.
- **Rời màn trận rồi vào lại:** nền dựng lại như cũ (một bản đồ).

## Đã tự quyết
- **Bám camera đang có, không đặt camera mới.** Số của @CodeLead đo từ scene Game lúc Play; chân thành, hero, quái đã ở px 840. Đã loại: dời mặt đất để chừa nhiều đất hơn (phải sửa camera và mọi ảnh đã đối chiếu).
- **Decor ở IDEA chỉ có cỏ và đá phẳng.** Theo `field-decor-flat`: trên đường đi chỉ hoạ tiết phẳng. Cờ và hàng rào (decor đứng) đưa vào dải cảnh xa từ PROTOTYPE, để không trông như vật cản. Đã loại: cờ cắm cạnh thành ở IDEA (dễ nhầm là vật cản, chồng chỗ hero xuất phát).
- **"Parallax" hiểu là cảnh xa có độ sâu.** Camera đứng yên nên không có chuyển động để tạo parallax; PROTOTYPE dùng hai dải cảnh xa khác cỡ, khác độ đậm. Đã loại: cho cảnh xa tự trôi (dễ tưởng là chuyển động của trận).
- **Số PROTOTYPE của cảnh xa và decor đứng (2026-11-26, @Manager nhờ khi vào PROTOTYPE, phiên Battlefield báo thiếu).** Chuyển bảng markdown sang bảng `LayerParams` (luật Excel khi sửa số), giữ nguyên mọi số IDEA. Tự quyết theo style Colorful:
  - Độ sâu bằng cỡ và độ nhạt, không bằng chuyển động (camera đứng yên). Dải sau nhạt hơn dải trước một bậc rõ (45% so với 20%); nền nhạt hơn unit để viền unit đủ tương phản (ý `art-unit-outline-contrast` của gốc).
  - Màu lấy tên trong bảng màu chung (xanh lá cho đồi gần, xanh dương cho núi xa, hồng và vàng cho cờ, kem cho hàng rào), không thêm mã màu mới, để không phải sửa node gốc.
  - Decor đứng thuộc lớp cảnh xa: vẽ sau dải cảnh xa trước, trước dải đất. Đã loại: đặt cờ, hàng rào trên dải đất (dễ đọc là vật cản trên đường đi, trái `field-decor-flat`).
  - Mọi số PROTOTYPE mới là dự kiến; chỉnh khi xem art @Codex trên khung trận.
- **Hai đường HUD trên frame chỉ là nét đo.** Số px của HUD vẫn là của `feature-hud`; frame ghi lại để kiểm nền không bị che sai chỗ.
- **Bỏ tạm dừng, quảng cáo và popup hồi sinh (2026-11-26, người quyết).** Bỏ tạm dừng và hồi sinh thành khỏi trường hợp biên của nền.

## Chưa rõ
- Không có điểm chặn.

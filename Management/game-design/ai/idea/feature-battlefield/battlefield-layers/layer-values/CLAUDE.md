# Thông số các lớp nền

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Khi màn trận dựng (bước chạy của scene Game, cùng lúc trận được dựng): tạo bốn lớp nền theo bảng dưới, ở các lớp vẽ sau thành, quái, hero.
- Khi rời màn trận: huỷ cùng trận.
- Không đọc gì từ trận; không đổi gì trong suốt trận.

## Thông số
| Thứ | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Quy đổi | 1u = 60 px trên màn 1920x1080 | | Camera đang có của scene Game (orthographic size 9, cao 18u), theo @CodeLead |
| Mặt đất y=0 | px 840 từ mép trên | px | Như trên; chân thành, hero, quái đều ở đây |
| Khung thấy ở 16:9 | x −16…+16u, y −4…+14u | u | Như trên; mép màn x khớp `feature-castle/castle-core/castle-body-values` |
| Phủ ngang của trời, cảnh xa, đất | x −21…+21u | u | Màn 21:9 thấy khoảng ±21u |
| Trời | y 0…14u | u | Phủ tới mép trên khung |
| Cảnh xa | y 0…2.5u (dự kiến) | u | Thấp hơn thành 4u |
| Dải đất | y −4…0u | u | Phủ tới mép dưới khung |
| Decor | 10 món: mỗi bên cỏ ở ±4, ±9, ±14u; đá ở ±6.5, ±11.5u | u (tâm x) | Rải đều 2.5u, cách thành |
| Cỡ decor | cỏ 0.8×0.3u, đá 0.6×0.3u | u | Phẳng, thấp hơn mọi unit (unit thấp nhất cao 0.8u) |
| Vùng không decor | x −3…+3u | u | Thân thành ±1.5u và chỗ hero ±2u, chừa thêm |
| Mép dưới dải HUD trên | y = 11.7u (px 140) | u | `feature-hud/hud-layout/layout-spec` (dải trên cao 140 px) |
| Mép trên thanh dưới | y = −1.3u (px 920) | u | Thanh nâng cấp, khay kĩ năng (đo lúc Play, theo @CodeLead); dải dưới của layout-spec cao 180 px |

Thứ tự vẽ (sau ra trước, theo node cha): trời → cảnh xa → dải đất → decor → thành → quái và hero → hiệu ứng → HUD → popup.

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Trời | 1 sprite @ClaudeImage, đứng yên | Art @Codex | Thêm mây trôi chậm |
| Cảnh xa | 1 dải sprite @ClaudeImage, đứng yên | 2 dải có độ sâu (dải sau nhỏ, nhạt hơn), art @Codex | Như PROTOTYPE |
| Dải đất | 1 sprite lặp ngang | Art @Codex | Như PROTOTYPE |
| Decor | 10 món phẳng (cỏ, đá), đứng yên | Thêm decor đứng (cờ, hàng rào) trong dải cảnh xa, art @Codex | Cỏ lay nhẹ |
| Đổi nền theo mốc đợt | Không | Không | Xét lại (trái "một bản đồ" ở vòng 1, cần người quyết) |

## Trường hợp biên
- **Màn rộng hơn 21:9:** hở hai mép ngoài ±21u; không hỗ trợ tỉ lệ đó. Màn 4:3 hẹp hơn: thấy ít hơn ±16u, nền vẫn phủ.
- **Unit đứng trên decor:** decor vẽ sau, unit che decor; không có va chạm.
- **Tạm dừng, thành bị phá, hồi sinh:** nền không đổi gì.
- **Rời màn trận rồi vào lại:** nền dựng lại như cũ (một bản đồ).

## Đã tự quyết
- **Bám camera đang có, không đặt camera mới.** Số của @CodeLead đo từ scene Game lúc Play; chân thành, hero, quái đã ở px 840. Đã loại: dời mặt đất để chừa nhiều đất hơn (phải sửa camera và mọi ảnh đã đối chiếu).
- **Decor ở IDEA chỉ có cỏ và đá phẳng.** Theo `field-decor-flat`: trên đường đi chỉ hoạ tiết phẳng. Cờ và hàng rào (decor đứng) đưa vào dải cảnh xa từ PROTOTYPE, để không trông như vật cản. Đã loại: cờ cắm cạnh thành ở IDEA (dễ nhầm là vật cản, chồng chỗ hero xuất phát).
- **"Parallax" hiểu là cảnh xa có độ sâu.** Camera đứng yên nên không có chuyển động để tạo parallax; PROTOTYPE dùng hai dải cảnh xa khác cỡ, khác độ đậm. Đã loại: cho cảnh xa tự trôi (dễ tưởng là chuyển động của trận).
- **Hai đường HUD trên frame chỉ là nét đo.** Số px của HUD vẫn là của `feature-hud`; frame ghi lại để kiểm nền không bị che sai chỗ.

## Chưa rõ
- Không có điểm chặn.

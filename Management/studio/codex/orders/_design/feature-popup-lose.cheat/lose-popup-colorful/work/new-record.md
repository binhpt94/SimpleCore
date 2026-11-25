Chi tiết cảnh (chỉ thêm cảnh; bố cục theo ảnh frame, hình thức theo ảnh tham chiếu của style đính kèm). LÀM LẠI theo phản hồi người chấm v1.

Nền (thay cho cảnh trận, theo phản hồi):
- KHÔNG vẽ cảnh trận, thành, lính, quái, trời, cỏ hay bất kỳ hình nào phía sau popup.
- Toàn màn hình 1920x1080 là một nền tím rất tối gần đen, phủ đều, phẳng; chỉ được có quầng sáng vàng nhạt mờ ngay sau emoji.

Popup: hình chữ nhật bo góc, phẳng, nhìn thẳng, ĐỐI XỨNG HOÀN HẢO qua trục dọc x = 960:
- Thân khung chiếm đúng x 560–1360, y 190–890 (800x700 px), trên ảnh 1920x1080. Không kéo dãn, không méo, không nghiêng, không phối cảnh. Hai cạnh trái phải song song và dày như nhau, bốn góc bo cùng bán kính.
- Khung: thân kem sáng, viền vàng cam dày có viền ngoài cam đậm hơn, góc trên uốn vành.
- Ruy băng tím vắt ngang đỉnh khung, cân giữa, hai đầu quặt xuống dài bằng nhau, mỗi bên một sao vàng cùng cỡ. Chữ tiêu đề "Thành đã bị phá" nằm giữa ruy băng: chữ vàng to, tròn, viền tím đậm, có bóng.
- Emoji mặt buồn màu vàng ở chính giữa trên ruy băng (tâm x = 960), tia sáng toả đều hai bên. Không thêm tim hay vật trang trí lệch một bên.
- Nơ hồng có sao vàng ở chính giữa mép dưới khung (tâm x = 960); lá xanh ở hai góc dưới, hai bên là ảnh soi gương của nhau, cùng cỡ. Ruy băng, emoji, nơ, lá chỉ tràn ra ngoài khung tối đa 80 px.
- Dòng "Trụ được 16 đợt" (y 350–420) và dòng "Kỉ lục: 16 đợt" (y 440–510): chữ tím đậm, mỗi dòng trên một mảng hồng nhạt bo tròn x 610–1310, cân giữa.
- Dòng "Kỉ lục mới!" (x 760–1160, y 530–600): dải vàng dạng viên thuốc, nổi bật nhất trong thân popup, chữ tím đậm, sao và lấp lánh đặt đối xứng hai bên.
- Nút "Chơi lại" (x 610–1010, y 730–850): nút chính, xanh lá, viên thuốc, bóng như kẹo, icon vòng lặp trắng bên trái chữ, to và nổi hơn nút phụ.
- Nút "Về màn bắt đầu" (x 1050–1310, y 730–850): nút phụ, xanh dương, viên thuốc, icon ngôi nhà trắng bên trái. Chữ "Về màn bắt đầu" giữ tỉ lệ chữ bình thường như chữ "Chơi lại", KHÔNG nén hẹp hay bóp dọc; nếu chật thì cho chữ nhỏ hơn hoặc xuống hai dòng ("Về màn" / "bắt đầu") trong nút.
- Mọi khối sáng ở mép trên, tối dần về mép dưới; viền mỗi khối là sắc đậm của chính màu đó, không viền đen. Giữ nguyên style Colorful, sáu chữ và các màu như bản v1 (khung vàng, ruy băng tím, thân kem, nút xanh lá và xanh dương, mảng hồng nhạt).

Chữ: đúng sáu chữ sau, đúng nguyên văn, đúng dấu tiếng Việt, đúng vị trí như frame, kiểu chữ không chân tròn như Roboto:
"Thành đã bị phá", "Trụ được 16 đợt", "Kỉ lục: 16 đợt", "Kỉ lục mới!", "Chơi lại", "Về màn bắt đầu".
Không thêm chữ, số, kí tự, đường kẻ hay số đo nào khác. Không có ô thưởng, nút hồi sinh, quảng cáo hay thanh tài nguyên.

Ảnh xuất ra đúng 1920x1080 px.

Đường chạy: studio.py codex (đường 1, Codex CLI ghi trực tiếp; đường 2 nếu đường 1 không được).

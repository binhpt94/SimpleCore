Sprite khung popup trống, dùng 9-slice trong Unity. Phong cách lấy ĐÚNG phần khung của ảnh tham chiếu thứ nhất (popup thua Colorful); ảnh tham chiếu thứ hai chỉ để lấy cỡ, vị trí viền.

Khung (PNG 800x700, nền ngoài hình trong suốt alpha 0):
- Một hình chữ nhật bo góc chiếm kín khung: mép ngoài cách mép ảnh 0–4 px ở cả bốn phía. Bán kính bo góc ngoài khoảng 40 px, bốn góc giống nhau. Phẳng, nhìn thẳng, đối xứng hoàn hảo qua trục dọc x = 400. Không bóng đổ ra ngoài hình.
- Từ ngoài vào trong:
  1. Viền ngoài cam đậm (#E07A10), dày 6 px.
  2. Viền vàng cam (#FFB020 tới #FFD040), dày khoảng 22 px, phồng như kẹo: vệt bóng sáng vàng nhạt (#FFE890) chạy dọc mép trên viền, phần viền ở cạnh dưới tối hơn phần viền ở cạnh trên.
  3. Một đường viền trong mỏng cam (#F0A020), dày 4 px, ngăn viền với thân.
- Tổng viền (1+2+3) khoảng 32 px ở trên, trái, phải; cạnh dưới có thể dày hơn tới 40 px. Mọi thứ của viền và bo góc nằm trong 48 px tính từ mép ảnh ở trái, trên, phải và 56 px ở dưới.
- Thân giữa: màu kem sáng (#FFF4E0 ở trên, chuyển rất nhẹ tới #FCEAD0 ở dưới), phẳng, không hoa văn, không mây, không vệt sáng, không chi tiết gì. Vùng x 48–752, y 48–644 chỉ là màu kem này.

Không có: chữ, số, ruy băng, emoji, nơ, lá, sao, lấp lánh, đường đứt, ô giữ chỗ, nút. Không viền đen.

Đường chạy: studio.py codex (đường 1, Codex CLI ghi trực tiếp).

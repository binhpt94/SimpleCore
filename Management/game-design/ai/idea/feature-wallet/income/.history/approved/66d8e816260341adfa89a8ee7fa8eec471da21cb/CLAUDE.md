# Nguồn thu

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- MỘT quái bị hạ cho MỘT lần cộng tiền (`feature-enemy` báo, kèm mức tiền của loại quái đó).
- MỘT đợt dọn sạch cho MỘT lần cộng tiền thưởng (`feature-wave` báo, kèm mức thưởng).
- Mọi lần cộng đi vào cùng một số dư (`feature-wallet/balance-spend`).
- Mức tiền cụ thể do `feature-balance` quyết cùng giá nâng cấp.

## Danh sách
- Nguồn thu: hạ quái, dọn sạch đợt.
- Thứ hạ được quái (đều cho tiền như nhau): hero, đòn tự bắn của thành, kĩ năng của thành.

## Luật cơ bản
- Quái gục là cộng ngay mức tiền của loại quái đó, bất kể ai hạ.
- Đợt được dọn sạch là cộng ngay thưởng dọn đợt.
- Quái trâu hơn cho nhiều tiền hơn quái yếu.
- Đợt sau thưởng nhiều hơn đợt trước.

## Đã tự quyết
- **Mức tiền nằm ở bên báo, không ở ví.** `feature-enemy` biết loại quái, `feature-wave` biết số đợt; ví chỉ cộng con số được đưa. Đã loại: ví giữ bảng tiền theo loại quái.
- **Thưởng dọn đợt tăng theo số đợt.** Đợt sau khó hơn thì phần thưởng cũng lớn hơn để người chơi còn theo kịp. Đã loại: thưởng bằng nhau.
- **Không thêm nguồn thu khác.** Vòng 1 chỉ nói "hạ quái được tiền" và dọn đợt là bước tiến; thêm nguồn là mở rộng game.

## Chưa rõ
- Không có điểm chặn.

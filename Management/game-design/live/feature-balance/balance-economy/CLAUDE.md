# Thu và chi

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Thu gồm HAI nguồn: tiền hạ quái (`feature-enemy`, tăng theo đợt ở `feature-wave/difficulty-scaling`) và thưởng dọn sạch đợt (`feature-wave/wave-clear`). Cả hai vào `feature-wallet`.
- Chi chỉ có MỘT chỗ: mua nâng cấp (`feature-upgrade`). Mỗi hero một mục cấp; thành ba mục (máu, sức đánh, kĩ năng). Giá mỗi mục tăng theo cấp của chính mục đó.
- Sức mạnh mua được từ tiền là đầu vào của `feature-balance/balance-difficulty-curve`.

## Định nghĩa
- **Thu của một đợt**: tổng tiền hạ quái và thưởng dọn sạch trong đợt đó.
- **Giá một lần nâng**: tiền phải trả để nâng một mục lên cấp kế tiếp.
- **Lời của một lần nâng**: sức mạnh thêm vào so với giá phải trả.

## Danh sách
Số liệu feature này đối chiếu (mỗi số nằm ở cấu hình của feature sở hữu):
- Tiền gốc khi hạ từng loại quái (`feature-enemy`).
- Mức tăng tiền mỗi con theo đợt (`feature-wave`).
- Thưởng dọn sạch đợt và mức tăng theo đợt (`feature-wave`).
- Giá ban đầu và mức tăng giá theo cấp của từng mục (`feature-upgrade`).
- Sức thêm vào mỗi cấp của từng mục (`feature-upgrade`, `feature-hero`, `feature-castle`).

## Luật cơ bản
- Tiền hạ quái trong một đợt nhiều hơn thưởng dọn sạch đợt đó.
- Thu của đợt sau không ít hơn đợt trước.
- Giá một mục tăng theo cấp nhanh hơn thu tăng theo đợt.
- Mục ở cấp thấp thì lời hơn chính mục đó ở cấp cao.
- Tiền không dùng hết thì giữ lại trong lượt; lượt mới bắt đầu lại từ đầu (`feature-run`).

## Đã tự quyết
- **Tiền hạ quái là nguồn chính.** Hợp với "nâng cấp ngay trong trận" ở vòng 1. Đã loại: dồn thưởng vào cuối đợt.
- **Giá tự hãm bằng tăng theo cấp, không có cấp tối đa.** Theo `feature-upgrade` (`upgrade-cost`).
- **Lời giảm dần theo cấp.** Để rải tiền ra nhiều mục là cách chơi tốt, không phải dồn một mục.

## Chưa rõ
- Không có điểm chặn.

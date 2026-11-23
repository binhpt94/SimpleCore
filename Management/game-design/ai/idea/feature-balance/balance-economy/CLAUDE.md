# Thu và chi

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Thu gồm HAI nguồn: tiền hạ quái (`feature-enemy`, mỗi con cố định; tổng tăng theo đợt vì `feature-wave/difficulty-scaling` cho đông quái và loại đắt hơn) và thưởng dọn sạch đợt (`feature-wave/wave-clear`). Cả hai vào `feature-wallet`.
- Chi chỉ có MỘT chỗ: mua nâng cấp (`feature-upgrade`). Mỗi hero một mục cấp; thành ba mục (máu, sức đánh, kĩ năng). Giá mỗi mục tăng theo cấp của chính mục đó.
- Sức mạnh mua được từ tiền là đầu vào của `feature-balance/balance-difficulty-curve`.

## Định nghĩa
- `glossary#wave-income`
- `glossary#next-price` (giá một lần nâng)
- `glossary#upgrade-value`

## Danh sách
Số liệu feature này đối chiếu (mỗi số nằm ở cấu hình của feature sở hữu):
- Tiền gốc khi hạ từng loại quái (`feature-enemy`).
- Số quái và phần từng loại theo đợt (`feature-wave`); tiền mỗi con không nhân theo đợt.
- Thưởng dọn sạch đợt và mức tăng theo đợt (`feature-wave`).
- Giá ban đầu và mức tăng giá theo cấp của từng mục (`feature-upgrade`).
- Sức thêm vào mỗi cấp của từng mục (`feature-upgrade`, `feature-hero`, `feature-castle`).

## Luật cơ bản
- Tiền hạ quái trong một đợt nhiều hơn thưởng dọn sạch đợt đó.
- Thu của đợt sau không ít hơn đợt trước. Ngoại lệ có chủ đích: đợt ngay sau đợt có Trùm thu ít hơn đợt Trùm, vì tiền Trùm là phần thưởng riêng của đợt Trùm.
- Giá một mục tăng theo cấp nhanh hơn thu tăng theo đợt.
- Mục ở cấp thấp thì lời hơn chính mục đó ở cấp cao.
- Tiền không dùng hết thì giữ lại trong lượt; lượt mới bắt đầu lại từ đầu (`feature-run`).

## Đã tự quyết
- **Tiền hạ quái là nguồn chính.** Hợp với "nâng cấp ngay trong trận" ở vòng 1. Đã loại: dồn thưởng vào cuối đợt.
- **Giá tự hãm bằng tăng theo cấp, không có cấp tối đa.** Theo `feature-upgrade` (`upgrade-cost`).
- **Lời giảm dần theo cấp.** Để rải tiền ra nhiều mục là cách chơi tốt, không phải dồn một mục.
- **Thu giảm sau đợt Trùm là ngoại lệ có chủ đích (2026-11-23, quyết định đã chốt).** Thêm ngoại lệ vào luật "thu đợt sau không ít hơn đợt trước": Trùm là phần thưởng riêng. Đã loại: giảm tiền Trùm cho thu phẳng (mất cảm giác thưởng khi hạ Trùm).

## Chưa rõ
- Không có điểm chặn.

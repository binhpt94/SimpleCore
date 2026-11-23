# Hiệu lực nâng cấp

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- MỘT lần mua làm MỘT mục tăng MỘT cấp (`feature-upgrade/upgrade-list`).
- Mục hero tác động lên MỘT hero; mục thành tác động lên thành.
- Feature này chỉ báo "mục X lên cấp Y". Cấp đó đổi ra chỉ số bao nhiêu thì do bên giữ chỉ số tính:
  - `feature-hero`: chỉ số hero theo cấp.
  - `feature-castle`: máu, sức đánh, kĩ năng thành theo cấp.
- Mức tăng mỗi cấp do `feature-balance` quyết.

## Định nghĩa
- Không dùng thuật ngữ riêng.

## Danh sách
- Cấp hero: tăng mọi chỉ số của hero đó.
- Máu thành: tăng máu tối đa, máu hiện tại tăng thêm đúng phần tăng đó.
- Sức đánh thành: đòn tự bắn mạnh hơn.
- Kĩ năng thành: cả ba kĩ năng mạnh hơn, hồi chiêu không đổi (`feature-castle/castle-upgrade-effects`).

## Luật cơ bản
- Hiệu lực xảy ra ngay lúc mua.
- Cấp càng cao thì càng mạnh; không có cấp nào làm yếu đi.
- Hero gục vẫn nhận cấp mới; khi hồi sinh dùng chỉ số theo cấp mới.
- Nâng cấp không mất khi hero gục; chỉ mất khi lượt mới bắt đầu (`feature-run`).

## Đã tự quyết
- **Máu thành tăng cả tối đa lẫn hiện tại.** Theo quyết định chung; đồng thời là cách cứu nguy duy nhất cho thành vì thành không tự hồi máu (`feature-castle`). Đã loại: chỉ tăng tối đa; hồi đầy máu khi mua (quá mạnh, biến mua máu thành nút hồi máu).
- **Feature này không giữ chỉ số.** Chỉ số hero thuộc `feature-hero`, chỉ số thành thuộc `feature-castle`; nâng cấp chỉ đẩy cấp sang. Giữ các feature độc lập. Đã loại: feature này giữ bảng chỉ số của mọi thứ.
- **Nâng kĩ năng không giảm hồi chiêu, khớp `feature-castle/castle-upgrade-effects`.** Hiệu ứng lên thành do feature thành quyết; node này chỉ ghi lại để không mâu thuẫn. Đã loại: "mạnh hơn hoặc hồi chiêu ngắn hơn tuỳ kĩ năng" (bản trước, lệch với feature thành).

## Chưa rõ
- Không có điểm chặn.

# Một nhịp giao chiến

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Một bước cập nhật của trận có MỘT nhịp giao chiến.
- Một nhịp có KHÔNG, MỘT hoặc NHIỀU đòn trúng.
- Một đòn có MỘT bên ra đòn và MỘT hoặc NHIỀU bên trúng (đòn vùng).
- Bên ra đòn: `feature-hero`, `feature-enemy`, `feature-castle` (đòn tự bắn, Sấm sét).
- Bên nhận kết quả: `feature-wave` (quái bị hạ, xét dọn sạch), `feature-run` (thành bị phá), `feature-wallet` qua quái (tiền hạ quái).

## Định nghĩa
- `glossary#target`

## Danh sách
Các bước của một nhịp:
1. Gom các đòn trúng của bước.
2. Bỏ đòn vào bên không còn nhận đòn (theo trạng thái đầu nhịp).
3. Cộng sát thương theo bên trúng.
4. Mỗi feature sở hữu trừ máu một lần, báo hết máu một lần.
5. Báo nhịp xong.

## Luật cơ bản
- Hai bên kết liễu nhau cùng nhịp thì cả hai đều hết máu.
- Sát thương đã chốt lúc ra đòn; nâng cấp giữa lúc ra đòn và lúc trúng không đổi đòn đó.
- Giao chiến không giữ máu hay chỉ số.

## Đã tự quyết
- **Ở IDEA không có giao chiến (độ quan trọng 6).** Mỗi bên tự trừ máu ngay khi trúng theo luật của nhánh chỉ số hero; kết quả khác nhau chỉ ở đòn trùng đúng một bước, hiếm ở nhịp game này.
- **Băng giá, Cổ vũ không đi qua giao chiến.** Chúng không trừ máu.
- **Bỏ tạm dừng, quảng cáo và popup hồi sinh (2026-11-26, người quyết).** Bỏ luật "tạm dừng: không có nhịp nào chạy".

## Chưa rõ
- Giao chiến là một dịch vụ riêng hay một bước trong vòng lặp trận: vòng 4, cùng @CodeLead.

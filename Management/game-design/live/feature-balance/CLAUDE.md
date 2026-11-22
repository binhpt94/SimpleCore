# Cân bằng số liệu

Dữ liệu cốt lõi: [data.md](data.md)

## Người chơi được gì
- Áp lực tăng đều, không quá dễ, không gãy đột ngột.
- Mỗi lựa chọn (đội, nâng cấp) đều có ý nghĩa.

## Liên quan
- `feature-hero`, `feature-castle`, `feature-enemy`, `feature-wave`, `feature-upgrade`, `feature-wallet`: các feature giữ số liệu.

## Đã tự quyết
- **Balance là feature riêng.** Luật vòng 2: balance cắt ngang nhiều feature.
- **Số liệu nằm trong cấu hình của từng feature, không gom vào một chỗ dùng chung.** Giữ các feature độc lập theo luật của project. Feature này là nơi quyết và đối chiếu các số đó.

## Chưa rõ
- Có công cụ xem chung các số (bảng, mô phỏng) hay không: vòng 3.

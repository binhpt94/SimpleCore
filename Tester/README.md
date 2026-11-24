# Tester: kết quả chơi thử của nhóm Testers
Nơi các phiên nhóm Testers ghi lại từng đợt chơi thử game trong Unity. Luật của nhóm: `.claude/rules/tester.md`.

## Cấu trúc thư mục
```
Tester/
  README.md                      file này
  <Tên tester>/                  một thư mục cho mỗi phiên tester (hiện có: UnityAutoPlay)
    README.md                    tester đó test gì, cách đọc một đợt
    <yyyymmdd-hhmmss>/           một đợt test, tên là thời điểm bắt đầu
      README.md                  mục đích, test gì, môi trường, các bước, kết quả, lỗi tìm thấy, cấu trúc thư mục
      images/                    ảnh chụp và dữ liệu phụ của đợt
    Archive/                     các đợt cũ đã dời đi
      README.md                  danh mục các đợt đã lưu trữ
```

## Ai đọc, ai ghi
| Thư mục | Đọc | Ghi |
|---|---|---|
| `Tester/` và các đợt đang mở | Leaders, @Core, Services, Features, Testers | chỉ phiên tester sở hữu thư mục |
| `Tester/<Tên>/Archive/` | chỉ nhóm Testers (và người) | chỉ lệnh `tester.py archive` |
Nhóm Tools và @DesignAdvisor không đọc thư mục này. Đợt cũ được dời vào `Archive` để các phiên khác không phải đọc lại kết quả đã hết giá trị.

## Lệnh
`python Management/tools/tester.py status` in các đợt đang mở cùng mục đích và kết quả; `check` kiểm cấu trúc. Tạo đợt (`new`) và lưu trữ (`archive`): xem `.claude/rules/tester.md`.

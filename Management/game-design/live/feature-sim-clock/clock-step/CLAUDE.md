# Bước nhịp và cách chạy

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Một trận có NHIỀU bước nhịp.
- Một khung hình chạy KHÔNG, MỘT hoặc NHIỀU bước nhịp (cách bước cố định); đúng MỘT bước (cách theo khung hình).
- Một bước nhịp: mỗi feature gameplay cập nhật MỘT lần; giao chiến chạy MỘT nhịp giao chiến.
- Một thao tác của người chơi thuộc đúng MỘT bước nhịp (bước kế tiếp sau lúc bấm).
- Config chung: MỘT cho cả game, chọn MỘT cách chạy.

## Định nghĩa
- `glossary#sim-step`
- `glossary#paused`

## Danh sách
Thứ tự trong một bước nhịp:
1. Áp các thao tác trong hàng chờ, đúng thứ tự đã đưa vào: `feature-upgrade` (mua), `feature-castle` (kĩ năng), `feature-run` (tạm dừng, tiếp tục; kết quả hồi sinh từ `feature-popup-revive`).
2. `feature-wave`: đồng hồ lúc thở, thả quái.
3. `feature-enemy`: quái đi, nhắm, ra đòn.
4. `feature-hero`: hero hồi sinh, đi, nhắm, ra đòn.
5. `feature-castle`: đòn tự bắn, hồi chiêu và kĩ năng.
6. `feature-combat`: một nhịp giao chiến (gom, lọc, cộng, chủ trừ máu, báo hết máu, báo nhịp xong).
7. `feature-run`: xét thua, đồng hồ chờ trước popup.

Hai cách chạy:
- Bước cố định (TickUpdate): bước dài bằng nhau.
- Theo khung hình (Update): bước dài bằng thời gian khung.

Tua nhanh ở bản dev: tăng số bước mỗi khung, chạy ngay nhiều bước, đóng băng, bước từng bước.

## Luật cơ bản
- Feature gameplay không đọc thời gian khung hình hay giờ thật.
- Đổi cách chạy chỉ đổi config, không sửa code của feature.
- Bước sau chỉ bắt đầu khi bước trước đã chạy xong mọi feature.
- Phần nhìn không đổi trạng thái gameplay.
- Tạm dừng: bước nhịp vẫn chạy (số bước chung tăng, hàng chờ được áp), đồng hồ gameplay không cộng, các feature gameplay đứng yên.
- Đầu lượt: lượt chơi yêu cầu dựng lượt mới thì nhịp đặt lại số bước, thời gian dư, seed và các dòng; bước gameplay đầu tiên chỉ chạy khi lượt chơi xác nhận đã bắt đầu.
- Hồi sinh thành: nhịp chạy tiếp số bước, không đặt lại.
- Huỷ trận: nhịp dọn hàng chờ và thời gian dư trước khi báo huỷ xong.

## Đã tự quyết
- **Thứ tự feature trong bước** theo @CodeLead (Wave, Enemy, Hero, Castle, Combat, Run), đối chiếu với `feature-combat/combat-tick/tick-values` (ý `tick-order`: đi và nhắm, ra đòn, rồi gom, lọc, cộng, trừ máu, báo). Các bên ra đòn đều chạy trước giao chiến, nên khớp. Thứ tự giữa quái, hero, thành không đổi kết quả đòn vì giao chiến xét theo trạng thái đầu nhịp; vẫn chốt cứng để lặp lại được. @CodeLead ép bằng thứ tự đăng kí và có test.
- **Thao tác áp vào đầu bước kế tiếp.** Một thao tác giữa chừng bước thì không rõ thuộc bước nào; ghi theo số bước cho tester phát lại được. Đã loại: áp ngay lúc bấm.
- **Hàng chờ thao tác do nhịp giữ** (@CodeLead đề nghị, 2026-11-25). Bản ghi theo số bước cần một nơi duy nhất. Hệ quả: mọi nút gameplay không đổi trạng thái ngay lúc bấm; bảy feature chạy theo nhịp là Combat, Castle, Enemy, Hero, Wave, Run, Upgrade. Hud và HudUpgrade chỉ chuyển lời bấm, không đổi. Nút ngoài trận (Chơi lại, nút của popup thua) không qua hàng chờ vì không còn bước nhịp nào chạy. Đã loại: mỗi feature tự giữ hàng chờ (bản ghi rải nhiều nơi, khó phát lại đúng thứ tự giữa các feature).
- **Thao tác lúc tạm dừng vẫn vào hàng chờ, feature tự từ chối.** Giữ nguyên luật chặn thao tác đã chốt ở nhánh tạm dừng của `feature-run`.
- **Tạm dừng không dừng nhịp** (@CodeLead). Luật tạm dừng đã chốt ở `feature-run` không phải đổi.
- **Đưa góp ý GPT lần hỏi `r2-1` của node cha (2026-11-25).**
  - **Hai đồng hồ:** số bước chung (mọi bước, để xếp và ghi thao tác) và đồng hồ gameplay (chỉ cộng bước lượt cho chạy). Ý `clock-time` và `pause-keeps-clock` sửa theo góp ý `dong-ho-khi-tran-chay`. Không trái với quyết định "tạm dừng không dừng nhịp" của @CodeLead: bước vẫn chạy, chỉ đồng hồ gameplay đứng.
  - **Thả quái sau kết quả** (`thu-tu-tha-quai`): đợt quái chạy đầu bước k+1, tức sau giao chiến và lượt chơi của bước k; khớp góp ý mà không phải đổi thứ tự đã chốt cùng @CodeLead. Đã loại: dời đợt quái xuống sau lượt chơi trong cùng bước (đổi thứ tự đăng kí mà kết quả như nhau).
  - **Bước sau chờ giao chiến chốt** (`chot-giao-chien`): đã có ở luật "Bước sau chỉ bắt đầu khi bước trước đã chạy xong mọi feature".
  - Kết quả hồi sinh đi qua hàng chờ (`phat-lai-ket-qua-hoi-sinh`).
- **Tua chỉ ở bản dev.** Người chơi không cần; tránh lộ đường gian lận.

## Chưa rõ
- Không có điểm chặn. Con số (độ dài bước, trần số bước) và cách chạy mặc định ở vòng 4.

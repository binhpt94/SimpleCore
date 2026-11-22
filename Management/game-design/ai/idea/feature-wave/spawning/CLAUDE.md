# Thả quái

Dữ liệu cốt lõi (tóm tắt và các ý có điểm): [data.md](data.md)

## Người chơi làm gì
- Không làm gì: quái tự ra. Người chơi chỉ quan sát và có thể tạm dừng.

## Người chơi thấy gì
- Đầu đợt: chữ "Đợt N" hiện giữa màn hình một lúc rồi mờ đi; sau đó quái mới bắt đầu ra.
- Quái xuất hiện ở mép trái và mép phải, đi vào phía thành.
- Góc màn hình hiện số đợt hiện tại và số quái còn lại của đợt (gồm cả quái chưa ra).

## Luật
- Quái ra từng con một, cách nhau một khoảng thời gian gọi là khoảng thả.
- Chia hai bên: mỗi bên một nửa số quái của đợt; số lẻ thì con dư ra bên ngẫu nhiên.
- Thứ tự bên ra ngẫu nhiên, nhưng không quá 3 con liên tiếp cùng một bên (để không bên nào trống quá lâu).
- Thứ tự loại quái trong đợt được xáo ngẫu nhiên.
- Quái ra ở mép màn hình, vị trí dọc lệch ngẫu nhiên một chút trong dải mặt đất để không chồng hình lên nhau.
- Số quái còn sống trên màn hình có giới hạn. Đủ giới hạn thì tạm hoãn thả, có con chết thì thả tiếp.
- Khoảng thả ngắn dần theo đợt, có mức thấp nhất.

## Trường hợp biên
- Tạm dừng: ngừng đếm thời gian thả, tiếp tục đúng chỗ khi bỏ tạm dừng.
- Thành hết máu giữa lúc thả: dừng thả ngay, quái chưa ra thì bỏ.
- Đợt chỉ có 1 quái: ra ở bên ngẫu nhiên.
- Hết giới hạn quái trên màn hình kéo dài: đồng hồ thả đứng chờ, không dồn quái ra một lúc khi có chỗ.

## Con số dự kiến
- Thời gian hiện chữ "Đợt N": 1.5 giây. Đủ đọc, không làm chậm nhịp.
- Khoảng thả đợt 1: 1.0 giây; mỗi đợt ngắn đi 0.05 giây; thấp nhất 0.3 giây (đạt ở đợt 15). Đợt sau dồn dập hơn nhưng vẫn kịp nhìn từng con.
- Giới hạn quái còn sống cùng lúc: 40 con. Giữ màn hình điện thoại còn đọc được và máy không bị nặng.
- Giới hạn liên tiếp cùng bên: 3 con.

## Đã tự quyết
- **Thả từng con theo nhịp đều, không thả theo cụm.** Dễ đọc, dễ cân bằng. Đã loại: cụm 5 con một lúc (có thể thêm sau như biến thể đợt).
- **Chia đều hai bên.** Người chơi không phải đoán bên nào nguy hơn; hero cũng được thành chia đều hai bên (`feature-castle`). Đã loại: lệch bên theo đợt.
- **Có giới hạn quái trên màn hình.** Vì đợt muộn có thể rất đông. Đã loại: không giới hạn.
- **Một dải mặt đất, không chia làn.** Hợp với "quái đi thẳng về phía thành" ở `feature-enemy`. Đã loại: nhiều làn trên dưới.

## Chưa rõ
- Không có điểm chặn.

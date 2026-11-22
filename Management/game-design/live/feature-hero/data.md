---
based-on: cf568e2870fe
---
# Hero

Các chiến binh người chơi chọn vào đội trước mỗi lượt chơi. Trong trận, hero tự tìm và đánh quái gần nhất, gục thì hồi sinh ở thành; người chơi chỉ tác động qua nâng cấp.

## Ý cốt lõi

### hero-roster | AI 7
Có một danh sách hero, mỗi hero có tên, cách đánh và chỉ số riêng (máu, sức đánh, tốc độ đánh, tầm đánh, tốc độ chạy). Bản đầu dự kiến 6 hero, mở sẵn hết.

### team-select | AI 7
Trước mỗi lượt chơi có màn chọn đội: chọn đúng số hero cố định (dự kiến 4) từ danh sách, mỗi hero một lần. Đủ người mới bấm vào trận được.

### hero-roles | AI 7
Hero chia theo cách đánh, ít nhất có cận chiến (máu cao, tầm ngắn, chắn quái) và bắn xa (máu thấp, tầm xa, đứng sau).

### hero-auto-combat | AI 8
Hero tự tìm quái gần nhất, chạy tới trong tầm và đánh; hết quái thì về đứng gần thành. Người chơi không điều khiển hero.

### hero-respawn | AI 7
Hero hết máu thì gục, biến mất khỏi trận, rồi hồi sinh ở thành sau một khoảng thời gian chờ; trong lúc chờ có hiện thời gian còn lại.

### hero-upgrade | AI 7
Mỗi hero trong đội có cấp riêng trong lượt chơi; lên cấp thì mọi chỉ số tăng. Cấp giữ nguyên khi hero gục và hồi sinh.

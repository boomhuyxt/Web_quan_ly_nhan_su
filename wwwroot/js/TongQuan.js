document.addEventListener("DOMContentLoaded", async function () {
    try {
        const response = await fetch('/api/tongquan/thong-ke');
        if (response.ok) {
            const data = await response.json();
            if (data.success) {
                // Đổ dữ liệu vào các thẻ thống kê số lượng
                document.getElementById('txtTongNhanVien').innerText = data.tongNhanVien;
                document.getElementById('txtTongPhongBan').innerText = data.tongPhongBan;
                document.getElementById('txtChuaChamCong').innerText = data.chuaChamCong;
                document.getElementById('txtTyLeDungGio').innerText = data.tyLeDungGio;
                document.getElementById('txtLuongGanNhat').innerText = data.luongGanNhat;

                // Xử lý hiển thị danh sách tin nhắn mới
                const activityList = document.getElementById('recentActivityList');
                if (activityList) {
                    activityList.innerHTML = '';
                    if (data.tinNhanMoi && data.tinNhanMoi.length > 0) {
                        data.tinNhanMoi.forEach(msg => {
                            // Xác định đường dẫn chuyển hướng khi click vào tin nhắn
                            const chatUrl = msg.maNhom
                                ? `/Home/Chat?groupId=${msg.maNhom}`
                                : `/Home/Chat?userId=${msg.nguoiGuiId}`;

                            const html = `
                            <a href="${chatUrl}" class="group flex items-center gap-4 p-4 rounded-[24px] bg-white hover:bg-blue-50/40 transition-all duration-300 shadow-[0_2px_8px_rgba(0,0,0,0.02)] border border-gray-50">
                                <div class="w-12 h-12 rounded-full overflow-hidden shrink-0 ring-2 ring-transparent group-hover:ring-primary/20 transition-all">
                                    <img class="w-full h-full object-cover" src="${msg.anhDaiDien || '/images/avatar_default.jpg'}"/>
                                </div>
                                <div class="flex-1 min-w-0">
                                    <div class="text-sm font-bold text-on-surface truncate">${msg.nguoiGui}</div>
                                    <div class="text-xs text-outline font-medium leading-snug mt-0.5 truncate">${msg.noiDung}</div>
                                </div>
                                <div class="text-[11px] font-medium text-primary shrink-0">${msg.thoiGian}</div>
                            </a>`;
                            activityList.insertAdjacentHTML('beforeend', html);
                        });
                    } else {
                        activityList.innerHTML = '<div class="text-center text-sm text-outline py-8">Không có tin nhắn mới nào trong 2 giờ qua.</div>';
                    }
                }
            }
        }
    } catch (error) {
        console.error("Lỗi khi đồng bộ dữ liệu tổng quan:", error);
    }
});
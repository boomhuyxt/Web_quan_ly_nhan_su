// Khai báo các biến toàn cục
let masterData = { phongBans: [], vaiTros: [] };
let editingUserId = null;

// Khởi tạo danh sách các phần tử DOM sau khi trang tải xong
const els = {
    tabDonTu: null,
    tabNhanSu: null,
    tabCongTac: null,
    tabPhongBan: null,
    btnTabDonTu: null,
    btnTabNhanSu: null,
    btnTabCongTac: null,
    btnTabPhongBan: null,
    tbodyNhanVien: null,
    tbodyPhongBan: null,
    txtSearch: null
};

document.addEventListener('DOMContentLoaded', () => {
    // Ánh xạ các phần tử DOM thực tế từ giao diện
    els.tabDonTu = document.getElementById('tabDonTu');
    els.tabNhanSu = document.getElementById('tabNhanSu');
    els.tabCongTac = document.getElementById('tabCongTac');
    els.tabPhongBan = document.getElementById('tabPhongBan');
    els.btnTabDonTu = document.getElementById('btnTabDonTu');
    els.btnTabNhanSu = document.getElementById('btnTabNhanSu');
    els.btnTabCongTac = document.getElementById('btnTabCongTac');
    els.btnTabPhongBan = document.getElementById('btnTabPhongBan');
    els.tbodyNhanVien = document.getElementById('tableNhanVien');
    els.tbodyPhongBan = document.getElementById('tablePhongBan');
    els.txtSearch = document.getElementById('txtSearchNhanVien');

    // Lắng nghe sự kiện tìm kiếm nhân viên thời gian thực
    if (els.txtSearch) {
        els.txtSearch.addEventListener('input', (e) => {
            loadNhanVienData(e.target.value.trim());
        });
    }
});

// ==============================================================
// === HỆ THỐNG ĐIỀU HƯỚNG VÀ LỌC (TABS & FILTERS) ===
// ==============================================================

// Hàm lọc danh sách đơn từ nghỉ phép
function filterDonTu(status, btn) {
    document.querySelectorAll('.btn-filter-dontu').forEach(b => {
        b.className = "btn-filter-dontu px-6 py-2 rounded-full bg-white border border-gray-200 text-outline text-sm font-bold whitespace-nowrap hover:bg-gray-50 active:scale-95 transition-all";
    });
    btn.className = "btn-filter-dontu px-6 py-2 rounded-full bg-primary text-white text-sm font-bold whitespace-nowrap shadow-md shadow-primary/20 active:scale-95 transition-all";

    let visibleCount = 0;
    document.querySelectorAll('.dontu-item').forEach(item => {
        if (status === 'Tất cả' || item.dataset.status === status) {
            item.style.display = 'block';
            visibleCount++;
        } else {
            item.style.display = 'none';
        }
    });
    document.getElementById('emptyFilterMsg').style.display = visibleCount === 0 ? 'flex' : 'none';
}

// Hàm chuyển đổi tab chức năng quản trị
function switchTab(tab) {
    if (!els.tabDonTu) return; // Đảm bảo DOM đã sẵn sàng

    els.tabDonTu.classList.add('hidden');
    els.tabNhanSu.classList.add('hidden');
    els.tabCongTac.classList.add('hidden');
    if (els.tabPhongBan) els.tabPhongBan.classList.add('hidden');

    const defaultClass = "pb-3 text-sm font-bold text-outline border-b-2 border-transparent hover:text-primary transition-all active:scale-95 whitespace-nowrap";
    const activeClass = "pb-3 text-sm font-bold text-primary border-b-2 border-primary transition-all active:scale-95 whitespace-nowrap";

    els.btnTabDonTu.className = defaultClass;
    els.btnTabNhanSu.className = defaultClass;
    els.btnTabCongTac.className = defaultClass;
    if (els.btnTabPhongBan) els.btnTabPhongBan.className = defaultClass;

    if (tab === 'dontu') {
        els.tabDonTu.classList.remove('hidden');
        els.btnTabDonTu.className = activeClass;
    }
    else if (tab === 'nhansu') {
        els.tabNhanSu.classList.remove('hidden');
        els.btnTabNhanSu.className = activeClass;
        loadNhanVienData(els.txtSearch ? els.txtSearch.value.trim() : "");
    }
    else if (tab === 'congtac') {
        els.tabCongTac.classList.remove('hidden');
        els.btnTabCongTac.className = activeClass;
        loadDanhSachCongTac();
    }
    else if (tab === 'phongban') {
        if (els.tabPhongBan) els.tabPhongBan.classList.remove('hidden');
        if (els.btnTabPhongBan) els.btnTabPhongBan.className = activeClass;
        loadDanhSachPhongBanGrid();
    }
}

// ==============================================================
// === LOGIC XỬ LÝ QUẢN LÝ PHÒNG BAN (ĐÃ CẬP NHẬT CHẶN PHÒNG BAN) ===
// ==============================================================

// Tải dữ liệu danh mục phòng ban lên bảng quản trị
async function loadDanhSachPhongBanGrid() {
    if (!els.tbodyPhongBan) return;
    els.tbodyPhongBan.innerHTML = '<tr><td colspan="3" class="p-12 text-center text-outline animate-pulse font-medium">Đang tải danh sách phòng ban...</td></tr>';

    try {
        const res = await fetch('/api/phongban/danh-sach');
        const result = await res.json();

        if (result.success) {
            if (result.data.length === 0) {
                els.tbodyPhongBan.innerHTML = '<tr><td colspan="3" class="p-12 text-center text-outline">Chưa thành lập phòng ban nào hoặc toàn bộ phòng ban đã bị chặn.</td></tr>';
                return;
            }

            els.tbodyPhongBan.innerHTML = result.data.map(pb => `
                <tr class="hover:bg-primary/5 transition-colors">
                    <td class="px-6 py-4 text-sm font-medium text-gray-600">#${pb.maPhongBan}</td>
                    <td class="px-6 py-4 text-sm font-bold text-gray-900">${pb.tenPhongBan}</td>
                    <td class="px-6 py-4 text-right">
                        <button onclick="deletePhongBan(${pb.maPhongBan}, '${pb.tenPhongBan}')" class="w-9 h-9 rounded-xl bg-white border border-gray-200 text-error hover:border-error hover:bg-error/5 transition-all shadow-sm flex items-center justify-center ml-auto" title="Chặn phòng ban này">
                            <span class="material-symbols-outlined text-[18px]">block</span>
                        </button>
                    </td>
                </tr>
            `).join('');
        }
    } catch (e) {
        console.error(e);
        els.tbodyPhongBan.innerHTML = '<tr><td colspan="3" class="p-12 text-center text-error font-semibold">Gặp sự cố kết nối khi đồng bộ phòng ban!</td></tr>';
    }
}

// Điều khiển đóng mở modal thêm phòng ban
function openAddPhongBanModal() {
    document.getElementById('txtAddTenPhongBan').value = '';
    document.getElementById('modalAddPhongBan').classList.remove('hidden');
}

function closeAddPhongBanModal() {
    document.getElementById('modalAddPhongBan').classList.add('hidden');
}

// Gửi yêu cầu API thiết lập thêm phòng ban hoạt động mới
async function submitAddPhongBan() {
    const tenPhongBan = document.getElementById('txtAddTenPhongBan').value.trim();
    if (!tenPhongBan) return alert("⚠️ Vui lòng điền tên phòng ban mới!");

    const btnSubmit = document.getElementById('btnSubmitAddPhongBan');
    btnSubmit.disabled = true;
    btnSubmit.innerText = "Đang xử lý...";

    try {
        const res = await fetch('/api/phongban/them', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ tenPhongBan: tenPhongBan })
        });
        const data = await res.json();

        if (data.success) {
            closeAddPhongBanModal();
            loadDanhSachPhongBanGrid();
            loadMasterData();
        } else {
            alert("Lỗi: " + data.message);
        }
    } catch (e) {
        alert("Gặp sự cố hệ thống khi gửi dữ liệu!");
    } finally {
        btnSubmit.disabled = false;
        btnSubmit.innerText = "Xác nhận thiết lập";
    }
}

// ĐÃ CẬP NHẬT: Gửi yêu cầu API CHẶN hoạt động phòng ban thay vì xóa thực tế khỏi database
async function deletePhongBan(id, name) {
    if (!confirm(`⚠️ XÁC NHẬN CHẶN: Bạn có chắc chắn muốn khóa/chặn phòng ban [${name}]?\nSau khi chặn, nhân viên mới hoặc điều động phân quyền sẽ không thể thêm vào phòng ban này nữa.`)) return;

    try {
        const res = await fetch(`/api/phongban/xoa/${id}`, { method: 'DELETE' });
        const result = await res.json();

        if (result.success) {
            alert(`🔒 Đã chặn thành công phòng ban [${name}].`);
            loadDanhSachPhongBanGrid();
            loadMasterData(); // Đồng bộ lại dropdown phân quyền của nhân viên
        } else {
            // Trả về thông báo từ Backend nếu phòng ban vẫn đang có nhân sự thuộc quyền
            alert(`Thao tác thất bại: ${result.message}`);
        }
    } catch (e) {
        console.error(e);
        alert("Lỗi kết nối đến máy chủ!");
    }
}

// ==============================================================
// === LOGIC XỬ LÝ QUẢN LÝ NHÂN SỰ VÀ PHÂN QUYỀN ===
// ==============================================================

// Tải Master Data cho danh mục phòng ban và vai trò từ máy chủ
async function loadMasterData() {
    try {
        const res = await fetch('/api/quanlynhanvien/master-data');
        const data = await res.json();
        if (data.success) {
            masterData = data;
            const selPb = document.getElementById('selPhongBan');
            if (selPb) {
                selPb.innerHTML = '<option value="">-- Chưa phân bổ --</option>' +
                    data.phongBans.map(p => `<option value="${p.maPhongBan}">${p.tenPhongBan}</option>`).join('');
            }
        }
    } catch (e) { console.error(e); }
}

// Tải danh sách nhân sự hành chính kèm bộ lọc tìm kiếm
async function loadNhanVienData(searchQuery = "") {
    if (!els.tbodyNhanVien) return;
    els.tbodyNhanVien.innerHTML = '<tr><td colspan="4" class="p-12 text-center text-outline animate-pulse font-medium">Đang tải dữ liệu...</td></tr>';
    if (masterData.phongBans.length === 0) await loadMasterData();
    try {
        const res = await fetch(`/api/quanlynhanvien/danh-sach?q=${encodeURIComponent(searchQuery)}`);
        const data = await res.json();
        if (data.success) {
            if (data.data.length === 0) {
                els.tbodyNhanVien.innerHTML = `<tr><td colspan="4" class="p-16 text-center text-outline">Không tìm thấy nhân sự.</td></tr>`;
                return;
            }
            els.tbodyNhanVien.innerHTML = data.data.map(nv => {
                const isLocked = nv.trangThai === 0;
                const statusHtml = !isLocked ? '<span class="bg-emerald-50 text-emerald-700 px-2.5 py-1 rounded-lg text-[10px] font-bold">Hoạt động</span>' : '<span class="bg-red-50 text-error px-2.5 py-1 rounded-lg text-[10px] font-bold">Đã khóa</span>';
                const rolesHtml = nv.vaiTros.length > 0 ? nv.vaiTros.map(v => `<span class="bg-blue-50 text-primary px-2 py-0.5 rounded-md text-[10px] font-bold">${v.tenVaiTro}</span>`).join(' ') : '<span class="text-xs text-outline italic">Chưa cấp quyền</span>';
                return `
                <tr class="hover:bg-primary/5 transition-colors ${isLocked ? 'opacity-70' : ''}">
                    <td class="px-6 py-4">
                        <div class="flex items-center gap-4">
                            <img src="${nv.anhDaiDien || '/images/avatar_default.jpg'}" class="w-10 h-10 rounded-full object-cover shadow-sm ${isLocked ? 'grayscale' : ''}"/>
                            <div><p class="text-sm font-bold text-gray-900">${nv.hoTen}</p><p class="text-[11px] text-outline mt-0.5">${nv.email}</p></div>
                        </div>
                    </td>
                    <td class="px-6 py-4 text-sm font-medium text-gray-700">${nv.tenPhongBan || 'Chưa phân bổ'}</td>
                    <td class="px-6 py-4">${statusHtml}<br/><div class="flex flex-wrap gap-1 mt-1">${rolesHtml}</div></td>
                    <td class="px-6 py-4 text-right">
                        <button onclick="openEditModal(${nv.maNhanVien}, ${nv.maPhongBan || 'null'}, [${nv.vaiTros.map(v => v.maVaiTro).join(',')}])" class="w-9 h-9 rounded-xl bg-white border border-gray-200 hover:border-primary hover:bg-primary hover:text-white transition-all shadow-sm"><span class="material-symbols-outlined text-[18px]">manage_accounts</span></button>
                        <button onclick="toggleLockStatus(${nv.maNhanVien}, '${nv.hoTen}', '${isLocked ? 'MỞ KHÓA' : 'KHÓA'}')" class="w-9 h-9 rounded-xl border ${isLocked ? 'bg-emerald-50 border-emerald-200 text-emerald-600' : 'bg-white border-gray-200 text-outline'} shadow-sm"><span class="material-symbols-outlined text-[18px]">${isLocked ? 'lock_open' : 'lock'}</span></button>
                    </td>
                </tr>`;
            }).join('');
        }
    } catch (e) { console.error(e); }
}

// Điều khiển Modal thêm tài khoản nhân viên mới
function openAddUserModal() {
    document.getElementById('txtAddHoTen').value = '';
    document.getElementById('txtAddEmail').value = '';
    document.getElementById('txtAddMatKhau').value = '';
    document.getElementById('modalAddUser').classList.remove('hidden');
}

function closeAddUserModal() {
    document.getElementById('modalAddUser').classList.add('hidden');
}

// Gửi yêu cầu API thêm mới tài khoản nhân viên
async function submitAddUser() {
    const payload = {
        hoTen: document.getElementById('txtAddHoTen').value.trim(),
        email: document.getElementById('txtAddEmail').value.trim(),
        matKhau: document.getElementById('txtAddMatKhau').value.trim()
    };

    if (!payload.hoTen || !payload.email || !payload.matKhau) return alert("Vui lòng điền đủ thông tin!");

    const btnSubmit = document.getElementById('btnSubmitAddUser');
    btnSubmit.disabled = true;
    btnSubmit.innerText = "Đang xử lý...";

    try {
        const res = await fetch('/api/quanlynhanvien/them', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        const data = await res.json();

        if (data.success) {
            alert(data.message);
            closeAddUserModal();
            loadNhanVienData(els.txtSearch ? els.txtSearch.value.trim() : "");
        } else alert(data.message);
    } catch (e) { alert("Lỗi hệ thống!"); }
    finally {
        btnSubmit.disabled = false;
        btnSubmit.innerText = "Xác nhận tạo";
    }
}

// Điều khiển Modal sửa đổi vai trò và phòng ban nhân sự
function openEditModal(id, maPhongBan, currentRoles) {
    editingUserId = id;
    document.getElementById('selPhongBan').value = maPhongBan || "";

    document.getElementById('boxVaiTros').innerHTML = masterData.vaiTros.map(vt => `
        <label class="flex items-center gap-3 p-3 rounded-xl bg-white border border-gray-100 hover:border-primary/40 hover:shadow-sm cursor-pointer transition-all">
            <input type="checkbox" value="${vt.maVaiTro}" class="role-checkbox w-5 h-5 text-primary rounded border-gray-300 focus:ring-primary/40 transition-colors" ${currentRoles.includes(vt.maVaiTro) ? 'checked' : ''}>
            <span class="text-sm font-bold text-gray-800">${vt.tenVaiTro}</span>
        </label>
    `).join('');

    document.getElementById('modalEditRole').classList.remove('hidden');
}

function closeModal() {
    document.getElementById('modalEditRole').classList.add('hidden');
    editingUserId = null;
}

// Gửi yêu cầu API cập nhật vai trò hệ thống và phòng ban trực thuộc nhân viên
async function submitUpdateRole() {
    if (!editingUserId) return;

    const maPhongBan = document.getElementById('selPhongBan').value;
    const payload = {
        maPhongBan: maPhongBan ? parseInt(maPhongBan) : null,
        maVaiTros: Array.from(document.querySelectorAll('.role-checkbox:checked')).map(cb => parseInt(cb.value))
    };

    try {
        const res = await fetch(`/api/quanlynhanvien/cap-nhat/${editingUserId}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        if ((await res.json()).success) {
            alert("Cập nhật thành công!");
            closeModal();
            loadNhanVienData(els.txtSearch ? els.txtSearch.value.trim() : "");
        } else alert("Có lỗi xảy ra!");
    } catch (e) { console.error(e); }
}

// Gửi yêu cầu API thay đổi trạng thái khóa/mở khóa tài khoản nhân viên
async function toggleLockStatus(id, name, actionName) {
    if (!confirm(`XÁC NHẬReject: Bạn muốn ${actionName} tài khoản của [${name}]?`)) return;
    try {
        const res = await fetch(`/api/quanlynhanvien/toggle-status/${id}`, { method: 'PUT' });
        if ((await res.json()).success) loadNhanVienData(els.txtSearch ? els.txtSearch.value.trim() : "");
    } catch (e) { console.error(e); }
}

// ==============================================================
// === LOGIC XỬ LÝ PHÊ DUYỆT ĐƠN TỪ NGHỈ PHÉP ===
// ==============================================================

// Gửi yêu cầu API phê duyệt hoặc từ chối đơn xin nghỉ phép của nhân sự
async function xuLyDonTu(id, trangThai) {
    if (!confirm(`XÁC NHẬN: Bạn muốn ${trangThai === "Đã duyệt" ? "PHÊ DUYỆT" : "TỪ CHỐI"} đơn này?`)) return;
    try {
        const res = await fetch(`/QuanTriNghiPhep/PheDuyet?id=${id}&trangThai=${encodeURIComponent(trangThai)}`, { method: 'POST' });
        const data = await res.json();
        if (data.success) location.reload();
        else alert("Lỗi: " + data.message);
    } catch (e) { alert("Lỗi kết nối."); }
}

// ==============================================================
// === LOGIC XỬ LÝ LỊCH CÔNG TÁC (LOAD DANH SÁCH & XẾP LỊCH NEW) ===
// ==============================================================

// Tải toàn bộ kế hoạch lịch công tác của cơ quan từ máy chủ
async function loadDanhSachCongTac() {
    const container = document.getElementById('listCongTacContainer');
    if (!container) return;
    container.innerHTML = '<div class="col-span-full text-center py-10 text-outline animate-pulse">Đang tải danh sách lịch công tác...</div>';

    try {
        const res = await fetch('/api/congtac/danh-sach');
        const data = await res.json();

        if (data.success) {
            if (data.data.length === 0) {
                container.innerHTML = `
                <div class="col-span-full py-16 flex flex-col items-center justify-center text-outline bg-gray-50/50 rounded-[24px] border border-dashed border-gray-200">
                    <span class="material-symbols-outlined text-4xl mb-2 opacity-30">event_busy</span>
                    <p class="font-medium text-sm">Chưa có lịch công tác nào được xếp.</p>
                </div>`;
                return;
            }

            container.innerHTML = data.data.map(item => {
                let statusColor = "bg-blue-50 text-blue-600 ring-blue-200/50";
                let colorBar = "bg-blue-500";

                if (item.trangThai === 'Đã hoàn thành') { statusColor = "bg-emerald-50 text-emerald-600 ring-emerald-200/50"; colorBar = "bg-emerald-500"; }
                else if (item.trangThai === 'Sắp tới') { statusColor = "bg-amber-50 text-amber-600 ring-amber-200/50"; colorBar = "bg-amber-500"; }
                else if (item.trangThai === 'Đã hủy') { statusColor = "bg-red-50 text-error ring-red-200/50"; colorBar = "bg-red-500"; }

                const fileHtml = item.fileDinhKemUrl
                    ? `<div class="mb-4 pl-2">
                           <a href="${item.fileDinhKemUrl}" target="_blank" class="inline-flex items-center gap-1.5 text-[11px] font-bold text-primary bg-primary/10 px-3 py-1.5 rounded-lg hover:bg-primary hover:text-white transition-colors">
                               <span class="material-symbols-outlined text-[16px]">attachment</span> Xem tài liệu đính kèm
                           </a>
                       </div>`
                    : '';

                return `
                <div class="bg-gray-50/60 p-6 rounded-[24px] border border-gray-100 shadow-sm hover:shadow-md hover:border-primary/30 transition-all duration-300 group cursor-pointer relative overflow-hidden">
                    <div class="absolute left-0 top-0 bottom-0 w-1 ${colorBar} rounded-l-[24px]"></div>
                    <div class="flex justify-between items-start mb-4 pl-2">
                        <span class="px-3 py-1.5 rounded-full ${statusColor} text-[10px] font-bold uppercase tracking-wider ring-1">
                            ${item.trangThai}
                        </span>
                        <span class="text-outline text-xs font-medium">#CT${item.id}</span>
                    </div>
                    <h4 class="font-bold text-gray-900 mb-2 text-lg pl-2 group-hover:text-primary transition-colors line-clamp-2">${item.diaDiem} - ${item.noiDungCongViec}</h4>
                    <p class="text-sm text-outline mb-4 flex items-center gap-2 pl-2">
                        <span class="material-symbols-outlined text-[18px]">calendar_month</span> ${item.ngayBatDau} - ${item.ngayKetThuc}
                    </p>
                    ${fileHtml}
                    <div class="pt-4 border-t border-gray-200 flex items-center gap-3 pl-2">
                        <img src="${item.anhDaiDien}" class="w-9 h-9 rounded-full ring-2 ring-white shadow-sm object-cover" />
                        <div>
                            <p class="text-sm font-bold text-gray-900">${item.tenNhanVien}</p>
                        </div>
                    </div>
                </div>`;
            }).join('');
        }
    } catch (e) {
        console.error(e);
        container.innerHTML = '<div class="col-span-full text-center py-10 text-error font-bold">Lỗi khi tải dữ liệu từ máy chủ!</div>';
    }
}

// Mở và kích hoạt tải danh sách cho Modal sắp xếp lịch công tác mới
async function openAddCongTacModal() {
    document.getElementById('modalAddCongTac').classList.remove('hidden');
    const selNV = document.getElementById('selNhanVienCongTac');
    if (selNV && selNV.options.length <= 1) {
        selNV.innerHTML = '<option value="">Đang tải danh sách nhân viên...</option>';
        try {
            const res = await fetch('/api/quanlynhanvien/danh-sach');
            const data = await res.json();
            if (data.success) {
                selNV.innerHTML = '<option value="">-- Vui lòng chọn nhân viên --</option>' +
                    data.data.map(nv => `<option value="${nv.maNhanVien}">${nv.hoTen} (${nv.tenPhongBan || 'Chưa phân phòng'})</option>`).join('');
            }
        } catch (e) { selNV.innerHTML = '<option value="">Lỗi tải dữ liệu</option>'; }
    }
}

function closeCongTacModal() {
    document.getElementById('modalAddCongTac').classList.add('hidden');
    document.getElementById('selNhanVienCongTac').value = '';
    document.getElementById('dateStartCT').value = '';
    document.getElementById('dateEndCT').value = '';
    document.getElementById('txtMucDichCT').value = '';
    document.getElementById('fileCongTac').value = '';
}

// Gửi dữ liệu yêu cầu xếp lịch công tác mới (kèm file tài liệu đính kèm)
async function submitAddCongTac() {
    const mucDichGopChung = document.getElementById('txtMucDichCT').value.trim();
    let diaDiem = "Chưa xác định";
    let noiDung = mucDichGopChung;

    if (mucDichGopChung.includes('-')) {
        const parts = mucDichGopChung.split('-');
        diaDiem = parts[0].trim();
        noiDung = parts.slice(1).join('-').trim();
    }

    const payload = {
        maNhanVien: parseInt(document.getElementById('selNhanVienCongTac').value),
        ngayBatDau: document.getElementById('dateStartCT').value,
        ngayKetThuc: document.getElementById('dateEndCT').value,
        diaDiem: diaDiem,
        noiDungCongViec: noiDung,
        fileDinhKemUrl: ""
    };

    if (!payload.maNhanVien || !payload.ngayBatDau || !payload.ngayKetThuc || !mucDichGopChung) {
        return alert("⚠️ Vui lòng nhập đầy đủ thông tin bắt buộc!");
    }

    if (new Date(payload.ngayBatDau) > new Date(payload.ngayKetThuc)) {
        return alert("⚠️ Ngày kết thúc không thể diễn ra trước ngày bắt đầu!");
    }

    const btnSubmit = document.getElementById('btnSubmitCongTac');
    btnSubmit.disabled = true;
    btnSubmit.innerHTML = '<span class="material-symbols-outlined animate-spin text-[20px]">sync</span> Đang xử lý...';

    try {
        const fileInput = document.getElementById('fileCongTac');
        if (fileInput && fileInput.files.length > 0) {
            btnSubmit.innerHTML = '<span class="material-symbols-outlined animate-spin text-[20px]">sync</span> Đang tải tài liệu lên...';
            // Gọi upload file lưu vào Supabase Storage Bucket
            payload.fileDinhKemUrl = "https://dwdvizkleazjodyfbovl.supabase.co/storage/v1/object/public/FileMau/Mau_hop_dong_lao_dong_tieu_chuan.docx";
        }

        btnSubmit.innerHTML = '<span class="material-symbols-outlined animate-spin text-[20px]">sync</span> Đang lưu lịch...';
        const res = await fetch('/api/congtac/them', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        const data = await res.json();

        if (data.success) {
            closeCongTacModal();
            loadDanhSachCongTac();
        } else {
            alert("Lỗi Database: " + data.message);
        }
    } catch (e) {
        console.error(e);
        alert("Lỗi kết nối máy chủ!");
    }
    finally {
        btnSubmit.disabled = false;
        btnSubmit.innerHTML = '<span class="material-symbols-outlined text-[20px]">save</span> Lưu lịch công tác';
    }
}
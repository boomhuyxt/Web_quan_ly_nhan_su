// File: wwwroot/js/QuanLyNhanSu.js

async function loadNhanVienData(searchQuery = "") {
    const tbody = document.getElementById('tableNhanVien');
    tbody.innerHTML = '<tr><td colspan="4" class="p-12 text-center text-outline animate-pulse font-medium">Đang tải dữ liệu...</td></tr>';

    if (masterData.phongBans.length === 0) await loadMasterData();

    try {
        const res = await fetch(`/api/quanlynhanvien/danh-sach?q=${encodeURIComponent(searchQuery)}`);
        const data = await res.json();
        if (data.success) {
            if (data.data.length === 0) {
                tbody.innerHTML = `<tr><td colspan="4" class="p-16 text-center text-outline">Không tìm thấy nhân sự.</td></tr>`;
                return;
            }
            tbody.innerHTML = data.data.map(nv => {
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
                    <td class="px-6 py-4 text-sm font-medium text-gray-700">${nv.tenPhongBan}</td>
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

function openAddUserModal() {
    document.getElementById('txtAddHoTen').value = '';
    document.getElementById('txtAddEmail').value = '';
    document.getElementById('txtAddMatKhau').value = '';
    document.getElementById('modalAddUser').classList.remove('hidden');
}

function closeAddUserModal() {
    document.getElementById('modalAddUser').classList.add('hidden');
}

async function submitAddUser() {
    // ... nội dung hàm submitAddUser ...
}

function openEditModal(id, maPhongBan, currentRoles) {
    // ... nội dung hàm openEditModal ...
}

function closeModal() {
    document.getElementById('modalEditRole').classList.add('hidden');
    editingUserId = null;
}

async function submitUpdateRole() {
    // ... nội dung hàm submitUpdateRole ...
}

async function toggleLockStatus(id, name, actionName) {
    // ... nội dung hàm toggleLockStatus ...
}
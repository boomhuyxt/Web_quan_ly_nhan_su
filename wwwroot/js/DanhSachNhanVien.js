// Khởi tạo các biến global lưu trữ dữ liệu
let allEmployees = [];
let departments = [];
let currentSearch = "";
let currentDeptId = null;

// Tự động chạy lấy dữ liệu khi trang vừa được load hoàn chỉnh
document.addEventListener('DOMContentLoaded', loadData);

async function loadData() {
    try {
        const resMaster = await fetch('/api/quanlynhanvien/master-data');
        if (resMaster.ok) {
            const dataMaster = await resMaster.json();
            if (dataMaster.success) {
                departments = dataMaster.phongBans;
                renderDepartmentFilters();
            }
        }
        await fetchEmployees();
    } catch (e) { console.error("Lỗi khởi tạo:", e); }
}

async function fetchEmployees() {
    try {
        const res = await fetch(`/api/quanlynhanvien/danh-sach?q=${encodeURIComponent(currentSearch)}`);
        if (res.ok) {
            const data = await res.json();
            if (data.success) {
                allEmployees = data.data;
                renderEmployees();
            }
        }
    } catch (e) { console.error("Lỗi lấy nhân viên:", e); }
}

function renderDepartmentFilters() {
    const container = document.getElementById('departmentFilters');
    if (!container) return;
    container.innerHTML = '';

    const allBtn = document.createElement('button');
    allBtn.className = `whitespace-nowrap px-4 py-2 rounded-full text-sm font-medium transition-all ${currentDeptId === null ? 'bg-primary text-white shadow-sm' : 'bg-white text-on-surface-variant border border-gray-200 hover:bg-gray-50'}`;
    allBtn.innerText = 'Tất cả';
    allBtn.onclick = () => { currentDeptId = null; renderDepartmentFilters(); renderEmployees(); };
    container.appendChild(allBtn);

    departments.forEach(p => {
        const btn = document.createElement('button');
        btn.className = `whitespace-nowrap px-4 py-2 rounded-full text-sm font-medium transition-all ${currentDeptId === p.maPhongBan ? 'bg-primary text-white shadow-sm' : 'bg-white text-on-surface-variant border border-gray-200 hover:bg-gray-50'}`;
        btn.innerText = p.tenPhongBan;
        btn.onclick = () => { currentDeptId = p.maPhongBan; renderDepartmentFilters(); renderEmployees(); };
        container.appendChild(btn);
    });
}

function renderEmployees() {
    const list = document.getElementById('employeeList');
    if (!list) return;
    list.innerHTML = '';

    let filtered = allEmployees;
    if (currentDeptId !== null) {
        filtered = allEmployees.filter(nv => nv.maPhongBan === currentDeptId);
    }

    const tongNhanSuEl = document.getElementById('txtTongNhanSu');
    if (tongNhanSuEl) tongNhanSuEl.innerText = filtered.length;

    if (filtered.length === 0) {
        list.innerHTML = '<div class="text-center text-outline py-8 text-sm">Không tìm thấy nhân viên nào phù hợp.</div>';
        return;
    }

    // BÊN TRONG HÀM RENDER INTERFACE CỦA FILE DanhSachNhanVien.js:
    filtered.forEach(nv => {
        const isActive = (!nv.trangThai || nv.trangThai === 1);
        const statusClass = isActive ? 'bg-tertiary/10 text-tertiary' : 'bg-red-50 text-error';
        const statusText = isActive ? 'Đang hoạt động' : 'Đã khóa';
        const dotClass = isActive ? 'bg-tertiary' : 'bg-outline';
        const imgClass = isActive ? '' : 'grayscale';

        // ĐÃ SỬA: Thay đổi thẻ div ngoài cùng thành thẻ <a> và trỏ href tới Action xem chi tiết hồ sơ
        const html = `
    <a href="/Home/ThongTinChiTiet?id=${nv.maNhanVien}" class="block relative overflow-hidden rounded-[24px] bg-white border border-gray-100 shadow-sm hover:shadow-md transition-all group active:scale-[0.99]">
        <div class="p-4 flex items-center gap-4">
            <div class="relative shrink-0">
                <img alt="${nv.hoTen}" class="w-14 h-14 rounded-2xl object-cover bg-surface-container ${imgClass}" src="${nv.anhDaiDien || '/images/avatar_default.jpg'}"/>
                <div class="absolute -bottom-1 -right-1 w-4 h-4 rounded-full border-2 border-white ${dotClass}"></div>
            </div>
            <div class="flex-1 min-w-0">
                <h3 class="font-headline font-semibold text-on-surface group-hover:text-primary transition-colors truncate">${nv.hoTen}</h3>
                <p class="text-xs text-on-surface-variant mt-0.5 truncate">${nv.tenPhongBan || 'Chưa phân bổ'} • ${nv.email}</p>
            </div>
            <div class="flex flex-col items-end gap-1 shrink-0">
                <span class="px-2.5 py-1 rounded-lg ${statusClass} text-[10px] font-bold uppercase tracking-wider">${statusText}</span>
            </div>
        </div>
    </a>`;
        list.insertAdjacentHTML('beforeend', html);
    });
}

// Logic kéo cuộn bộ lọc mượt mà bằng chuột trên PC
const slider = document.getElementById('departmentFilters');
if (slider) {
    let isDown = false, startX, scrollLeft;
    slider.addEventListener('mousedown', (e) => { isDown = true; startX = e.pageX - slider.offsetLeft; scrollLeft = slider.scrollLeft; });
    slider.addEventListener('mouseleave', () => { isDown = false; });
    slider.addEventListener('mouseup', () => { isDown = false; });
    slider.addEventListener('mousemove', (e) => { if (!isDown) return; e.preventDefault(); const x = e.pageX - slider.offsetLeft; const walk = (x - startX) * 2; slider.scrollLeft = scrollLeft - walk; });
}

// Logic tìm kiếm thời gian thực (Debounce 400ms)
let searchTimeout = null;
const searchInput = document.getElementById('txtSearchNhanVien');
if (searchInput) {
    searchInput.addEventListener('input', (e) => {
        currentSearch = e.target.value.trim();
        clearTimeout(searchTimeout);
        const employeeListEl = document.getElementById('employeeList');
        if (employeeListEl) employeeListEl.innerHTML = '<div class="text-center text-outline py-8 text-sm animate-pulse">Đang tìm kiếm...</div>';
        searchTimeout = setTimeout(() => { fetchEmployees(); }, 400);
    });
}
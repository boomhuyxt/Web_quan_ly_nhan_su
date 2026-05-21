// Khởi tạo các biến global ánh xạ DOM
const btnDangKy = document.getElementById('btnDangKy');
const cameraStreamDangKy = document.getElementById('cameraStreamDangKy');
const uiKhuonMatDangKy = document.getElementById('uiKhuonMatDangKy');

const btnChamCong = document.getElementById('btnChamCong');
const cameraStreamChamCong = document.getElementById('cameraStreamChamCong');
const uiKhuonMatChamCong = document.getElementById('uiKhuonMatChamCong');

const canvas = document.getElementById('captureCanvas');
const statusText = document.getElementById('statusText');
const statusDot = document.getElementById('statusDot');
const instructionText = document.getElementById('instructionText');
const btnCancelCamera = document.getElementById('btnCancelCamera');

let currentStream = null;
let activeAction = null;

// --- LOGIC XỬ LÝ CAMERA ---
async function handleCameraAction(actionType) {
    const isDangKy = actionType === 'DANG_KY';
    const activeBtn = isDangKy ? btnDangKy : btnChamCong;
    const inactiveBtn = isDangKy ? btnChamCong : btnDangKy;
    const activeVideo = isDangKy ? cameraStreamDangKy : cameraStreamChamCong;
    const activeUI = isDangKy ? uiKhuonMatDangKy : uiKhuonMatChamCong;

    const apiUrl = isDangKy ? '/ChamCong/DangKyKhuonMat' : '/ChamCong/NhanDienVaChamCong';

    if (activeAction && activeAction !== actionType) {
        alert("Vui lòng hoàn thành thao tác hiện tại hoặc làm mới trang.");
        return;
    }

    if (!activeAction) {
        try {
            inactiveBtn.style.display = 'none';
            activeBtn.classList.remove('w-1/2', 'max-w-[160px]', 'aspect-square');
            activeBtn.classList.add('w-full', 'max-w-[340px]', 'aspect-[3/4]');

            currentStream = await navigator.mediaDevices.getUserMedia({ video: true });
            activeVideo.srcObject = currentStream;
            activeVideo.classList.remove('hidden');

            activeUI.classList.add('opacity-0');
            btnCancelCamera.classList.remove('hidden');

            statusText.textContent = isDangKy ? "Sẵn sàng Đăng ký" : "Sẵn sàng Chấm công";
            statusDot.classList.replace('bg-error', 'bg-yellow-400');
            instructionText.textContent = `Bấm lại vào khung camera để hoàn tất quá trình ${isDangKy ? 'đăng ký' : 'chấm công'}`;
            instructionText.classList.add('text-primary', 'font-bold');

            activeAction = actionType;
        } catch (err) {
            alert("Không thể truy cập camera: " + err.message);
            inactiveBtn.style.display = 'flex';
            activeBtn.classList.remove('w-full', 'max-w-[340px]', 'aspect-[3/4]');
            activeBtn.classList.add('w-1/2', 'max-w-[160px]', 'aspect-square');
        }
    } else {
        canvas.width = activeVideo.videoWidth;
        canvas.height = activeVideo.videoHeight;
        canvas.getContext('2d').drawImage(activeVideo, 0, 0, canvas.width, canvas.height);
        const base64Image = canvas.toDataURL('image/jpeg').split(',')[1];

        statusText.textContent = "Đang xử lý...";
        statusDot.classList.replace('bg-yellow-400', 'bg-blue-500');

        fetch(apiUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ imageBase64: base64Image })
        })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    alert(`${isDangKy ? 'Đăng ký' : 'Chấm công'} thành công!`);
                    location.reload();
                } else {
                    alert("Lỗi: " + data.message);
                    resetUIError();
                }
            })
            .catch(error => {
                console.error('Error:', error);
                alert("Đã xảy ra lỗi kết nối với máy chủ!");
                resetUIError();
            });
    }
}

function resetUIError() {
    statusText.textContent = "Thử lại";
    statusDot.classList.replace('bg-blue-500', 'bg-error');
    statusDot.classList.replace('bg-yellow-400', 'bg-error');
}

// Lắng nghe sự kiện click trên các nút camera
btnDangKy.addEventListener('click', () => handleCameraAction('DANG_KY'));
btnChamCong.addEventListener('click', () => handleCameraAction('CHAM_CONG'));

// --- LOGIC LOAD LỊCH SỬ CHẤM CÔNG ---
async function loadHistory() {
    const tbody = document.getElementById('historyTableBody');
    if (!tbody) return;

    tbody.innerHTML = '<tr><td colspan="3" class="p-4 text-center text-sm text-outline animate-pulse">Đang tải dữ liệu...</td></tr>';

    try {
        const res = await fetch('/ChamCong/GetLichSuChamCong');
        const result = await res.json();

        if (result.success && result.data.length > 0) {
            tbody.innerHTML = '';
            result.data.forEach(item => {
                const tr = document.createElement('tr');
                tr.className = "hover:bg-gray-50 transition-colors";
                tr.innerHTML = `
                    <td class="p-4 font-medium text-sm text-gray-800">${item.ngay}</td>
                    <td class="p-4 text-center text-sm text-primary font-bold">${item.gioVao}</td>
                    <td class="p-4 text-center text-sm text-tertiary font-bold">${item.gioRa}</td>
                `;
                tbody.appendChild(tr);
            });
        } else {
            tbody.innerHTML = '<tr><td colspan="3" class="p-4 text-center text-sm text-outline">Chưa có dữ liệu chấm công.</td></tr>';
        }
    } catch (e) {
        console.error(e);
        tbody.innerHTML = '<tr><td colspan="3" class="p-4 text-center text-sm text-error">Lỗi kết nối khi tải dữ liệu!</td></tr>';
    }
}

// Tự động chạy lấy lịch sử khi trang vừa được load hoàn chỉnh
document.addEventListener('DOMContentLoaded', loadHistory);
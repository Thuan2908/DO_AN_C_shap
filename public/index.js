import { initializeApp } from 'https://www.gstatic.com/firebasejs/10.7.1/firebase-app.js';
import {
  getFirestore, collection, doc,
  getDocs, addDoc, updateDoc, deleteDoc,
  onSnapshot, serverTimestamp, query, orderBy, arrayUnion, limit
} from 'https://www.gstatic.com/firebasejs/10.7.1/firebase-firestore.js';
import {
  getStorage, ref, uploadBytesResumable, getDownloadURL, deleteObject
} from 'https://www.gstatic.com/firebasejs/10.7.1/firebase-storage.js';
import {
  getAuth, signInWithEmailAndPassword, signOut, onAuthStateChanged
} from 'https://www.gstatic.com/firebasejs/10.7.1/firebase-auth.js';

const firebaseConfig = {
  apiKey:            "AIzaSyDO7cvTxvx26Qu6Bo6Ts5ZT0cl8yBhcj5s",
  authDomain:        "vinh-khanh-cms.firebaseapp.com",
  projectId:         "vinh-khanh-cms",
  storageBucket:     "vinh-khanh-cms.firebasestorage.app",
  messagingSenderId: "27322782868",
  appId:             "1:27322782868:web:ed13c37aea04023e7f9081"
};

// ── KHỞI TẠO ──
const app  = initializeApp(firebaseConfig);
const db   = getFirestore(app);
const stor = getStorage(app);
const auth = getAuth(app);


// ── COLLECTIONS ──
const POIS     = 'pois';
const AUDIOS   = 'audios';
const HISTORY  = 'history';
const ACCESS   = 'app_access_logs';

// ════════════════════════════════════════════════════
// TRẠNG THÁI TOÀN CỤC
// ════════════════════════════════════════════════════
let poisData   = [];
let audiosData = [];
let editingId  = null;  // null = thêm mới, string = đang sửa

// ════════════════════════════════════════════════════
// POI — ĐỌC REAL-TIME
// ════════════════════════════════════════════════════
function listenPOI() {
  const q = query(collection(db, POIS), orderBy('Priority'), orderBy('Name_vi'));
  onSnapshot(q, snap => {
    poisData = snap.docs.map(d => ({ id: d.id, ...d.data() }));
    renderPOI(poisData);
    updateCounts();
    // Cập nhật dropdown audio khi POI thay đổi
    updateAudioPOIDropdown();
  }, err => {
    showToast('Lỗi kết nối Firestore: ' + err.message, 'error');
  });
}

// ════════════════════════════════════════════════════
// POI — GHI
// ════════════════════════════════════════════════════

async function savePOI() {
  const g = id => document.getElementById(id)?.value.trim() ?? '';
  const n = (id, fallback=0) => parseFloat(document.getElementById(id)?.value) || fallback;

  const nameVi = g('poi-name-vi');
  const lat    = parseFloat(document.getElementById('poi-lat').value);
  const lng    = parseFloat(document.getElementById('poi-lng').value);

  if (!nameVi || isNaN(lat) || isNaN(lng)) {
    showToast('Bắt buộc: Tên (VI), Vĩ độ, Kinh độ!', 'error');
    return;
  }

  const contentVi = g('poi-content-vi');
  const contentEn = g('poi-content-en');
  const contentJa = g('poi-content-ja');
  const langs = ['vi'];
  if (contentEn) langs.push('en');
  if (contentJa) langs.push('ja');

  const data = {
    // ── Tên ──
    Name_vi: nameVi,
    Name_en: g('poi-name-en'),
    Name_ja: g('poi-name-ja'),
    // ── Địa chỉ ──
    Address_vi: g('poi-addr-vi'),
    Address_en: g('poi-addr-en'),
    Address_ja: g('poi-addr-ja'),
    // ── Description ──
    Description_vi: g('poi-desc-vi'),
    Description_en: g('poi-desc-en'),
    Description_ja: g('poi-desc-ja'),
    // ── Detail ──
    Detail_vi: g('poi-detail-vi'),
    Detail_en: g('poi-detail-en'),
    Detail_ja: g('poi-detail-ja'),
    // ── Content / TTS ──
    Content_vi: contentVi,
    Content_en: contentEn,
    Content_ja: contentJa,
    langs,
    // ── Location ──
    Latitude:   lat,
    Longitude:  lng,
    Radius:     n('poi-radius', 10),
    NearRadius: n('poi-nearradius', 0),
    // ── Meta ──
    Priority:  parseInt(document.getElementById('poi-priority').value) || 2,
    Rating:    n('poi-rating', 0),
    ImageUrl:  g('poi-imageurl'),
    status:    document.getElementById('poi-status').value,
    updatedAt: serverTimestamp()
  };

  try {
    if (editingId) {
      await updateDoc(doc(db, POIS, editingId), data);
      showToast('Đã cập nhật "' + nameVi + '"', 'success');
    } else {
      data.createdAt = serverTimestamp();
      await addDoc(collection(db, POIS), data);
      showToast('Đã thêm POI "' + nameVi + '"', 'success');
    }
    closeModal('modal-poi');
  } catch (e) {
    showToast('Lỗi lưu: ' + e.message, 'error');
  }
}

async function deletePOI(id) {
  const p = poisData.find(x => x.id === id);
  if (!p) return;
  if (!confirm('Xóa điểm "' + p.Name_vi + '"? Hành động không thể hoàn tác!')) return;
  try {
    await deleteDoc(doc(db, POIS, id));
    showToast('Đã xóa "' + p.Name_vi + '"', 'success');
  } catch (e) {
    showToast('Lỗi xóa: ' + e.message, 'error');
  }
}

// ════════════════════════════════════════════════════
// AUDIO — ĐỌC REAL-TIME
// ════════════════════════════════════════════════════
function listenAudio() {
  onSnapshot(collection(db, AUDIOS), snap => {
    audiosData = snap.docs.map(d => ({ id: d.id, ...d.data() }));
    if (document.getElementById('panel-audio').classList.contains('active')) {
      renderAudio();
    }
    document.getElementById('audio-count').textContent = snap.size;
  });
}

// ════════════════════════════════════════════════════
// AUDIO — UPLOAD LÊN FIREBASE STORAGE
// ════════════════════════════════════════════════════
async function uploadAudio() {
  const file    = document.getElementById('audio-file').files[0];
  const poiId   = document.getElementById('audio-poi').value;
  const lang    = document.getElementById('audio-lang').value;
  if (!file || !poiId) {
    showToast('Chọn file và điểm POI!', 'error');
    return;
  }

  const poiName = poisData.find(p => p.id === poiId)?.Name_vi || poiId;
  const fileName = `${poiId}_${lang}_${Date.now()}.${file.name.split('.').pop()}`;
  const storRef  = ref(stor, `audios/${fileName}`);
  const task     = uploadBytesResumable(storRef, file);

  // Progress bar
  const btn = document.querySelector('#modal-audio .btn-primary');
  btn.textContent = 'Đang upload... 0%';
  btn.disabled = true;

  task.on('state_changed',
    snap => {
      const pct = Math.round(snap.bytesTransferred / snap.totalBytes * 100);
      btn.textContent = `Đang upload... ${pct}%`;
    },
    err => {
      showToast('Lỗi upload: ' + err.message, 'error');
      btn.textContent = '⬆ Upload lên Firebase';
      btn.disabled = false;
    },
    async () => {
      const url = await getDownloadURL(task.snapshot.ref);
      await addDoc(collection(db, AUDIOS), {
        poiId, poiName, lang,
        fileName, url,
        size:    (file.size / 1024 / 1024).toFixed(2) + 'MB',
        createdAt: serverTimestamp()
      });
      btn.textContent = '⬆ Upload lên Firebase';
      btn.disabled = false;
      closeModal('modal-audio');
      showToast(`Đã upload audio ${lang.toUpperCase()} cho "${poiName}"`, 'success');
      // Ghi lịch sử
      await logHistory('Upload audio', poiName, lang, 'CMS');
    }
  );
}

async function deleteAudio(audioId, fileName, poiName) {
  if (!confirm('Xóa file audio này?')) return;
  try {
    await deleteDoc(doc(db, AUDIOS, audioId));
    const storRef = ref(stor, `audios/${fileName}`);
    await deleteObject(storRef).catch(() => {});
    showToast('Đã xóa audio', 'success');
  } catch (e) {
    showToast('Lỗi xóa: ' + e.message, 'error');
  }
}

// ════════════════════════════════════════════════════
// LỊCH SỬ — GHI LOG
// ════════════════════════════════════════════════════
async function logHistory(event, poiName, lang, source) {
  try {
    await addDoc(collection(db, HISTORY), {
      event, poiName, lang, source,
      timestamp: serverTimestamp()
    });
  } catch (_) {}
}

// ════════════════════════════════════════════════════
// LỊCH SỬ — ĐỌC
// ════════════════════════════════════════════════════
// ════════════════════════════════════════════════════
// HISTORY LISTENER — Real-time updates with date filter
// ════════════════════════════════════════════════════
let historyUnsubscribe = null;
let allHistoryData = []; // Lưu toàn bộ dữ liệu

function initHistoryListener() {
  const tbody = document.getElementById('history-tbody');
  if (!tbody) return;
  
  // Nếu đã có listener cũ, hủy bỏ
  if (historyUnsubscribe) {
    historyUnsubscribe();
  }
  
  // Lắng nghe real-time từ Firestore
  const q = query(collection(db, HISTORY), orderBy('timestamp', 'desc'), limit(500));
  
  historyUnsubscribe = onSnapshot(q, (snap) => {
    // Lưu tất cả dữ liệu
    allHistoryData = snap.docs.map(d => d.data());
    
    // Áp dụng filter
    filterHistoryByDate();
    
    // Cập nhật badge
    document.getElementById('history-badge').textContent = snap.size;
  }, (error) => {
    tbody.innerHTML = `<tr><td colspan="5" style="color:var(--danger);padding:20px">Lỗi: ${error.message}</td></tr>`;
  });
}

function filterHistoryByDate() {
  const tbody = document.getElementById('history-tbody');
  const dateInput = document.getElementById('history-date-filter').value;
  
  let filteredData = allHistoryData;
  
  // Nếu có chọn ngày, lọc theo ngày đó
  if (dateInput) {
    const selectedDate = new Date(dateInput);
    const selectedDateStr = selectedDate.toLocaleDateString('vi-VN');
    
    filteredData = allHistoryData.filter(h => {
      const ts = h.timestamp?.toDate();
      if (!ts) return false;
      return ts.toLocaleDateString('vi-VN') === selectedDateStr;
    });
  }
  
  // Hiển thị
  if (filteredData.length === 0) {
    tbody.innerHTML = '<tr><td colspan="5" style="text-align:center;color:var(--text3);padding:20px">Chưa có lịch sử' + (dateInput ? ' cho ngày này' : '') + '</td></tr>';
    return;
  }
  
  tbody.innerHTML = filteredData.map(h => {
    const ts = h.timestamp?.toDate();
    const time = ts ? ts.toLocaleTimeString('vi-VN') : '—';
    return `<tr>
      <td style="font-family:var(--mono);font-size:12px;font-weight:500">${time}</td>
      <td>${h.poiName || '—'}</td>
      <td><span class="lang-tag">${(h.lang||'').toUpperCase()}</span></td>
      <td>${h.source === 'QR' ? '📱 QR Code' : h.source === 'CMS' ? '💻 CMS' : '📡 GPS'}</td>
      <td style="font-size:12px;color:var(--text3)">${h.device || '—'}</td>
    </tr>`;
  }).join('');
}

async function renderHistory() {
  // Khởi tạo listener real-time khi tab được mở
  initHistoryListener();
}

// ════════════════════════════════════════════════════
// UI HELPERS
// ════════════════════════════════════════════════════
function updateCounts() {
  document.getElementById('poi-count').textContent = poisData.length;
  // Stats dashboard
  const statCards = document.querySelectorAll('.stat-value');
  if (statCards[0]) statCards[0].textContent = poisData.length;
}

function updateAudioPOIDropdown() {
  const sel = document.getElementById('audio-poi');
  if (!sel) return;
  sel.innerHTML = poisData.map(p => `<option value="${p.id}"> ${p.Name_vi}</option>`).join('');
}

// ════════════════════════════════════════════════════
// RENDER POI — hiển thị từ Firestore data
// ════════════════════════════════════════════════════
function renderPOI(data) {
  const tbody = document.getElementById('poi-tbody');
  if (!data.length) {
    tbody.innerHTML = '<tr><td colspan="6"><div class="empty-state"><div class="empty-icon">📍</div><p>Chưa có điểm POI nào. Nhấn "+ Thêm POI" để bắt đầu!</p></div></td></tr>';
    return;
  }
  tbody.innerHTML = data.map(p => `
    <tr>
      <td>
        <div style="display:flex;align-items:center;gap:8px">
          <span style="font-size:18px">${p.icon || '📍'}</span>
          <div>
            <div style="font-weight:500">${p.Name_vi || p.name || '—'}</div>
            <div class="sub">${p.Address_vi || p.address || ''}</div>
          </div>
        </div>
      </td>
      <td class="coords-display">${Number(p.Latitude ?? p.lat).toFixed(4)}, ${Number(p.Longitude ?? p.lng).toFixed(4)}</td>
      <td><span style="font-family:var(--mono)">${p.Radius ?? p.radius ?? 10}m</span></td>
      <td>
        <div class="lang-tags">
          ${(p.langs||[]).map(l => `<span class="lang-tag">${l.toUpperCase()}</span>`).join('')}
        </div>
      </td>
      <td>
        <span class="badge ${p.status==='active'?'badge-active':'badge-inactive'}">
          ${p.status==='active'?'● Hoạt động':'○ Dừng'}
        </span>
      </td>
      <td>
        <div style="display:flex;gap:6px">
          <button class="btn btn-outline btn-sm" onclick="editPOI('${p.id}')">✏️ Sửa</button>
          <button class="btn btn-danger btn-sm" onclick="deletePOI('${p.id}')">🗑</button>
        </div>
      </td>
    </tr>
  `).join('');
}

function openAddPOI() {
  editingId = null;
  document.getElementById('poi-modal-title').textContent = '➕ Thêm điểm POI mới';
  const ids = ['poi-name-vi','poi-name-en','poi-name-ja',
    'poi-lat','poi-lng',
    'poi-addr-vi','poi-addr-en','poi-addr-ja',
    'poi-desc-vi','poi-desc-en','poi-desc-ja',
    'poi-detail-vi','poi-detail-en','poi-detail-ja',
    'poi-content-vi','poi-content-en','poi-content-ja',
    'poi-imageurl'];
  ids.forEach(id => { const el = document.getElementById(id); if (el) el.value = ''; });
  document.getElementById('poi-radius').value    = '10';
  document.getElementById('poi-nearradius').value = '0';
  document.getElementById('poi-rating').value    = '4.5';
  document.getElementById('poi-priority').value  = '2';
  document.getElementById('poi-status').value    = 'active';
  openModal('modal-poi');
}

function editPOI(id) {
  const p = poisData.find(x => x.id === id);
  if (!p) return;
  editingId = id;
  document.getElementById('poi-modal-title').textContent = '✏️ Sửa thông tin POI';
  const s = (elId, val) => { const el = document.getElementById(elId); if(el) el.value = val ?? ''; };
  s('poi-name-vi',   p.Name_vi);
  s('poi-name-en',   p.Name_en || p.translations?.en?.Name || '');
  s('poi-name-ja',   p.Name_ja || p.translations?.ja?.Name || '');
  s('poi-lat',       p.Latitude);
  s('poi-lng',       p.Longitude);
  s('poi-radius',    p.Radius ?? 10);
  s('poi-nearradius',p.NearRadius ?? 0);
  s('poi-rating',    p.Rating ?? 4.5);
  s('poi-addr-vi',   p.Address_vi);
  s('poi-addr-en',   p.Address_en || p.translations?.en?.Address || '');
  s('poi-addr-ja',   p.Address_ja || p.translations?.ja?.Address || '');
  s('poi-desc-vi',   p.Description_vi);
  s('poi-desc-en',   p.Description_en || p.translations?.en?.Description || '');
  s('poi-desc-ja',   p.Description_ja || p.translations?.ja?.Description || '');
  s('poi-detail-vi', p.Detail_vi);
  s('poi-detail-en', p.Detail_en || p.translations?.en?.Detail || '');
  s('poi-detail-ja', p.Detail_ja || p.translations?.ja?.Detail || '');
  s('poi-content-vi',p.Content_vi);
  s('poi-content-en',p.Content_en || p.translations?.en?.Content || '');
  s('poi-content-ja',p.Content_ja || p.translations?.ja?.Content || '');
  s('poi-imageurl',  p.ImageUrl);
  document.getElementById('poi-priority').value = p.Priority ?? 2;
  document.getElementById('poi-status').value   = p.status   ?? 'active';
  openModal('modal-poi');
}

function filterPOI(query) {
  const q = (query || '').toLowerCase();
  const filtered = q
    ? poisData.filter(p =>
        (p.Name_vi||p.name||'').toLowerCase().includes(q) ||
        (p.Name_en||'').toLowerCase().includes(q) ||
        (p.Address_vi||p.address||'').toLowerCase().includes(q))
    : poisData;
  renderPOI(filtered);
}

// ════════════════════════════════════════════════════
// RENDER AUDIO
// ════════════════════════════════════════════════════
function renderAudio() {
  const list = document.getElementById('audio-list');
  if (!audiosData.length) {
    list.innerHTML = '<div class="empty-state"><div class="empty-icon">🎵</div><p>Chưa có audio nào. Upload file đầu tiên!</p></div>';
    return;
  }
  const grouped = {};
  audiosData.forEach(a => {
    const key = a.poiId || 'unknown';
    if (!grouped[key]) grouped[key] = { name: a.poiName || key, files: [] };
    grouped[key].files.push(a);
  });
  list.innerHTML = Object.entries(grouped).map(([poiId, g]) => `
    <div class="card" style="margin-bottom:14px">
      <div class="card-title">${poisData.find(p=>p.id===poiId)?.icon || '📍'} ${g.name}</div>
      ${g.files.map(f => `
        <div class="audio-row">
          <span class="audio-icon">🎵</span>
          <div class="audio-info">
            <div class="audio-name">${f.fileName}</div>
            <div class="audio-meta">${f.size || '—'}</div>
          </div>
          <span class="audio-lang-badge">${(f.lang||'').toUpperCase()}</span>
          <a href="${f.url}" target="_blank" class="btn btn-outline btn-sm" style="margin-left:8px">▶ Nghe</a>
          <button class="btn btn-danger btn-sm" onclick="deleteAudio('${f.id}','${f.fileName}','${g.name}')">🗑</button>
        </div>
      `).join('')}
      <button class="btn btn-outline btn-sm" style="margin-top:8px" onclick="openAddAudio()">+ Upload thêm</button>
    </div>
  `).join('');
}

function openAddAudio() { updateAudioPOIDropdown(); openModal('modal-audio'); }

function previewAudio(input) {
  if (!input.files[0]) return;
  const f = input.files[0];
  document.getElementById('audio-filename').textContent = f.name;
  document.getElementById('audio-filesize').textContent = (f.size/1024/1024).toFixed(2) + 'MB';
  document.getElementById('audio-player').src = URL.createObjectURL(f);
  document.getElementById('audio-preview').style.display = 'block';
}

// ════════════════════════════════════════════════════
// RENDER TRANSLATIONS
// ════════════════════════════════════════════════════
function renderTranslations() {
  const tbody = document.getElementById('trans-tbody');
  tbody.innerHTML = poisData.map(p => {

    const hasVi = p.Name_vi || p.Address_vi || p.Description_vi || p.Detail_vi || p.Content_vi;

    const hasEn = p.Name_en || p.translations?.en?.Name || (p.langs || []).includes('en');
    const hasJa = p.Name_ja || p.translations?.ja?.Name || (p.langs || []).includes('ja');
    
    return `
    <tr>
      <td><b>${p.icon||'📍'} ${p.Name_vi||p.name||'—'}</b></td>

      <!-- 🇻🇳 VI -->
      <td style="cursor:${hasVi?'pointer':'default'}"
          onclick="${hasVi?`viewTranslation('${p.id}','vi')`:''}"
          class="${hasVi?'hover-highlight':''}">
        ${hasVi
          ? '<span class="badge badge-active">✓ Có</span>'
          : '<span class="badge badge-pending">! Thiếu</span>'}
      </td>

      <!-- 🇬🇧 EN -->
      <td style="cursor:${hasEn?'pointer':'default'}"
          onclick="${hasEn?`viewTranslation('${p.id}','en')`:''}"
          class="${hasEn?'hover-highlight':''}">
        ${hasEn
          ? '<span class="badge badge-active">✓ Có</span>'
          : '<span class="badge badge-pending">! Thiếu</span>'}
      </td>

      <!-- 🇯🇵 JA -->
      <td style="cursor:${hasJa?'pointer':'default'}"
          onclick="${hasJa?`viewTranslation('${p.id}','ja')`:''}"
          class="${hasJa?'hover-highlight':''}">
        ${hasJa
          ? '<span class="badge badge-active">✓ Có</span>'
          : '<span class="badge badge-pending">! Thiếu</span>'}
      </td>
    </tr>
  `;
  }).join('');
}

// ════════════════════════════════════════════════════
// QR CODE
// ════════════════════════════════════════════════════
function renderQR() {
  const grid = document.getElementById('qr-grid');
  grid.innerHTML = poisData.map(p => `
    <div class="qr-card" id="qr-wrap-${p.id}">
      <div class="qr-box" id="qr-${p.id}"></div>
      <div class="qr-poi-name">${p.icon||'📍'} ${p.Name_vi||p.name||'—'}</div>
      <div class="qr-poi-addr">${p.Address_vi||p.address||''}</div>
      <div style="display:flex;gap:6px;justify-content:center">
        <button class="btn btn-outline btn-sm" onclick="downloadQR('${p.id}','${p.name}')">⬇ Tải PNG</button>
        <button class="btn btn-outline btn-sm" onclick="copyQRLink('${p.id}')">🔗 Copy link</button>
      </div>
    </div>
  `).join('');
  setTimeout(() => {
    poisData.forEach(p => {
      const container = document.getElementById('qr-' + p.id);
      if (container && container.children.length === 0) {
        new QRCode(container, {
          text: 'https://vinh-khanh-cms.web.app/poi.html?id=' + p.id,
          width: 110, height: 110,
          colorDark: '#000', colorLight: '#fff',
          correctLevel: QRCode.CorrectLevel.M
        });
      }
    });
  }, 100);
}

function downloadQR(id, name) {
  const container = document.getElementById('qr-' + id);
  const canvas = container?.querySelector('canvas');
  if (canvas) {
    const a = document.createElement('a');
    a.download = 'qr_' + id + '.png';
    a.href = canvas.toDataURL();
    a.click();
    showToast('Đã tải QR cho "' + name + '"', 'success');
  }
}

function copyQRLink(id) {
  navigator.clipboard.writeText('https://vinh-khanh-cms.web.app/poi.html?id=' + id);
  showToast('Đã copy deep link!', 'success');
}

// ════════════════════════════════════════════════════
// NAVIGATION
// ════════════════════════════════════════════════════
function switchPanel(name) {
  document.querySelectorAll('.panel').forEach(p => p.classList.remove('active'));
  document.querySelectorAll('.nav-item').forEach(n => n.classList.remove('active'));
  document.getElementById('panel-' + name).classList.add('active');
  const titles = {
    dashboard:'Dashboard', poi:'Điểm POI', audio:'Quản lý Audio',
    translations:'Bản dịch', qr:'QR Code', tours:'Tour tham quan',
    history:'Lịch sử sử dụng',access:'Xem truy cập', settings:'Cài đặt'
  };
  document.getElementById('page-title').textContent = titles[name] || name;
  event.currentTarget.classList.add('active');

  if (name === 'audio')        renderAudio();
  if (name === 'translations') renderTranslations();
  if (name === 'qr')           renderQR();
  if (name === 'tours')        renderTours();
  if (name === 'history')      renderHistory();
  if (name === 'access') loadAccessLogs(); 
}

function renderTours() {
  const classic = [
    {n:'Bún mắm Bà Năm',d:'~5 phút'},{n:'Hủ tiếu Nam Vang',d:'~3 phút'},
    {n:'Chè bưởi Cô Lan',d:'~4 phút'},{n:'Bánh tráng trộn Hoa',d:'~2 phút'},
    {n:'Cơm tấm Sài Gòn',d:'~3 phút'},
  ];
  const quick = [
    {n:'Bún mắm Bà Năm',d:'~5 phút'},{n:'Chè bưởi Cô Lan',d:'~4 phút'},
    {n:'Cà phê vợt cổ truyền',d:'~3 phút'},{n:'Lẩu mắm đặc sản',d:'~5 phút'},
  ];
  ['classic','quick'].forEach(key => {
    const stops = key === 'classic' ? classic : quick;
    document.getElementById('tour-' + key).innerHTML = stops.map((s, i) => `
      <div class="tour-stop">
        <div class="stop-number">${i+1}</div>
        <div class="stop-name">${s.n}</div>
        <div class="stop-dist">${s.d}</div>
        <span style="cursor:grab;color:var(--text3)">⋮⋮</span>
      </div>
    `).join('');
  });
}

function switchLangTab(el, group) {
  const tabsContainer = el.closest('.lang-tabs');
  tabsContainer.querySelectorAll('.lang-tab').forEach(t => t.classList.remove('active'));
  el.classList.add('active');

  ['vi','en','ja'].forEach(lang => {
    const wrap = document.getElementById(group + '-' + lang + '-wrap');
    if (wrap) wrap.style.display = (group + '-' + lang === group) ? 'block' : 'none';
  });

  const parts  = group.split('-');
  const prefix = parts.slice(0, -1).join('-');
  const active = parts[parts.length - 1];
  ['vi','en','ja'].forEach(lang => {
    const wrap = document.getElementById(prefix + '-' + lang + '-wrap');
    if (wrap) wrap.style.display = lang === active ? 'block' : 'none';
  });
}

function openModal(id)  { document.getElementById(id).classList.add('open'); }
function closeModal(id) { document.getElementById(id).classList.remove('open'); }

function showToast(msg, type = 'success') {
  const t = document.getElementById('toast');
  t.innerHTML = (type === 'success' ? '✅ ' : '❌ ') + msg;
  t.className = 'show ' + type;
  clearTimeout(window._toastTimer);
  window._toastTimer = setTimeout(() => { t.className = ''; }, 3500);
}

document.querySelectorAll('.modal-overlay').forEach(m => {
  m.addEventListener('click', e => { if (e.target === m) m.classList.remove('open'); });
});

// ════════════════════════════════════════════════════
// KHỞI ĐỘNG — Auth gate
// ════════════════════════════════════════════════════
onAuthStateChanged(auth, user => {
  if (user) {
    document.getElementById('login-screen').style.display = 'none';
    document.getElementById('app-shell').style.display    = 'flex';
    document.querySelector('.sidebar-footer .user-row .avatar').textContent =
      user.email.charAt(0).toUpperCase();
    document.querySelector('.sidebar-footer .user-row div div:first-child').textContent =
      user.displayName || 'Admin';
    document.querySelector('.sidebar-footer .user-row div div:last-child').textContent =
      user.email;
    listenPOI();
    listenAudio();
  } else {
    document.getElementById('login-screen').style.display = 'flex';
    document.getElementById('app-shell').style.display    = 'none';
  }
});

async function login() {
  const email = document.getElementById('login-email').value.trim();
  const pass  = document.getElementById('login-pass').value;
  const btn   = document.getElementById('login-btn');
  const err   = document.getElementById('login-error');
  if (!email || !pass) { err.textContent = 'Vui lòng nhập email và mật khẩu.'; return; }
  btn.textContent = 'Đang đăng nhập...';
  btn.disabled    = true;
  err.textContent = '';
  try {
    await signInWithEmailAndPassword(auth, email, pass);
  } catch (e) {
    const msgs = {
      'auth/invalid-credential':    'Email hoặc mật khẩu không đúng.',
      'auth/user-not-found':        'Tài khoản không tồn tại.',
      'auth/wrong-password':        'Mật khẩu không đúng.',
      'auth/too-many-requests':     'Quá nhiều lần thử. Thử lại sau.',
      'auth/invalid-email':         'Email không hợp lệ.',
    };
    err.textContent = msgs[e.code] || ('Lỗi: ' + e.message);
    btn.textContent = 'Đăng nhập';
    btn.disabled    = false;
  }
}

async function logout() {
  if (confirm('Đăng xuất khỏi CMS?')) {
    await signOut(auth);
  }
}

document.getElementById('login-pass')?.addEventListener('keydown', e => {
  if (e.key === 'Enter') login();
});


// ════════════════════════════════════════════════════
// TRANSLATION MODAL FUNCTIONS
// ════════════════════════════════════════════════════
let currentTranslateLang = null; // 'en', 'ja'
const langLabels = {
  'vi': '🇻🇳 Tiếng Việt',
  'en': '🇬🇧 English',
  'ja': '🇯🇵 日本語'
};

function selectTranslateLang(lang, label) {
  currentTranslateLang = lang;
  const poiId = document.getElementById('trans-poi-select').value;
  
  if (!poiId) {
    showToast('Vui lòng chọn POI trước!', 'error');
    return;
  }
  
  ['en','ja'].forEach(l => {
    const btn = document.getElementById(`lang-${l}-btn`);
    if (l === lang) {
      btn.classList.add('active');
      btn.style.background = 'var(--accent)';
      btn.style.color = '#000';
    } else {
      btn.classList.remove('active');
      btn.style.background = 'transparent';
      btn.style.color = 'var(--text2)';
    }
  });
  
  document.getElementById('selected-lang-display').textContent = `✓ Đã chọn: ${label}`;
  
  loadPOIForTranslate();
}

function loadPOIForTranslate() {
  const poiId = document.getElementById('trans-poi-select').value;
  const poi = poisData.find(p => p.id === poiId);
  
  if (!poi || !currentTranslateLang) {
    document.getElementById('translate-form').style.display = 'none';
    return;
  }
  
  document.getElementById('trans-name-vi').value = poi.Name_vi || '';
  document.getElementById('trans-addr-vi').value = poi.Address_vi || '';
  document.getElementById('trans-desc-vi').value = poi.Description_vi || '';
  document.getElementById('trans-detail-vi').value = poi.Detail_vi || '';
  document.getElementById('trans-content-vi').value = poi.Content_vi || '';
  
  const langLabel = langLabels[currentTranslateLang];
  document.querySelectorAll('[id^="trans-"][id$="-label"]').forEach(el => {
    el.textContent = el.textContent.split(' —')[0] + ' — ' + langLabel;
  });
  
  const existingTranslation = getExistingTranslation(poiId, currentTranslateLang);
  if (existingTranslation) {
    document.getElementById('trans-name').value = existingTranslation.Name || '';
    document.getElementById('trans-addr').value = existingTranslation.Address || '';
    document.getElementById('trans-desc').value = existingTranslation.Description || '';
    document.getElementById('trans-detail').value = existingTranslation.Detail || '';
    document.getElementById('trans-content').value = existingTranslation.Content || '';
  } else {
    document.getElementById('trans-name').value = '';
    document.getElementById('trans-addr').value = '';
    document.getElementById('trans-desc').value = '';
    document.getElementById('trans-detail').value = '';
    document.getElementById('trans-content').value = '';
  }
  
  document.getElementById('translate-form').style.display = 'block';
}

function getExistingTranslation(poiId, lang) {
  const poi = poisData.find(p => p.id === poiId);
  if (!poi) return null;
  
  if (poi.translations && poi.translations[lang]) {
    return poi.translations[lang];
  }
  
  if (lang === 'en') {
    return {
      Name: poi.Name_en || '',
      Address: poi.Address_en || '',
      Description: poi.Description_en || '',
      Detail: poi.Detail_en || '',
      Content: poi.Content_en || ''
    };
  }
  
  if (lang === 'ja') {
    return {
      Name: poi.Name_ja || '',
      Address: poi.Address_ja || '',
      Description: poi.Description_ja || '',
      Detail: poi.Detail_ja || '',
      Content: poi.Content_ja || ''
    };
  }
  
  return null;
}

async function autoTranslateField(sourceId, targetId) {
  const sourceText = document.getElementById(sourceId).value;
  if (!sourceText || !currentTranslateLang) {
    showToast('Vui lòng nhập text và chọn ngôn ngữ!', 'error');
    return;
  }
  
  const btn = event.target;
  btn.textContent = '⏳ Dang dich...';
  btn.disabled = true;
  
  try {
    const translated = await autoTranslate(sourceText, currentTranslateLang);
    document.getElementById(targetId).value = translated;
    showToast('Dịch thành công!', 'success');
  } catch (e) {
    showToast('Lỗi dịch: ' + e.message, 'error');
  }
  
  btn.textContent = '🔄 Dịch tự động';
  btn.disabled = false;
}

async function saveTranslation() {
  const poiId = document.getElementById('trans-poi-select').value;
  if (!poiId || !currentTranslateLang) {
    showToast('Vui lòng chọn POI và ngôn ngữ!', 'error');
    return;
  }
  
  const translationData = {
    Name: document.getElementById('trans-name').value,
    Address: document.getElementById('trans-addr').value,
    Description: document.getElementById('trans-desc').value,
    Detail: document.getElementById('trans-detail').value,
    Content: document.getElementById('trans-content').value
  };
  
  if (!translationData.Name) {
    showToast('Tên ngôn ngữ không được để trống!', 'error');
    return;
  }
  
  try {
    const docRef = doc(db, POIS, poiId);
    const updateData = {};
    updateData[`translations.${currentTranslateLang}`] = translationData;
    updateData[`langs`] = arrayUnion(currentTranslateLang);
    
    await updateDoc(docRef, updateData);
    showToast(`Đã lưu bản dịch ${langLabels[currentTranslateLang]}!`, 'success');
    closeModal('modal-translate');
    renderTranslations();
  } catch (e) {
    showToast('Lỗi lưu: ' + e.message, 'error');
  }
}

let accessUnsubscribe = null;

function loadAccessLogs() {
  const tbody = document.getElementById('access-tbody');
  if (!tbody) return;

  // Hủy listener cũ nếu có
  if (accessUnsubscribe) accessUnsubscribe();

  // Lắng nghe real-time từ collection app_access_logs
  accessUnsubscribe = onSnapshot(collection(db, ACCESS), (snap) => {
    const logs = snap.docs.map(doc => ({
      id: doc.id,
      ...doc.data()
    }));
    
    // Sắp xếp theo thời gian mới nhất
    logs.sort((a, b) => {
      const ta = a.LastActive?.toDate?.() || new Date(a.LastActive || 0);
      const tb = b.LastActive?.toDate?.() || new Date(b.LastActive || 0);
      return tb - ta;
    });

    renderAccessLogs(logs);
  }, (err) => {
    console.error("Lắng nghe access logs lỗi:", err);
    tbody.innerHTML = `<tr><td colspan="5" style="color:var(--danger)">Lỗi: ${err.message}</td></tr>`;
  });
}

function renderAccessLogs(data) {
  const tbody = document.getElementById('access-tbody');
  tbody.innerHTML = '';

  if (data.length === 0) {
    tbody.innerHTML = '<tr><td colspan="5" style="text-align:center;padding:20px;color:var(--text3)">Chưa có dữ liệu truy cập</td></tr>';
    return;
  }

  data.forEach(item => {
    // Xử lý timestamp từ Firestore hoặc string ISO
    let lastDate;
    if (item.LastActive?.toDate) {
      lastDate = item.LastActive.toDate();
    } else {
      lastDate = new Date(item.LastActive || Date.now());
    }

    const status = item.Status || 'online';
    const isOnline = status === 'online';

    const tr = document.createElement('tr');
    tr.innerHTML = `
      <td style="font-family:var(--mono);font-size:12px">${item.id}</td>
      <td style="font-weight:500">${item.Device || '—'}</td>
      <td>${item.Platform || '—'}</td>
      <td style="color:var(--text2)">${lastDate.toLocaleString('vi-VN')}</td>
      <td>
        <span class="badge ${isOnline ? 'badge-active' : 'badge-inactive'}" style="display:inline-flex;align-items:center;gap:4px">
          ${isOnline ? '● Online' : '○ Offline'}
        </span>
      </td>
    `;
    tbody.appendChild(tr);
  });
}

let viewingTranslationPOI = null;
let viewingTranslationLang = null;

function viewTranslation(poiId, lang) {
  const poi = poisData.find(p => p.id === poiId);
  if (!poi) return;
  
  let translation = poi.translations?.[lang];
  
  if (lang === 'vi') {
    translation = {
      Name: poi.Name_vi || '',
      Address: poi.Address_vi || '',
      Description: poi.Description_vi || '',
      Detail: poi.Detail_vi || '',
      Content: poi.Content_vi || ''
    };
  }

  if (!translation) {
    if (lang === 'en') {
      translation = {
        Name: poi.Name_en || '',
        Address: poi.Address_en || '',
        Description: poi.Description_en || '',
        Detail: poi.Detail_en || '',
        Content: poi.Content_en || ''
      };
    } else if (lang === 'ja') {
      translation = {
        Name: poi.Name_ja || '',
        Address: poi.Address_ja || '',
        Description: poi.Description_ja || '',
        Detail: poi.Detail_ja || '',
        Content: poi.Content_ja || ''
      };
    }
  }
  
  if (!translation || !translation.Name) {
    showToast('Không tìm thấy bản dịch!', 'error');
    return;
  }
  
  viewingTranslationPOI = poiId;
  viewingTranslationLang = lang;
  
  const langLabel = langLabels[lang];
  const langEmoji = { 'vi': '🇻🇳', 'en': '🇬🇧', 'ja': '🇯🇵' }[lang] || '🌐';
  
  document.getElementById('view-trans-title').textContent = 
    `${langEmoji} Bản dịch ${langLabel} — ${poi.Name_vi}`;
  
  const html = `
    <div style="background:var(--surface2);padding:16px;border-radius:10px">
      <div style="margin-bottom:16px">
        <div style="font-size:12px;color:var(--text3);margin-bottom:4px;font-weight:600;text-transform:uppercase">Tên</div>
        <div style="font-size:15px;color:var(--accent);font-weight:600">${translation.Name || '—'}</div>
      </div>
      <div style="margin-bottom:16px">
        <div style="font-size:12px;color:var(--text3);margin-bottom:4px;font-weight:600;text-transform:uppercase">Địa chỉ</div>
        <div style="font-size:14px;color:var(--text)">${translation.Address || '—'}</div>
      </div>
      <div style="margin-bottom:16px">
        <div style="font-size:12px;color:var(--text3);margin-bottom:4px;font-weight:600;text-transform:uppercase">Mô tả ngắn</div>
        <div style="font-size:14px;color:var(--text)">${translation.Description || '—'}</div>
      </div>
      <div style="margin-bottom:16px">
        <div style="font-size:12px;color:var(--text3);margin-bottom:4px;font-weight:600;text-transform:uppercase">Mô tả dài</div>
        <div style="font-size:14px;color:var(--text);line-height:1.5;white-space:pre-wrap">${translation.Detail || '—'}</div>
      </div>
      <div>
        <div style="font-size:12px;color:var(--text3);margin-bottom:4px;font-weight:600;text-transform:uppercase">Script TTS</div>
        <div style="font-size:14px;color:var(--text);line-height:1.5;white-space:pre-wrap">${translation.Content || '—'}</div>
      </div>
    </div>
  `;
  
  document.getElementById('view-trans-content').innerHTML = html;
  openModal('modal-view-translation');
}

window.switchPanel    = switchPanel;
window.openAddPOI     = openAddPOI;
window.editPOI        = editPOI;
window.savePOI        = savePOI;
window.deletePOI      = deletePOI;
window.filterPOI      = filterPOI;
window.openAddAudio   = openAddAudio;
window.previewAudio   = previewAudio;
window.uploadAudio    = uploadAudio;
window.deleteAudio    = deleteAudio;
window.renderQR       = renderQR;
window.downloadQR     = downloadQR;
window.copyQRLink     = copyQRLink;
window.switchLangTab  = switchLangTab;
window.openModal      = openModal;
window.closeModal     = closeModal;
window.showToast      = showToast;
window.filterHistoryByDate = filterHistoryByDate;
window.login          = login;
window.logout         = logout;
window.viewTranslation        = viewTranslation;

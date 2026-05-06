import { initializeApp } from 'https://www.gstatic.com/firebasejs/10.7.1/firebase-app.js';
import { getFirestore, doc, getDoc, updateDoc, addDoc, collection, serverTimestamp }
  from 'https://www.gstatic.com/firebasejs/10.7.1/firebase-firestore.js';

// FIREBASE CONFIG
const firebaseConfig = {
  apiKey: "AIzaSyDO7cvTxvx26Qu6Bo6Ts5ZT0cl8yBhcj5s",
  authDomain: "vinh-khanh-cms.firebaseapp.com",
  projectId: "vinh-khanh-cms",
  storageBucket: "vinh-khanh-cms.firebasestorage.app",
  messagingSenderId: "27322782868",
  appId: "1:27322782868:web:ed13c37aea04023e7f9081"
};

const app = initializeApp(firebaseConfig);
const db = getFirestore(app);

// STATE
let poi = null;
let selectedLang = 'vi';
let isSpeaking = false;
let utterance = null;

const LANGS = [
  { code: 'vi', flag: '🇻🇳', label: 'Tiếng Việt' },
  { code: 'en', flag: '🇬🇧', label: 'English' },
  { code: 'ja', flag: '🇯🇵', label: '日本語' },
];

// LẤY ID TỪ URL
function getPoiId() {
  const params = new URLSearchParams(window.location.search);
  return params.get('id') || params.get('poi') || '';
}

// LẤY HOẶC TẠO ID THIẾT BỊ (WEB)
function getWebDeviceId() {
  let id = localStorage.getItem('web_device_id');
  if (!id) {
    id = 'WEB-' + Math.random().toString(36).substr(2, 9).toUpperCase();
    localStorage.setItem('web_device_id', id);
  }
  return id;
}

// GHI LỊCH SỬ QUÉT QR
async function logScanHistory(poiData) {
  try {
    const deviceId = getWebDeviceId();
    const userAgent = navigator.userAgent;
    let deviceModel = "Web Browser";

    if (userAgent.match(/Android/i)) deviceModel = "Android Device";
    else if (userAgent.match(/iPhone|iPad|iPod/i)) deviceModel = "iOS Device";

    await addDoc(collection(db, 'history'), {
      poiName: poiData.Name_vi || poiData.name || 'Unknown',
      lang: selectedLang,
      source: 'QR',
      device: deviceModel,
      deviceId: deviceId,
      timestamp: serverTimestamp()
    });
    console.log("Logged scan history for:", poiData.Name_vi);
  } catch (e) {
    console.error("Error logging history:", e);
  }
}

// LOAD DỮ LIỆU
async function loadPoi() {
  const id = getPoiId();
  if (!id) {
    showError('Không tìm thấy mã điểm tham quan.');
    return;
  }

  try {
    const snap = await getDoc(doc(db, 'pois', id));
    if (!snap.exists()) {
      showError('Điểm tham quan không tồn tại.');
      return;
    }
    poi = { id: snap.id, ...snap.data() };
    if (poi.status === 'inactive') {
      showError('Điểm tham quan này tạm ngưng hoạt động.');
      return;
    }
    renderPoi();

    // TỰ ĐỘNG GHI LOG KHI QUÉT THÀNH CÔNG
    logScanHistory(poi);
  } catch (e) {
    showError('Lỗi kết nối: ' + e.message);
  }
}



// HIỂN THỊ
function renderPoi() {
  document.getElementById('screen-loading').style.display = 'none';
  const screen = document.getElementById('screen-poi');
  screen.style.display = 'flex';

  // Ảnh
  const imgWrap = document.getElementById('poi-image-wrap');
  if (poi.ImageUrl) {
    imgWrap.innerHTML = `<img class="poi-image" src="/images/${poi.ImageUrl}" onerror="this.style.display='none';document.getElementById('img-fallback').style.display='flex'">
    <div class="poi-image-placeholder" id="img-fallback" style="display:none">🍽️</div>`;
  } else {
    imgWrap.innerHTML = `<div class="poi-image-placeholder">🍽️</div>`;
  }

  // Thông tin cơ bản
  document.getElementById('poi-stars').textContent = '⭐'.repeat(Math.floor(poi.Rating || 5));
  document.getElementById('poi-rating-num').textContent = (poi.Rating || 5.0).toFixed(1);

  // Ngôn ngữ
  const grid = document.getElementById('lang-grid');
  grid.innerHTML = LANGS.map(l => `
    <button class="lang-btn ${l.code === selectedLang ? 'selected' : ''}" onclick="selectLang('${l.code}')">
      <span class="lang-flag">${l.flag}</span>
      <span class="lang-name">${l.label}</span>
    </button>
  `).join('');

  updateContent();
}

function updateContent() {
  const name = poi[`Name_${selectedLang}`] || poi.Name_vi || '';
  const addr = poi[`Address_${selectedLang}`] || poi.Address_vi || '';
  const desc = poi[`Description_${selectedLang}`] || poi.Description_vi || '';
  const content = poi[`Content_${selectedLang}`] || poi.Content_vi || '';

  document.getElementById('poi-name').textContent = name;
  document.getElementById('poi-address').textContent = addr;
  document.getElementById('poi-description').textContent = desc;
  document.getElementById('script-box').textContent = content;

  if (poi.Latitude && poi.Longitude) {
    document.getElementById('map-link').href = `https://www.google.com/maps/search/?api=1&query=${poi.Latitude},${poi.Longitude}`;
  }
}

window.selectLang = function (lang) {
  if (isSpeaking) stopSpeak();
  selectedLang = lang;

  document.querySelectorAll('.lang-btn').forEach(btn => {
    const code = btn.getAttribute('onclick').match(/'(\w+)'/)[1];
    btn.classList.toggle('selected', code === lang);
  });

  updateContent();
};

window.toggleSpeak = function () {
  if (isSpeaking) {
    stopSpeak();
  } else {
    startSpeak();
  }
};

function startSpeak() {
  const text = poi[`Content_${selectedLang}`] || poi.Content_vi || '';
  if (!text) return;

  utterance = new SpeechSynthesisUtterance(text);
  const langMap = { vi: 'vi-VN', en: 'en-US', ja: 'ja-JP' };
  utterance.lang = langMap[selectedLang] || 'vi-VN';

  utterance.onstart = () => {
    isSpeaking = true;
    document.getElementById('btn-speak').classList.add('playing');
    document.getElementById('speak-label').textContent = 'Dừng thuyết minh';
  };
  utterance.onend = () => {
    isSpeaking = false;
    document.getElementById('btn-speak').classList.remove('playing');
    document.getElementById('speak-label').textContent = 'Nghe thuyết minh';
  };

  speechSynthesis.speak(utterance);
}

function stopSpeak() {
  speechSynthesis.cancel();
  isSpeaking = false;
  document.getElementById('btn-speak').classList.remove('playing');
  document.getElementById('speak-label').textContent = 'Nghe thuyết minh';
}

window.toggleScript = function () {
  const box = document.getElementById('script-box');
  box.classList.toggle('show');
};

function showError(msg) {
  document.getElementById('screen-loading').style.display = 'none';
  document.getElementById('screen-error').style.display = 'block';
  document.getElementById('error-msg').textContent = msg;
}

loadPoi();

import { initializeApp } from 'https://www.gstatic.com/firebasejs/10.7.1/firebase-app.js';
import { getFirestore, collection, getDocs, query, where, orderBy }
  from 'https://www.gstatic.com/firebasejs/10.7.1/firebase-firestore.js';

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

async function loadPois() {
  const listEl = document.getElementById('poi-list');

  try {
    // Truy vấn tất cả và sắp xếp theo Priority
    const q = query(collection(db, 'pois'), orderBy('Priority', 'asc'));
    const snap = await getDocs(q);
    
    // Lọc các quán 'active' bằng Javascript để không cần tạo Index trên Firebase
    const activePois = snap.docs
      .map(doc => ({ id: doc.id, ...doc.data() }))
      .filter(p => p.status === 'active');
    
    if (activePois.length === 0) {
      listEl.innerHTML = '<div style="text-align:center; color:var(--text3); padding:40px;">Hiện chưa có quán nào hoạt động.</div>';
      return;
    }

    let html = '';
    activePois.forEach(p => {
      const id = p.id;
      
      const imgHtml = p.ImageUrl
        ? `<img class="poi-img" src="/images/${p.ImageUrl}" onerror="this.parentElement.innerHTML='<div class=poi-img-placeholder>🍜</div>'">`
        : `<div class="poi-img-placeholder">🍜</div>`;

      html += `
        <a href="poi.html?id=${id}" class="poi-item">
          ${imgHtml}
          <div class="poi-info">
            <div class="poi-name">${p.Name_vi || 'Quán ăn'}</div>
            <div class="poi-addr">${p.Address_vi || ''}</div>
          </div>
          <div class="poi-arrow">❯</div>
        </a>
      `;
    });
    
    listEl.innerHTML = html;
  } catch (e) {
    console.error(e);
    listEl.innerHTML = `<div style="text-align:center; color:var(--danger); padding:40px;">Lỗi kết nối: ${e.message}</div>`;
  }
}

loadPois();

/* ════════════════════════════════════════════════════════════════════
   Mesa-Mohloane Infrastructure Reporting System — Client JS
   ════════════════════════════════════════════════════════════════════ */

// API base injected by Razor shell via window.__API_BASE__
const API_BASE =
    window.__API_BASE__
    || localStorage.getItem('mesa_api_base')
    || window.location.origin;

/* ── Auth helpers ──────────────────────────────────────────────────── */
const Auth = {
    getToken: () => localStorage.getItem('mesa_token') || document.cookie.split(';').map(c => c.trim()).find(c => c.startsWith('mesa_token='))?.split('=')[1] || '',
    setToken: (t) => localStorage.setItem('mesa_token', t),
    getUser: () => JSON.parse(localStorage.getItem('mesa_user') || 'null'),
    setUser: (u) => localStorage.setItem('mesa_user', JSON.stringify(u)),
    getRole: () => localStorage.getItem('mesa_role'),
    setRole: (r) => localStorage.setItem('mesa_role', r),

    clear: () => {
        localStorage.removeItem('mesa_token');
        localStorage.removeItem('mesa_user');
        localStorage.removeItem('mesa_role');
    },

    isLoggedIn: () => (document.body?.dataset?.authenticated === 'true'),

    redirectIfGuest: () => { if (!Auth.isLoggedIn()) window.location.href = '/Auth/Login'; },

    redirectByRole: () => {
        const role = Auth.getRole();
        if (role === 'Administrator') window.location.href = '/Admin/Dashboard';
        else if (role === 'Contractor') window.location.href = '/Contractor/Dashboard';
        else if (role === 'Inspector') window.location.href = '/Inspector/Dashboard';
        else window.location.href = '/Citizen/Dashboard';
    }
};

/* Hydrate client user state from DOM data attributes set by the shell */
function hydrateClientUserFromDom() {
    try {
        const body = document.body;
        if (!body) return;

        let user = Auth.getUser();
        const role = Auth.getRole();
        const ds = body.dataset || {};

        const fullName = ds.userName;
        const email = ds.userEmail;
        const userRole = ds.userRole;

        if (!user && (fullName || email || userRole)) {
            user = {
                fullName: fullName || '',
                email: email || '',
                role: userRole || '',
                firstName: (fullName || '').split(' ')[0] || '',
                lastName: (fullName || '').split(' ').slice(1).join(' ') || ''
            };
            Auth.setUser(user);
        }

        if (!role && userRole) Auth.setRole(userRole);
    } catch { /* ignore */ }
}

/* ── API Client ─────────────────────────────────────────────────────── */
const Api = {
    async request(method, path, body = null, extraHeaders = null) {
        const headers = { 'Content-Type': 'application/json', ...(extraHeaders || {}) };

        // Attach antiforgery token for mutating MVC proxy calls
        if (method !== 'GET' && window.__ANTI_FORGERY__) {
            headers['RequestVerificationToken'] = window.__ANTI_FORGERY__;
        }

        // Attach JWT only for direct /api/ calls
        const token = Auth.getToken();
        if (token && String(path || '').startsWith('/api/')) {
            headers['Authorization'] = `Bearer ${token}`;
        }

        const opts = { method, headers };
        if (body) opts.body = JSON.stringify(body);

        // MVC proxy paths stay on same origin; API paths get the base URL
        const isMvcProxy = ['/Admin/', '/Auth/', '/Contractor/', '/Citizen/', '/Inspector/']
            .some(p => String(path || '').startsWith(p));

        const url = isMvcProxy ? path : `${API_BASE}${path}`;

        let res;
        try {
            res = await fetch(url, opts);
        } catch (e) {
            return { ok: false, status: 0, data: { error: 'Network error', detail: e?.message } };
        }

        const text = await res.text();
        let data;
        try { data = text ? JSON.parse(text) : null; } catch { data = text; }

        return { ok: res.ok, status: res.status, data };
    },

    get: (path) => Api.request('GET', path),
    post: (path, body) => Api.request('POST', path, body),
    put: (path, body) => Api.request('PUT', path, body),
    patch: (path, body) => Api.request('PATCH', path, body),
    delete: (path) => Api.request('DELETE', path),
};

/* ── Toast Notifications ────────────────────────────────────────────── */
function showToast(message, type = 'info', duration = 3500) {
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        document.body.appendChild(container);
    }

    const icons = {
        success: 'fa-check-circle',
        error: 'fa-times-circle',
        warning: 'fa-exclamation-triangle',
        info: 'fa-info-circle'
    };

    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    toast.innerHTML = `<i class="fa-solid ${icons[type] || icons.info}"></i><span>${message}</span>`;
    container.appendChild(toast);

    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transform = 'translateX(100%)';
        toast.style.transition = 'all .3s ease';
        setTimeout(() => toast.remove(), 300);
    }, duration);
}

/* ── Dropdown Menus ─────────────────────────────────────────────────── */
document.addEventListener('click', e => {
    const btn = e.target.closest('[data-dropdown-toggle]');
    if (btn) {
        e.stopPropagation();
        const menu = document.getElementById(btn.dataset.dropdownToggle);
        if (!menu) return;
        const isOpen = menu.classList.contains('show');
        document.querySelectorAll('.dropdown-menu.show').forEach(m => m.classList.remove('show'));
        if (!isOpen) menu.classList.add('show');
        return;
    }
    document.querySelectorAll('.dropdown-menu.show').forEach(m => m.classList.remove('show'));
});

/* ── Modal Helpers ──────────────────────────────────────────────────── */
function openModal(id) { document.getElementById(id)?.classList.add('show'); }
function closeModal(id) { document.getElementById(id)?.classList.remove('show'); }

document.addEventListener('click', e => {
    if (e.target.classList.contains('modal-overlay')) e.target.classList.remove('show');
    if (e.target.dataset.closeModal) closeModal(e.target.dataset.closeModal);
});

document.addEventListener('keydown', e => {
    if (e.key === 'Escape')
        document.querySelectorAll('.modal-overlay.show').forEach(m => m.classList.remove('show'));
});

/* ── Sidebar Toggle (mobile) ────────────────────────────────────────── */
document.addEventListener('click', e => {
    if (e.target.closest('#sidebar-toggle')) {
        document.getElementById('sidebar')?.classList.toggle('open');
        return;
    }
    if (!e.target.closest('#sidebar') && !e.target.closest('#sidebar-toggle')) {
        document.getElementById('sidebar')?.classList.remove('open');
    }
});

/* ── Paginator ──────────────────────────────────────────────────────── */
class Paginator {
    constructor({ container, pageSize = 10, onPageChange }) {
        this.container = container;
        this.pageSize = pageSize;
        this.onPageChange = onPageChange;
        this.current = 1;
        this.total = 0;
    }

    setTotal(total) { this.total = total; this.render(); }
    get totalPages() { return Math.max(1, Math.ceil(this.total / this.pageSize)); }
    get start() { return (this.current - 1) * this.pageSize + 1; }
    get end() { return Math.min(this.current * this.pageSize, this.total); }

    render() {
        const c = document.getElementById(this.container);
        if (!c) return;
        const pages = this.totalPages;

        const range = (s, e) => Array.from({ length: e - s + 1 }, (_, i) => s + i);
        let pageNums = [];
        if (pages <= 7) pageNums = range(1, pages);
        else if (this.current <= 4) pageNums = [...range(1, 5), '...', pages];
        else if (this.current >= pages - 3) pageNums = [1, '...', ...range(pages - 4, pages)];
        else pageNums = [1, '...', ...range(this.current - 1, this.current + 1), '...', pages];

        let btns = '';
        pageNums.forEach(p => {
            if (p === '...') {
                btns += `<span class="page-btn" style="border:none;background:none;cursor:default">…</span>`;
            } else {
                btns += `<button class="page-btn ${p === this.current ? 'active' : ''}" onclick="paginator_${this.container}.goTo(${p})">${p}</button>`;
            }
        });

        c.innerHTML = `
            <div class="pagination-info">
                Showing ${this.total === 0 ? 0 : this.start}–${this.total === 0 ? 0 : this.end} of ${this.total} records
            </div>
            <div class="pagination-nav">
                <button class="page-btn" onclick="paginator_${this.container}.goTo(${this.current - 1})"
                    ${this.current === 1 ? 'disabled' : ''}>
                    <i class="fa-solid fa-chevron-left" style="font-size:.7rem"></i>
                </button>
                ${btns}
                <button class="page-btn" onclick="paginator_${this.container}.goTo(${this.current + 1})"
                    ${this.current === pages ? 'disabled' : ''}>
                    <i class="fa-solid fa-chevron-right" style="font-size:.7rem"></i>
                </button>
            </div>`;

        window[`paginator_${this.container}`] = this;
    }

    goTo(page) {
        if (page < 1 || page > this.totalPages) return;
        this.current = page;
        this.render();
        this.onPageChange(page);
    }
}

/* ── Confirm Dialog ─────────────────────────────────────────────────── */
function confirmAction(message, onConfirm, dangerLabel = 'Confirm') {
    if (!document.getElementById('confirm-modal')) {
        document.body.insertAdjacentHTML('beforeend', `
            <div class="modal-overlay" id="confirm-modal">
                <div class="modal" style="max-width:380px">
                    <div class="modal-header">
                        <span class="modal-title" style="display:flex;align-items:center;gap:.5rem">
                            <i class="fa-solid fa-triangle-exclamation" style="color:var(--warning)"></i>
                            Confirm Action
                        </span>
                        <button class="modal-close" data-close-modal="confirm-modal">
                            <i class="fa-solid fa-xmark"></i>
                        </button>
                    </div>
                    <div class="modal-body">
                        <p id="confirm-message" style="color:var(--text-secondary);font-size:.9rem"></p>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary btn-sm" data-close-modal="confirm-modal">Cancel</button>
                        <button class="btn btn-danger btn-sm" id="confirm-ok">Confirm</button>
                    </div>
                </div>
            </div>`);
    }

    document.getElementById('confirm-message').textContent = message;
    document.getElementById('confirm-ok').textContent = dangerLabel;
    openModal('confirm-modal');

    const btn = document.getElementById('confirm-ok');
    const clone = btn.cloneNode(true);
    btn.replaceWith(clone);
    clone.addEventListener('click', () => { closeModal('confirm-modal'); onConfirm(); });
}

/* ── Format Helpers ─────────────────────────────────────────────────── */
function formatDate(iso) {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
}

function formatDateTime(iso) {
    if (!iso) return '—';
    return new Date(iso).toLocaleString('en-GB', {
        day: '2-digit', month: 'short', year: 'numeric',
        hour: '2-digit', minute: '2-digit'
    });
}

function formatCurrency(amount) {
    if (amount === null || amount === undefined) return '—';
    return `M ${Number(amount).toLocaleString('en-LS', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

function initials(name) {
    return (name || '').split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2) || '?';
}

function avatarColor(name) {
    const colors = ['#1A56DB', '#057A55', '#0694A2', '#1E429F', '#046040', '#0E9F6E', '#1C64F2'];
    let hash = 0;
    for (let i = 0; i < (name || '').length; i++) hash = name.charCodeAt(i) + ((hash << 5) - hash);
    return colors[Math.abs(hash) % colors.length];
}

/* ── Status / Role Badges ───────────────────────────────────────────── */
function incidentStatusBadge(status) {
    const map = {
        Pending: '<span class="badge badge-warning badge-dot">Pending</span>',
        Reported: '<span class="badge badge-info badge-dot">Reported</span>',
        Verified: '<span class="badge badge-primary badge-dot">Verified</span>',
        Published: '<span class="badge badge-blue badge-dot">Published</span>',
        Assigned: '<span class="badge badge-teal badge-dot">Assigned</span>',
        InProgress: '<span class="badge badge-info badge-dot">In Progress</span>',
        Completed: '<span class="badge badge-success badge-dot">Completed</span>',
        Closed: '<span class="badge badge-gray badge-dot">Closed</span>',
        Rejected: '<span class="badge badge-danger badge-dot">Rejected</span>',
    };
    return map[status] || `<span class="badge badge-gray">${status}</span>`;
}

function tenderStatusBadge(status) {
    const map = {
        Submitted: '<span class="badge badge-info badge-dot">Submitted</span>',
        UnderReview: '<span class="badge badge-warning badge-dot">Under Review</span>',
        Approved: '<span class="badge badge-success badge-dot">Approved</span>',
        Rejected: '<span class="badge badge-danger badge-dot">Rejected</span>',
        Withdrawn: '<span class="badge badge-gray badge-dot">Withdrawn</span>',
    };
    return map[status] || `<span class="badge badge-gray">${status}</span>`;
}

function invoiceStatusBadge(status) {
    const map = {
        Submitted: '<span class="badge badge-info badge-dot">Submitted</span>',
        Validated: '<span class="badge badge-primary badge-dot">Validated</span>',
        Approved: '<span class="badge badge-success badge-dot">Approved</span>',
        Flagged: '<span class="badge badge-warning badge-dot">Flagged</span>',
        Disbursed: '<span class="badge badge-green badge-dot">Disbursed</span>',
        Rejected: '<span class="badge badge-danger badge-dot">Rejected</span>',
    };
    return map[status] || `<span class="badge badge-gray">${status}</span>`;
}

function paymentStatusBadge(status) {
    const map = {
        Initiated: '<span class="badge badge-info badge-dot">Initiated</span>',
        Approved: '<span class="badge badge-primary badge-dot">Approved</span>',
        Disbursed: '<span class="badge badge-green badge-dot">Disbursed</span>',
        Failed: '<span class="badge badge-danger badge-dot">Failed</span>',
    };
    return map[status] || `<span class="badge badge-gray">${status}</span>`;
}

function roleBadge(role) {
    const map = {
        Administrator: '<span class="badge badge-blue">Administrator</span>',
        Contractor: '<span class="badge badge-teal">Contractor</span>',
        Citizen: '<span class="badge badge-green">Citizen</span>',
        Inspector: '<span class="badge badge-primary">Inspector</span>',
    };
    return map[role] || `<span class="badge badge-gray">${role}</span>`;
}

/* Render star rating (1–5) */
function starRating(stars) {
    let html = '';
    for (let i = 1; i <= 5; i++) {
        html += `<i class="fa-solid fa-star" style="color:${i <= stars ? '#C27803' : '#D1D5DB'};font-size:.8rem"></i>`;
    }
    return html;
}

/* Score bar (0–1 value → colored bar) */
function scoreBar(value) {
    const pct = Math.round(value * 100);
    const cls = pct >= 70 ? 'high' : pct >= 40 ? 'medium' : 'low';
    return `<div class="score-bar"><div class="score-fill ${cls}" style="width:${pct}%"></div></div>
            <span style="font-size:.75rem;color:var(--text-muted)">${pct}%</span>`;
}

/* ── Sidebar active link ─────────────────────────────────────────────── */
document.addEventListener('DOMContentLoaded', () => {
    hydrateClientUserFromDom();

    const path = window.location.pathname;
    document.querySelectorAll('.sidebar-link').forEach(link => {
        const href = link.getAttribute('href');
        if (href && path.startsWith(href) && href !== '/') {
            link.classList.add('active');
        }
    });

    // Populate user info in sidebar
    const user = Auth.getUser();
    if (user) {
        const nameEl = document.getElementById('sidebar-user-name');
        const roleEl = document.getElementById('sidebar-user-role');
        const avatarEl = document.getElementById('sidebar-avatar');
        const displayName = user.fullName || `${user.firstName} ${user.lastName}`;

        if (nameEl) nameEl.textContent = displayName;
        if (roleEl) roleEl.textContent = user.role;
        if (avatarEl) {
            avatarEl.textContent = initials(displayName);
            avatarEl.style.background = avatarColor(displayName);
        }
    }

    loadNotifCount();
});

/* ── Notification count ─────────────────────────────────────────────── */
async function loadNotifCount() {
    if (!Auth.isLoggedIn()) return;
    try {
        const res = await Api.get('/api/notifications/unread-count');
        if (res?.ok) {
            const count = res.data?.unreadCount ?? 0;
            const badge = document.getElementById('notif-count');
            const dot = document.getElementById('notif-dot');
            if (badge) badge.textContent = count > 0 ? count : '';
            if (dot) dot.style.display = count > 0 ? 'block' : 'none';
        }
    } catch { /* ignore */ }
}

/* ── Logout ─────────────────────────────────────────────────────────── */
async function logout() {
    try { await Api.post('/api/auth/logout', {}); } catch { /* ignore */ }
    Auth.clear();
    window.location.href = '/Auth/Logout';
}
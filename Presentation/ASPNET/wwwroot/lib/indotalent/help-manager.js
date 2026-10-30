// In-app contextual help. A single floating button + right-side drawer, injected once into the
// authenticated shell (loaded only from _Admin via __js.cshtml, so it never appears on login pages).
//
// Content lives in a central static repository (help-content.json) so page views never change when
// instructions are updated. Each entry is keyed by a normalised page id derived from the route
// (folder or folder/page), letting related pages share a topic while materially different pages get
// their own. Resolution runs fresh every time the panel opens, so content always matches the page
// currently shown and never goes stale. Rendering is done with textContent / DOM nodes only — no
// innerHTML of content fields — so help text can never execute scripts.
const HelpManager = (() => {
    const CONTENT_URL = '/lib/indotalent/help-content.json';

    let contentCache = null;   // in-memory for this page load: panel can reopen without re-fetching
    let loadPromise = null;
    let fab, overlay, drawer, headerTitle, headerDesc, body, closeBtn;
    let lastFocused = null;
    let built = false;

    // ── Content loading ────────────────────────────────────────────────────────────────────────
    const loadContent = () => {
        if (contentCache) return Promise.resolve(contentCache);
        if (loadPromise) return loadPromise;
        // 'no-cache' revalidates (cheap 304) so an updated file is picked up on the next page load,
        // while the in-memory cache above keeps repeated opens on one page free of requests.
        loadPromise = fetch(CONTENT_URL, { cache: 'no-cache', headers: { 'Accept': 'application/json' } })
            .then(r => (r.ok ? r.json() : []))
            .then(data => { contentCache = Array.isArray(data) ? data : []; return contentCache; })
            .catch(() => { contentCache = []; return contentCache; });
        return loadPromise;
    };

    // Route → { full: "folder/page", folder: "folder" }, lowercased, ids/params ignored.
    const keysFor = (pathname) => {
        const parts = (pathname || '/').toLowerCase().split('/').filter(Boolean);
        const folder = parts[0] || '';
        const page = parts[1] || '';
        return { full: page ? `${folder}/${page}` : folder, folder };
    };

    const resolveEntry = (entries, pathname) => {
        const { full, folder } = keysFor(pathname);
        const active = (entries || []).filter(e => (e.status || 'active') === 'active');
        const byId = (id) => active.find(e => (e.pageId || '').toLowerCase() === id);
        return (full && byId(full)) || (folder && byId(folder)) || null;
    };

    // ── Safe DOM builders (no innerHTML of content) ──────────────────────────────────────────────
    const el = (tag, className, text) => {
        const node = document.createElement(tag);
        if (className) node.className = className;
        if (text != null) node.textContent = text;
        return node;
    };

    const section = (title) => {
        const wrap = el('div', 'help-section');
        wrap.appendChild(el('div', 'help-section-title', title));
        return wrap;
    };

    const renderEntry = (entry) => {
        body.textContent = '';

        if (!entry) {
            const empty = el('div', 'help-empty');
            empty.appendChild(el('i', 'fas fa-question-circle'));
            empty.appendChild(el('p', null, 'There is no specific help for this page yet.'));
            empty.appendChild(el('p', null, 'Use the sidebar to navigate, and the toolbar buttons (Add, Edit, Delete, Search, Export) to manage records on list pages.'));
            body.appendChild(empty);
            return;
        }

        headerTitle.textContent = entry.title || 'Page Help';
        headerDesc.textContent = entry.description || '';

        if (entry.overview) {
            const s = section('Overview');
            s.appendChild(el('p', null, entry.overview));
            body.appendChild(s);
        }

        if (Array.isArray(entry.instructions) && entry.instructions.length) {
            const s = section('How to use this page');
            const ol = el('ol', 'help-steps');
            entry.instructions.forEach(step => ol.appendChild(el('li', null, step)));
            s.appendChild(ol);
            body.appendChild(s);
        }

        if (Array.isArray(entry.tips) && entry.tips.length) {
            const s = section('Important tips');
            const ul = el('ul', 'help-tips');
            entry.tips.forEach(tip => ul.appendChild(el('li', null, tip)));
            s.appendChild(ul);
            body.appendChild(s);
        }

        if (Array.isArray(entry.faqs) && entry.faqs.length) {
            const s = section('Common questions');
            entry.faqs.forEach(faq => {
                const card = el('div', 'help-faq');
                card.appendChild(el('div', 'help-faq-q', faq.q || ''));
                card.appendChild(el('p', 'help-faq-a', faq.a || ''));
                s.appendChild(card);
            });
            body.appendChild(s);
        }

        if (Array.isArray(entry.related) && entry.related.length) {
            const s = section('Related');
            const ul = el('ul', 'help-related');
            entry.related.forEach(rel => {
                const li = document.createElement('li');
                const a = el('a', null, rel.label || rel.url || 'Open');
                // Only allow app-relative links, so a content entry can never point off-site.
                const url = String(rel.url || '');
                a.setAttribute('href', url.startsWith('/') ? url : '#');
                li.appendChild(a);
                ul.appendChild(li);
            });
            s.appendChild(ul);
            body.appendChild(s);
        }
    };

    // ── Open / close ─────────────────────────────────────────────────────────────────────────────
    const open = async () => {
        lastFocused = document.activeElement;
        headerTitle.textContent = 'Page Help';
        headerDesc.textContent = '';
        body.textContent = '';
        body.appendChild(el('p', 'text-muted', 'Loading help…'));

        overlay.classList.add('open');
        drawer.classList.add('open');
        drawer.setAttribute('aria-hidden', 'false');
        document.addEventListener('keydown', onKeydown);
        closeBtn.focus();

        const entries = await loadContent();
        renderEntry(resolveEntry(entries, window.location.pathname));
    };

    const close = () => {
        overlay.classList.remove('open');
        drawer.classList.remove('open');
        drawer.setAttribute('aria-hidden', 'true');
        document.removeEventListener('keydown', onKeydown);
        if (lastFocused && typeof lastFocused.focus === 'function') lastFocused.focus();
    };

    const onKeydown = (e) => { if (e.key === 'Escape') close(); };

    // ── One-time build of the button + drawer ─────────────────────────────────────────────────────
    const build = () => {
        if (built) return;
        built = true;

        fab = el('button', 'help-fab');
        fab.type = 'button';
        fab.setAttribute('aria-label', 'Page Help');
        fab.setAttribute('title', 'Need help with this page?');
        fab.appendChild(el('i', 'fas fa-question-circle'));
        fab.addEventListener('click', open);

        overlay = el('div', 'help-overlay');
        overlay.addEventListener('click', close);

        drawer = el('aside', 'help-drawer');
        drawer.setAttribute('role', 'dialog');
        drawer.setAttribute('aria-modal', 'true');
        drawer.setAttribute('aria-label', 'Page help');
        drawer.setAttribute('aria-hidden', 'true');

        const header = el('div', 'help-drawer-header');
        const titleWrap = document.createElement('div');
        headerTitle = el('h5', null, 'Page Help');
        headerTitle.id = 'helpDrawerTitle';
        headerDesc = el('p', 'help-page-desc', '');
        titleWrap.appendChild(headerTitle);
        titleWrap.appendChild(headerDesc);
        drawer.setAttribute('aria-labelledby', 'helpDrawerTitle');

        closeBtn = el('button', 'help-close');
        closeBtn.type = 'button';
        closeBtn.setAttribute('aria-label', 'Close help');
        closeBtn.appendChild(el('i', 'fas fa-times'));
        closeBtn.addEventListener('click', close);

        header.appendChild(titleWrap);
        header.appendChild(closeBtn);

        body = el('div', 'help-drawer-body');

        drawer.appendChild(header);
        drawer.appendChild(body);

        document.body.appendChild(fab);
        document.body.appendChild(overlay);
        document.body.appendChild(drawer);
    };

    document.addEventListener('DOMContentLoaded', build);

    // Exposed for manual use/testing.
    return { open, close };
})();

/*
 * I18n — client-side, instant (no-reload) language switching.
 *
 * Two kinds of text on any page need translating, and they're handled two different ways:
 *
 * 1. Vue-rendered text (every page's own `{{ }}` interpolations) — call `I18n.t('some.key')`
 *    from inside a page's `setup()` and return it (or a wrapper) to the template. I18n.locale is
 *    a single shared Vue `ref`, so any page's Vue app that reads it inside a template expression
 *    re-renders automatically the instant the language changes — no reload, no per-page wiring
 *    beyond calling t().
 *
 * 2. Plain-DOM text that exists outside any Vue app (navbar, sidebar, footer — built with
 *    vanilla JS/Syncfusion, not Vue) — mark the element `data-i18n="some.key"` (or
 *    `data-i18n-attr="placeholder:some.key"` for an attribute rather than textContent) and
 *    I18n.translateStaticDom() sweeps and updates every such element. This runs once on load and
 *    again on every setLocale() call.
 *
 * Dictionaries live in i18n-dictionaries.js (loaded right after this file) as a plain nested
 * object per locale: window.I18nDictionaries = { en: {...}, bn: {...} }. Keys are dotted paths,
 * e.g. "common.save", "dashboard.title". A missing key falls back to the key itself rather than
 * throwing, so an not-yet-translated page never breaks — it just shows the raw key, which is an
 * obvious, harmless "needs translating" signal during rollout rather than a crash.
 */
const I18n = (() => {
    const STORAGE_KEY = 'appLocale';
    const DEFAULT_LOCALE = 'en';
    const SUPPORTED_LOCALES = ['en', 'bn'];

    const readStoredLocale = () => {
        try {
            const stored = localStorage.getItem(STORAGE_KEY);
            return SUPPORTED_LOCALES.includes(stored) ? stored : DEFAULT_LOCALE;
        } catch (error) {
            return DEFAULT_LOCALE;
        }
    };

    // A single shared Vue ref — every page's own, separate Vue app instance can read this same
    // object (it's just a JS object reference, not tied to one createApp root), so all of them
    // re-render together the instant it changes.
    const locale = Vue.ref(readStoredLocale());

    const resolve = (key, dict) => key.split('.').reduce((node, part) => (node && typeof node === 'object') ? node[part] : undefined, dict);

    const t = (key, fallback) => {
        const dict = (typeof I18nDictionaries !== 'undefined' ? I18nDictionaries : {})[locale.value];
        const value = dict ? resolve(key, dict) : undefined;
        if (typeof value === 'string') return value;
        // Falls back to the English dictionary before giving up entirely, so a key translated
        // only in "en" so far (e.g. a page not yet localized into Bangla) still shows real text
        // instead of the raw key while viewing in Bangla.
        const enDict = (typeof I18nDictionaries !== 'undefined' ? I18nDictionaries : {}).en;
        const enValue = enDict ? resolve(key, enDict) : undefined;
        if (typeof enValue === 'string') return enValue;
        return fallback !== undefined ? fallback : key;
    };

    const translateStaticDom = (root = document) => {
        root.querySelectorAll('[data-i18n]').forEach((el) => {
            el.textContent = t(el.getAttribute('data-i18n'));
        });
        root.querySelectorAll('[data-i18n-attr]').forEach((el) => {
            // "placeholder:some.key" or multiple, comma-separated: "placeholder:a.b,title:c.d"
            el.getAttribute('data-i18n-attr').split(',').forEach((pair) => {
                const [attr, key] = pair.split(':').map((s) => s.trim());
                if (attr && key) el.setAttribute(attr, t(key));
            });
        });
    };

    const setLocale = (next) => {
        if (!SUPPORTED_LOCALES.includes(next) || next === locale.value) return;
        locale.value = next;
        try {
            localStorage.setItem(STORAGE_KEY, next);
        } catch (error) {
            // Best-effort only — the language still applies for the rest of this page view.
        }
        translateStaticDom();
        document.documentElement.setAttribute('lang', next);
        document.dispatchEvent(new CustomEvent('i18n:locale-changed', { detail: { locale: next } }));
    };

    document.addEventListener('DOMContentLoaded', () => {
        document.documentElement.setAttribute('lang', locale.value);
        translateStaticDom();
    });

    return {
        locale,
        supportedLocales: SUPPORTED_LOCALES,
        t,
        setLocale,
        translateStaticDom,
    };
})();

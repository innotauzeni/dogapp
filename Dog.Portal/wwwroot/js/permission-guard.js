/*
 * Permission guard — hides action buttons the signed-in role is not allowed to use.
 *
 * Convenience only: the API still enforces every permission, so this never replaces server checks.
 *
 *  1. Explicit:  <button data-perm="pet.edit">          hidden unless the user holds pet.edit
 *                <button data-perm="pet.edit,pet.create"> hidden unless the user holds ANY of them
 *                data-perm-ignore                        never touched by the automatic rules
 *  2. Automatic: on a page whose module uses the standard verbs (view / create / edit|update / delete),
 *                Add/New/Create, Edit/Update, Delete/Remove and View buttons are hidden when the role
 *                lacks "<module>.<verb>". The module prefix comes from the sidebar menu entry for the
 *                current page (e.g. "Dogs/Index" with access name "pet.view" -> "pet").
 *                Buttons inside modals/forms are left alone (e.g. "remove row" inside an edit form).
 *
 * Page config is supplied by the layout: window.__PG = { perms: [...], prefix: "pet" }
 */
(function () {
    'use strict';
    var cfg = window.__PG || {};
    var perms = {};
    (cfg.perms || []).forEach(function (p) { perms[String(p).toLowerCase()] = true; });
    var prefix = String(cfg.prefix || '').toLowerCase();

    function has(p) { return !!perms[String(p).toLowerCase()]; }
    function hasAny(list) { return list.some(has); }

    var VERBS = ['view', 'create', 'edit', 'update', 'delete'];
    // Automatic rules only apply to modules that follow the standard verb naming for this user.
    var standard = prefix !== '' && VERBS.some(function (v) { return has(prefix + '.' + v); });

    function allowed(action) {
        if (action === 'edit') return has(prefix + '.edit') || has(prefix + '.update');
        return has(prefix + '.' + action);
    }

    var SKIP_AREAS = 'aside, header, nav, .sidebar, .topbar, .navbar, .dropdown-menu.user-menu, [data-perm-ignore]';
    var MODAL_AREAS = '.modal, .modal-backdrop-custom, .tr-modal, [role="dialog"], form';

    function classify(el) {
        var oc = (el.getAttribute('onclick') || '').toLowerCase();
        var txt = (el.textContent || '').replace(/\s+/g, ' ').trim().toLowerCase();
        var title = (el.getAttribute('title') || el.getAttribute('aria-label') || '').toLowerCase();
        var href = (el.getAttribute('href') || '').toLowerCase();
        var iconEl = el.querySelector('i[class*="bi-"]');
        var ic = iconEl ? iconEl.className.toLowerCase() : '';

        if (/close|cancel|dismiss|toggle|filter|search|clear|export|print|back/.test(oc) || /^(cancel|close|back|clear|filter|search|export|print)/.test(txt)) return null;

        if (/(^|[^a-z])(delete|remove|destroy)/.test(oc) || /^(delete|remove)\b/.test(txt) || /\b(delete|remove)\b/.test(title) || ic.indexOf('bi-trash') >= 0 || /\/delete/.test(href)) return 'delete';
        if (/(^|[^a-z])(edit|update)/.test(oc) || /^(edit|update|rename)\b/.test(txt) || /\b(edit|update)\b/.test(title) || ic.indexOf('bi-pencil') >= 0 || /\/edit/.test(href)) return 'edit';
        if (/(^|[^a-z])(add|new|create)[a-z]*\(/.test(oc) || /^(\+\s*)?(add|new|create)\b/.test(txt) || /\/(create|add|new)(\b|\/|\?|$)/.test(href) || (ic.indexOf('bi-plus') >= 0 && txt.length < 28)) return 'create';
        if (/^view\b/.test(txt) || /\bview\b/.test(title) || ic.indexOf('bi-eye') >= 0) return 'view';
        return null;
    }

    function hide(el) {
        el.style.setProperty('display', 'none', 'important');
        el.setAttribute('data-perm-hidden', '1');
    }

    function apply(root) {
        root = root || document;

        // 1. Explicit data-perm
        root.querySelectorAll('[data-perm]').forEach(function (el) {
            var need = el.getAttribute('data-perm').split(',').map(function (s) { return s.trim(); }).filter(Boolean);
            if (need.length && !hasAny(need)) hide(el);
        });

        // 2. Automatic rules
        if (standard) {
            root.querySelectorAll('button, a, [onclick], .act-item').forEach(function (el) {
                if (el.hasAttribute('data-perm') || el.hasAttribute('data-perm-hidden') || el.hasAttribute('data-perm-ignore')) return;
                if (el.closest(SKIP_AREAS) || el.closest(MODAL_AREAS)) return;
                var action = classify(el);
                if (action && !allowed(action)) hide(el);
            });
        }

        // A row menu whose every item was hidden should not leave an empty "⋮" button behind.
        root.querySelectorAll('.act-wrap').forEach(function (wrap) {
            var items = wrap.querySelectorAll('.act-item');
            if (items.length > 0 && Array.prototype.every.call(items, function (i) { return i.hasAttribute('data-perm-hidden'); })) hide(wrap);
        });
    }

    var scheduled = false;
    function schedule() {
        if (scheduled) return;
        scheduled = true;
        (window.requestAnimationFrame || setTimeout)(function () { scheduled = false; apply(document.querySelector('.content-area') || document); });
    }

    function start() {
        apply(document.querySelector('.content-area') || document);
        // Pages draw tables and menus with JavaScript after load; keep guarding what they add.
        var host = document.querySelector('.content-area');
        if (host && window.MutationObserver) new MutationObserver(schedule).observe(host, { childList: true, subtree: true });
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start); else start();
    window.PG = { can: has, canAny: function () { return hasAny(Array.prototype.slice.call(arguments)); }, apply: apply, prefix: prefix };
})();

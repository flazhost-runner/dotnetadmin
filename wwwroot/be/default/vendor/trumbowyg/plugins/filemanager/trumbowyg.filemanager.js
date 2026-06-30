/**
 * Trumbowyg File Manager Plugin — DotNetAdmin port
 * Endpoints: GET /admin/v1/media/list | POST /admin/v1/media/upload | POST /admin/v1/media/delete
 * CSRF: reads from <meta name="csrf-token"> → X-CSRF-TOKEN header
 */
(function($) {
    'use strict';

    function getCsrf() {
        var meta = document.querySelector('meta[name="csrf-token"]');
        return meta ? meta.getAttribute('content') : '';
    }

    function buildModal() {
        var overlay = document.createElement('div');
        overlay.id = 'fm-overlay';
        overlay.style.cssText = 'position:fixed;inset:0;z-index:9999;background:rgba(0,0,0,.5);display:flex;align-items:center;justify-content:center';

        overlay.innerHTML = [
            '<div style="background:#fff;border-radius:12px;width:700px;max-width:95vw;max-height:85vh;display:flex;flex-direction:column;overflow:hidden;box-shadow:0 20px 60px rgba(0,0,0,.3)">',
              '<div style="display:flex;align-items:center;justify-content:space-between;padding:16px 20px;border-bottom:1px solid #e5e7eb">',
                '<h3 style="margin:0;font-size:15px;font-weight:600;color:#1f2937">File Manager</h3>',
                '<button id="fm-close" style="border:0;background:0;cursor:pointer;color:#6b7280;font-size:18px">&times;</button>',
              '</div>',
              '<div style="padding:14px 20px;border-bottom:1px solid #e5e7eb;display:flex;gap:10px;align-items:center">',
                '<input type="file" id="fm-file-input" accept="image/*" style="flex:1;font-size:13px">',
                '<button id="fm-upload-btn" style="background:#3b82f6;color:#fff;border:0;border-radius:6px;padding:6px 14px;cursor:pointer;font-size:13px;white-space:nowrap">Upload</button>',
              '</div>',
              '<div id="fm-list" style="flex:1;overflow-y:auto;padding:14px 20px">',
                '<p style="color:#9ca3af;font-size:13px">Loading…</p>',
              '</div>',
            '</div>'
        ].join('');

        document.body.appendChild(overlay);
        return overlay;
    }

    function loadFileList(overlay, trumbowyg) {
        var list = overlay.querySelector('#fm-list');
        list.innerHTML = '<p style="color:#9ca3af;font-size:13px">Loading…</p>';
        fetch('/admin/v1/media/list', { headers: { 'X-CSRF-TOKEN': getCsrf() } })
            .then(function(r) { return r.json(); })
            .then(function(res) {
                if (!res.success || !res.data || res.data.length === 0) {
                    list.innerHTML = '<p style="color:#9ca3af;font-size:13px;text-align:center;padding:20px">No files uploaded yet.</p>';
                    return;
                }
                var html = '<div style="display:grid;grid-template-columns:repeat(auto-fill,minmax(130px,1fr));gap:10px">';
                res.data.forEach(function(item) {
                    html += [
                        '<div class="fm-item" style="border:1px solid #e5e7eb;border-radius:8px;overflow:hidden;cursor:pointer;transition:box-shadow .15s" ',
                             'data-url="' + item.url + '" data-key="' + item.key + '">',
                          '<div style="height:90px;overflow:hidden;background:#f9fafb;display:flex;align-items:center;justify-content:center">',
                            '<img src="' + item.url + '" alt="' + item.name + '" style="max-width:100%;max-height:90px;object-fit:contain">',
                          '</div>',
                          '<div style="padding:6px;border-top:1px solid #f3f4f6;display:flex;align-items:center;justify-content:space-between">',
                            '<span style="font-size:10px;color:#6b7280;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;max-width:80px" title="' + item.name + '">' + item.name + '</span>',
                            '<button class="fm-delete-btn" data-key="' + item.key + '" style="border:0;background:0;cursor:pointer;color:#ef4444;font-size:12px" title="Delete">&times;</button>',
                          '</div>',
                        '</div>'
                    ].join('');
                });
                html += '</div>';
                list.innerHTML = html;

                list.querySelectorAll('.fm-item').forEach(function(el) {
                    el.addEventListener('click', function(e) {
                        if (e.target.classList.contains('fm-delete-btn')) return;
                        insertImage(el.getAttribute('data-url'), trumbowyg);
                        closeModal(overlay);
                    });
                });

                list.querySelectorAll('.fm-delete-btn').forEach(function(btn) {
                    btn.addEventListener('click', function(e) {
                        e.stopPropagation();
                        if (!confirm('Delete this file?')) return;
                        deleteFile(btn.getAttribute('data-key'), overlay, trumbowyg);
                    });
                });
            })
            .catch(function() {
                list.innerHTML = '<p style="color:#ef4444;font-size:13px">Failed to load files.</p>';
            });
    }

    function uploadFile(file, overlay, trumbowyg) {
        var btn = overlay.querySelector('#fm-upload-btn');
        btn.disabled = true;
        btn.textContent = 'Uploading…';

        var fd = new FormData();
        fd.append('file', file);

        fetch('/admin/v1/media/upload', {
            method: 'POST',
            headers: { 'X-CSRF-TOKEN': getCsrf() },
            body: fd
        })
            .then(function(r) { return r.json(); })
            .then(function(res) {
                if (res.success) {
                    overlay.querySelector('#fm-file-input').value = '';
                    loadFileList(overlay, trumbowyg);
                } else {
                    alert(res.message || 'Upload failed');
                }
            })
            .catch(function() { alert('Upload failed'); })
            .finally(function() { btn.disabled = false; btn.textContent = 'Upload'; });
    }

    function deleteFile(key, overlay, trumbowyg) {
        var fd = new FormData();
        fd.append('key', key);

        fetch('/admin/v1/media/delete', {
            method: 'POST',
            headers: { 'X-CSRF-TOKEN': getCsrf() },
            body: fd
        })
            .then(function(r) { return r.json(); })
            .then(function(res) {
                if (res.success) loadFileList(overlay, trumbowyg);
                else alert(res.message || 'Delete failed');
            })
            .catch(function() { alert('Delete failed'); });
    }

    function insertImage(url, trumbowyg) {
        trumbowyg.execCmd('insertImage', url, false, true);
    }

    function closeModal(overlay) {
        if (overlay && overlay.parentNode) overlay.parentNode.removeChild(overlay);
    }

    function openFileManager(trumbowyg) {
        var existing = document.getElementById('fm-overlay');
        if (existing) existing.parentNode.removeChild(existing);

        var overlay = buildModal();

        overlay.querySelector('#fm-close').addEventListener('click', function() { closeModal(overlay); });
        overlay.addEventListener('click', function(e) { if (e.target === overlay) closeModal(overlay); });

        overlay.querySelector('#fm-upload-btn').addEventListener('click', function() {
            var input = overlay.querySelector('#fm-file-input');
            if (!input.files || !input.files[0]) { alert('Please select a file first'); return; }
            uploadFile(input.files[0], overlay, trumbowyg);
        });

        loadFileList(overlay, trumbowyg);
    }

    $.extend(true, $.trumbowyg, {
        langs: {
            en: { filemanager: 'File Manager' },
            id: { filemanager: 'Pengelola File' }
        },
        plugins: {
            filemanager: {
                init: function(trumbowyg) {
                    trumbowyg.addBtnDef('filemanager', {
                        fn: function() { openFileManager(trumbowyg); },
                        ico: 'insert-image',
                        title: $.trumbowyg.langs.en.filemanager
                    });
                }
            }
        }
    });
})(jQuery);

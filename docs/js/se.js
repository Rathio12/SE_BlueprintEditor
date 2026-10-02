window.se = {
  relativePaths: function (input) {
    if (!input || !input.files) return [];
    return Array.from(input.files).map(function (f) { return f.webkitRelativePath || f.name; });
  },
  load: function (key) {
    try { return window.localStorage.getItem(key); } catch (e) { return null; }
  },
  save: function (key, value) {
    try { window.localStorage.setItem(key, value); return true; } catch (e) { return false; }
  },
  copy: function (text) {
    if (navigator.clipboard && navigator.clipboard.writeText) {
      return navigator.clipboard.writeText(text).then(function () { return true; }, function () { return false; });
    }
    return Promise.resolve(false);
  },
  download: function (name, text) {
    var blob = new Blob(["﻿" + text], { type: "text/csv;charset=utf-8" });
    var a = document.createElement("a");
    a.href = URL.createObjectURL(blob);
    a.download = name;
    document.body.appendChild(a);
    a.click();
    setTimeout(function () { URL.revokeObjectURL(a.href); a.remove(); }, 0);
  },
  open: function (url) {
    window.open(url, "_blank", "noopener");
  }
};

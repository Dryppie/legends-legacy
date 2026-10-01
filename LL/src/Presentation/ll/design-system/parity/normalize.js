/*
 * Parity normalizer: turns each `.case` element under `root` into a comparable outline of its DOM — elements with
 * sorted attributes and classes, merged text — so the React reference and the Angular port can be compared.
 * Angular host elements (lg-*), comments, Angular's own attributes and generated ids are taken out.
 */
window.lgParityNormalize = function (root) {
  var out = {};
  var ID_ATTRS = ['id', 'aria-describedby', 'aria-controls', 'aria-labelledby', 'aria-activedescendant', 'for'];
  // Static attributes Angular leaves on attribute-selector components (button[lgButton], li[lgListRow], h1[lgHeading]).
  var INPUTS = ['sub', 'size', 'density', 'hotkey', 'icon', 'state', 'reason', 'shortfall', 'remaining', 'pendinglabel',
    'rarity', 'image', 'meta', 'quantity', 'value', 'live', 'selected', 'muted', 'interactive'];
  var SKIP = /^(_ng|ng-reflect|ng-version|lgslot$|lglistrow$|lgbutton$|lgheading$|lgwhy$|lgwhyid$|lgkey$)/;
  function styleNorm(s) {
    return s.split(';').map(function (x) { return x.trim(); }).filter(Boolean).map(function (x) {
      var i = x.indexOf(':'); return x.slice(0, i).trim() + ': ' + x.slice(i + 1).trim().replace(/"/g, '');
    }).sort().join('; ');
  }
  Array.prototype.forEach.call(root.querySelectorAll('.case'), function (c) {
    var ids = new Map(), n = 0;
    function mapId(v) { if (!ids.has(v)) ids.set(v, 'ID' + (++n)); return ids.get(v); }
    var lines = [];
    function walk(node, depth) {
      var pad = new Array(depth + 1).join('  ');
      if (node.nodeType === 3) { var t = node.textContent.replace(/\s+/g, ' ').trim(); if (t) lines.push(pad + '"' + t + '"'); return; }
      if (node.nodeType !== 1) return;
      var tag = node.tagName.toLowerCase();
      if (tag.indexOf('lg-') === 0) { Array.prototype.forEach.call(node.childNodes, function (k) { walk(k, depth); }); return; }
      var attrComponent = node.hasAttribute('lgbutton') || node.hasAttribute('lglistrow') || node.hasAttribute('lgheading');
      var attrs = [];
      Array.prototype.forEach.call(node.attributes, function (a) {
        var name = a.name, v = a.value;
        if (SKIP.test(name) || (attrComponent && INPUTS.indexOf(name) >= 0)) return;
        if (name === 'class') v = v.split(/\s+/).filter(Boolean).sort().join(' ');
        if (name === 'style') v = styleNorm(v);
        if (ID_ATTRS.indexOf(name) >= 0) v = v.split(/\s+/).map(mapId).join(' ');
        if (name === 'href' && v.charAt(0) === '#' && v.length > 1) v = '#' + mapId(v.slice(1));
        attrs.push(name + '="' + v + '"');
      });
      attrs.sort();
      lines.push(pad + '<' + tag + (attrs.length ? ' ' + attrs.join(' ') : '') + '>');
      if (tag === 'svg' && node.classList.contains('lg-icon')) { lines.push(pad + '  ' + node.innerHTML); return; }
      Array.prototype.forEach.call(node.childNodes, function (k) { walk(k, depth + 1); });
    }
    var clone = c.cloneNode(true);
    var tw = document.createTreeWalker(clone, NodeFilter.SHOW_COMMENT), comments = [];
    while (tw.nextNode()) comments.push(tw.currentNode);
    comments.forEach(function (x) { x.remove(); });
    clone.normalize();
    Array.prototype.forEach.call(clone.childNodes, function (k) { walk(k, 0); });
    out[c.getAttribute('data-case')] = { tree: lines.join('\n'), text: c.textContent.replace(/\s+/g, ' ').trim() };
  });
  return out;
};

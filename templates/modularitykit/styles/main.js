// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license.
(function () {
  var SIDEBAR_STATE_KEY = 'modularitykit.docfx.apiSidebarCollapsed';

  function forceApiSidebarOpen() {
    var sidetoggle = document.getElementById('sidetoggle');
    if (!sidetoggle) {
      return;
    }

    sidetoggle.classList.add('in');
    sidetoggle.classList.remove('collapse');
    sidetoggle.classList.remove('collapsing');
    sidetoggle.style.display = 'block';
    sidetoggle.style.height = 'auto';
    sidetoggle.style.visibility = 'visible';

    var tocToggle = document.querySelector('.toc-toggle');
    if (tocToggle) {
      tocToggle.setAttribute('aria-expanded', 'true');
      tocToggle.removeAttribute('data-toggle');
      tocToggle.removeAttribute('href');
      tocToggle.style.display = 'none';
    }

  }

  function getApiLayout() {
    return document.querySelector('.article.row.grid-right');
  }

  function getApiSidebar() {
    return document.querySelector('.article.row.grid-right > .hidden-sm.col-md-2');
  }

  function getApiMain() {
    return document.querySelector('.article.row.grid-right > .col-md-10');
  }

  function readSidebarState() {
    try {
      return window.localStorage.getItem(SIDEBAR_STATE_KEY);
    } catch (error) {
      return null;
    }
  }

  function writeSidebarState(isCollapsed) {
    try {
      window.localStorage.setItem(SIDEBAR_STATE_KEY, isCollapsed ? '1' : '0');
    } catch (error) {
      return;
    }
  }

  function getCurrentDocPath() {
    var path = window.location.pathname.replace(/\/+$/, '');
    var file = path.split('/').pop() || '';
    return {
      path: path,
      file: file
    };
  }

  function itemMatchesCurrentPage(item, current) {
    var links = item.querySelectorAll('a[href]');
    for (var i = 0; i < links.length; i += 1) {
      var href = links[i].getAttribute('href') || '';
      if (!href) {
        continue;
      }

      if (href === current.file) {
        return true;
      }

      if (current.path.indexOf(href.replace(/^\.\.\//, '')) !== -1) {
        return true;
      }
    }

    return false;
  }

  function setTocItemCollapsed(item, collapsed) {
    var stub = null;
    var childList = null;
    var children = item.children;

    for (var i = 0; i < children.length; i += 1) {
      if (!stub && children[i].classList.contains('expand-stub')) {
        stub = children[i];
      } else if (!childList && children[i].tagName === 'UL' && children[i].classList.contains('nav')) {
        childList = children[i];
      }
    }

    if (!stub || !childList) {
      return;
    }

    item.classList.toggle('api-toc-collapsed', collapsed);
    childList.hidden = collapsed;
    stub.setAttribute('aria-expanded', collapsed ? 'false' : 'true');
    stub.setAttribute('title', collapsed ? 'Expand section' : 'Collapse section');
  }

  function toggleTocItem(item) {
    setTocItemCollapsed(item, !item.classList.contains('api-toc-collapsed'));
  }

  function ensureApiTocCollapsible() {
    if (window.location.pathname.indexOf('/obj/api/') === -1) {
      return;
    }

    var tocRoot = document.querySelector('.toc .nav.level1');
    if (!tocRoot) {
      return;
    }

    var current = getCurrentDocPath();
    var items = tocRoot.children;

    Array.prototype.forEach.call(items, function (item) {
      var childList = null;
      var stub = null;
      var children = item.children;
      var j;

      for (j = 0; j < children.length; j += 1) {
        if (!stub && children[j].classList && children[j].classList.contains('expand-stub')) {
          stub = children[j];
        } else if (!childList && children[j].tagName === 'UL' && children[j].classList.contains('nav')) {
          childList = children[j];
        }
      }

      if (!childList || !stub) {
        return;
      }

      stub.setAttribute('role', 'button');
      stub.setAttribute('tabindex', '0');
      stub.setAttribute('aria-label', 'Toggle section');
      stub.style.display = 'inline-flex';
      stub.style.pointerEvents = 'auto';
      stub.textContent = '';

      var shouldCollapse = !itemMatchesCurrentPage(item, current);
      setTocItemCollapsed(item, shouldCollapse);

      stub.onclick = function (event) {
        event.preventDefault();
        event.stopPropagation();
        toggleTocItem(item);
      };

      stub.onkeydown = function (event) {
        if (event.key === 'Enter' || event.key === ' ') {
          event.preventDefault();
          toggleTocItem(item);
        }
      };
    });
  }

  function ensureApiSidebarToggle() {
    if (window.location.pathname.indexOf('/obj/api/') === -1) {
      return;
    }

    var layout = getApiLayout();
    var sidebar = getApiSidebar();
    var main = getApiMain();

    if (!layout || !sidebar || !main) {
      return;
    }

    var toolbar = main.querySelector('.api-layout-tools');
    if (!toolbar) {
      toolbar = document.createElement('div');
      toolbar.className = 'api-layout-tools';

      var toggle = document.createElement('button');
      toggle.type = 'button';
      toggle.className = 'api-layout-toggle';
      toggle.setAttribute('aria-expanded', 'true');
      toggle.setAttribute('aria-controls', 'api-sidepanel');
      toggle.textContent = 'Hide side panel';

      toggle.addEventListener('click', function () {
        var collapsed = document.body.classList.toggle('api-sidebar-collapsed');
        writeSidebarState(collapsed);
        toggle.setAttribute('aria-expanded', collapsed ? 'false' : 'true');
        toggle.textContent = collapsed ? 'Show side panel' : 'Hide side panel';
      });

      toolbar.appendChild(toggle);
      main.insertBefore(toolbar, main.firstChild);
    }

    sidebar.id = 'api-sidepanel';

    var collapsed = readSidebarState() === '1';
    document.body.classList.toggle('api-sidebar-collapsed', collapsed);

    var toggleButton = toolbar.querySelector('.api-layout-toggle');
    if (toggleButton) {
      toggleButton.setAttribute('aria-expanded', collapsed ? 'false' : 'true');
      toggleButton.textContent = collapsed ? 'Show side panel' : 'Hide side panel';
    }
  }

  document.addEventListener('click', function (event) {
    if (event.target.closest('.toc-toggle')) {
      event.preventDefault();
      event.stopImmediatePropagation();
    }
  }, true);

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', forceApiSidebarOpen);
    document.addEventListener('DOMContentLoaded', ensureApiSidebarToggle);
    document.addEventListener('DOMContentLoaded', ensureApiTocCollapsible);
  } else {
    forceApiSidebarOpen();
    ensureApiSidebarToggle();
    ensureApiTocCollapsible();
  }
})();
